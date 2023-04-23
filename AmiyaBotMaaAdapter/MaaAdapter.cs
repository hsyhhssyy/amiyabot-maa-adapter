using AmiyaBotMaaAdapter.Helpers;
using AmiyaBotMaaAdapter.Interop;
using HandyControl.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AmiyaBotMaaAdapter
{
    internal class MaaAdapter
    {
        private class MaaTask
        {
            public String Uuid { get; set; }
            public String Type { get; set; }
            public String Parameter { get; set; }
        }

        public static MaaAdapter CurrentAdapter { get; set; } = new MaaAdapter();
        public string Server
        {
            get => _server;
            set
            {
                if (_server == value) return;
                _server = value;
                SaveConfig();
            } }

        public string Signature
        {
            get => _signature;
            set => _signature = value;
        }

        private const string FilePath = "secret.json";

        private string _uuid;
        private string _secret;
        private string _maa;
        private string _server;
        private string _signature;

        private bool _listening;

        private ConcurrentQueue<MaaTask> _taskQueue=new ConcurrentQueue<MaaTask>();
        private List<String> ReceivedTask = new List<String>();
        private IntPtr _handle;

        public void Load()
        {

            string uuid = "";
            string secret = "";

            // Check if file exists and is valid JSON
            if (File.Exists(FilePath))
            {
                try
                {
                    string json = File.ReadAllText(FilePath);
                    Dictionary<string, object> data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    if (data != null)
                    {
                        if (data.ContainsKey("uuid"))
                        {
                            uuid = (string)data["uuid"];
                        }

                        if (data.ContainsKey("secret"))
                        {
                            secret = (string)data["secret"];
                        }

                        if (data.ContainsKey("server"))
                        {
                            _server = (string)data["server"];
                        }

                        if (data.ContainsKey("maa"))
                        {
                            _maa = (string)data["maa"];
                        }

                        if (data.ContainsKey("signature"))
                        {
                            _signature = (string)data["signature"];
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("无法读取密钥文件，文件可能损坏或不存在。错误原因是:"+e.Message);
                }
            }

            // If UUID or Secret is missing, generate new values
            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(secret))
            {
                uuid = Guid.NewGuid().ToString();
                secret = Guid.NewGuid().ToString();

                SaveConfig();
            }

            _uuid = uuid;
            _secret = secret;

            Thread thread = new Thread(EnumratorWorker);
            thread.IsBackground = true;
            thread.Start();

            thread = new Thread(AmiyaBotWorker);
            thread.IsBackground = true;
            thread.Start();
        }

        private void SaveConfig()
        {
            // Write new values to file
            dynamic newData = new
            {
                uuid = _uuid,
                secret = _secret,
                maa = _maa,
                server = _server,
                signature = _signature
            };

            string newJson = JsonConvert.SerializeObject(newData);
            File.WriteAllText(FilePath, newJson);
        }

        public String GenerateSignature()
        {
            _uuid = Guid.NewGuid().ToString();
            _secret = Guid.NewGuid().ToString();

            SaveConfig();
            
            var error = GetResponseData(HttpHelper.PostAction(_server + "/maa/token", JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "uuid", _uuid },
                { "secret", _secret  }
            })), out var signatureResponse);

            if (error != null)
            {
                MessageBox.Show("获取密钥出错，错误原因：" + error);
                return "";
            }
            
            _signature = signatureResponse["code"].ToString();

            SaveConfig();

            return _signature;
        }

        public void StartListen()
        {
            if (_listening)
            {
                return;
            }

            Task.Run(() =>
            {

                Logger.Current.Info("正在连接模拟器.....");

                //AstInterop.AsstSetUserDir("")
                AsstInterop.AsstLoadResource("E:\\Tools\\MAA");
                //var handle = AstInterop.AsstCreateEx(callback, IntPtr.Zero);
                _handle = AsstInterop.AsstCreate();

                AsstInterop.AsstSetInstanceOption(_handle, (int)InstanceOptionType.touch_type, "adb");

                var success =
                    AsstInterop.AsstConnect(_handle, "E:\\LeiDian\\LDPlayer9\\adb.exe", "emulator-5556", "LDPlayer");

                if (success)
                {
                    Logger.Current.Info("模拟器连接成功。");
                    _listening = true;
                }
                else
                {
                    Logger.Current.Report("模拟器连接失败!");
                }
            });
        }

        private void EnumratorWorker()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (!_listening)
                {
                    continue;
                }

                MaaTask item;
                if (_taskQueue.TryDequeue(out item))
                {
                    bool executeSuccess = false;
                    try
                    {
                        // 执行某个操作
                        AsstInterop.AsstAppendTask(_handle, item.Type, item.Parameter);
                        bool success = AsstInterop.AsstStart(_handle);
                        Logger.Current.Info($"开始执行任务{item.Type}({item.Uuid})。");
                        while (AsstInterop.AsstRunning(_handle))
                        {
                            Thread.Sleep(1000);
                        }

                        executeSuccess = true;
                        Logger.Current.Info($"任务{item.Uuid}执行成功。");
                    }
                    catch (Exception exp)
                    {
                        Logger.Current.Critical(Logger.Current.FormatException(exp, ""));
                    }
                    finally
                    {
                        //汇报任务进度
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void AmiyaBotWorker()
        {
            while (true)
            {
                Thread.Sleep(30*1000);
                if (!_listening)
                {
                    continue;
                }

                //联网获取任务
                var error = GetResponseData(HttpHelper.PostAction(_server + "/maa/getTask", JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    { "uuid", _uuid },
                    { "signature", _signature  }
                })),out var getTaskResponse);

                if (error != null)
                {
                    Logger.Current.Report("联网获取任务报错:"+error);
                    continue;
                }

                var tasks = getTaskResponse["task"] as List<object>;
                if (tasks != null)
                {
                    var validTasks = tasks.OfType<Dictionary<String, object>>().ToList();


                    if (validTasks.Any())
                    {
                        var taskToEnqueue = new List<MaaTask>();

                        foreach (var task in validTasks)
                        {

                            var maa = new MaaTask();
                            maa.Type = task["type"]?.ToString();
                            maa.Parameter = task["parameter"].ToString();
                            maa.Uuid = task["uuid"]?.ToString();

                            if (!ReceivedTask.Contains(maa.Uuid))
                            {
                                ReceivedTask.Add(maa.Uuid);
                                taskToEnqueue.Add(maa);
                            }
                        }

                        if (taskToEnqueue.Any())
                        {

                            //如果队列是空的，默认添加一个唤醒
                            //if (_taskQueue.Count == 0)
                            //{
                            //    var maa = new MaaTask();
                            //    maa.Type = "StartUp";
                            //    maa.Parameter = "{\"start_game_enabled\":\"true\"}";
                            //    maa.Uuid = "";

                            //    _taskQueue.Enqueue(maa);
                            //}

                            foreach (var maaTask in taskToEnqueue)
                            {
                                _taskQueue.Enqueue(maaTask);
                            }

                            Logger.Current.Info("联网获取了" + tasks.Count + "个任务。");
                        }
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private String GetResponseData(HttpHelper.DeserializedHttpResponse rawResponse,out Dictionary<String,Object> data)
        {
            data = new Dictionary<String, Object>();

            if (rawResponse.Success == false)
            {
                return rawResponse.RawData;
            }

            var dictResponse = JsonConvertHelper.FormatToDictionary(rawResponse.Data) as Dictionary<String, object>;
            
            if (dictResponse == null)
            {
                return "未知错误";
            }

            dictResponse = dictResponse["data"] as Dictionary<String, object>;

            if (dictResponse == null)
            {
                return "未知错误";
            }

            if ((bool)dictResponse["success"] == false)
            {
                return dictResponse["reason"]?.ToString();
            }

            data = dictResponse;


            return null;
        }
    }
}
