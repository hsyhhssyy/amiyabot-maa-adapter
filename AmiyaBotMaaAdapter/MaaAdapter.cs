using AmiyaBotMaaAdapter.Helpers;
using AmiyaBotMaaAdapter.Interop;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using static AmiyaBotMaaAdapter.Interop.AsstInterop;

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
        private static AsstInterop.AsstApiCallback asstCallback = new AsstInterop.AsstApiCallback(Asst_OnCallback);
        private static int AsstLastCallback = 0 ;

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

        public string Resources
        {
            get => _resources;
            set
            {
                if (_resources == value) return;
                _resources = value;
                SaveConfig();
            }
        }

        private const string FilePath = "secret.json";

        private string _resources;
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

                        if (data.ContainsKey("resources"))
                        {
                            _resources = (string)data["resources"];
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
                signature = _signature,
                resources = _resources,
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

                if (string.IsNullOrWhiteSpace(_resources))
                {
                    return;
                }

                Logger.Current.Info("正在连接模拟器.....");

                //打开Gui.json
                var guiPath = Path.Combine(_resources, "config/gui.json");

                var guiJson = JsonConvert.DeserializeObject<Dictionary<String, object>>(File.ReadAllText(guiPath));


                //AstInterop.AsstSetUserDir("")
                AsstInterop.AsstLoadResource(_resources);
                _handle = AsstInterop.AsstCreateEx(asstCallback, IntPtr.Zero);
                //_handle = AsstInterop.AsstCreate();

                AsstInterop.AsstSetInstanceOption(_handle, (int)InstanceOptionType.touch_type,
                    guiJson["Connect.TouchMode"]?.ToString());

                var success =
                    AsstInterop.AsstConnect(_handle, guiJson["Connect.AdbPath"]?.ToString(),
                        guiJson["Connect.Address"]?.ToString(), guiJson["Connect.ConnectConfig"]?.ToString());

                if (success)
                {
                    Logger.Current.Info("模拟器连接成功。");

                    UploadGuiJson();

                    _listening = true;
                }
                else
                {
                    Logger.Current.Report("模拟器连接失败!");
                }
            });
        }

        private void UploadGuiJson()
        {
            var guiPath = Path.Combine(_resources, "config/gui.json");

            var error = GetResponseData(HttpHelper.PostAction(_server + "/maa/guiJson", JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "uuid", _uuid },
                { "signature", _signature  },
                { "gui_json", File.ReadAllText(guiPath) }
            })), out var getTaskResponse);

            if (error != null)
            {
                Logger.Current.Report("上传GuiJson发生错误:" + error);
            }
        }

        private static void Asst_OnCallback(int msg, string detailsJson, IntPtr customArg)
        {
            AsstLastCallback = msg;
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
                    String payload = "";
                    try
                    {
                        switch (item.Type)
                        {
                            case "CaptureImage":
                            {
                                ulong buffSize = 1024 * 1024 * 50;
                                IntPtr buff = Marshal.AllocHGlobal((int)buffSize);
                                var actualSize = AsstInterop.AsstGetImage(_handle, buff, buffSize);

                                var managedBuff = new byte[actualSize];
                                Marshal.Copy(buff,managedBuff,0,(int)actualSize);
                                Marshal.FreeHGlobal(buff);

                                payload = Convert.ToBase64String(managedBuff);
                                
                                break;
                            }
                            default:
                                // 执行某个操作
                                AsstInterop.AsstAppendTask(_handle, item.Type, item.Parameter);
                                Logger.Current.Info($"开始执行任务{item.Type}({item.Uuid})。");
                                AsstInterop.AsstStart(_handle);
                                while (AsstInterop.AsstRunning(_handle)!=0
                                       && (AsstLastCallback!= 10002 && AsstLastCallback!=3))
                                {
                                    Thread.Sleep(1000);
                                }

                                if (AsstInterop.AsstRunning(_handle) != 0)
                                {
                                    AsstInterop.AsstStop(_handle);
                                }
                                executeSuccess = true;
                                Logger.Current.Report("任务执行成功");
                                break;
                        }
                    }
                    catch (Exception exp)
                    {
                        Logger.Current.Critical(Logger.Current.FormatException(exp, ""));
                    }
                    finally
                    {
                        //汇报任务进度
                        var error = GetResponseData(HttpHelper.PostAction(_server + "/maa/reportStatus", JsonConvert.SerializeObject(new Dictionary<string, string>()
                        {
                            { "uuid", _uuid },
                            { "signature", _signature  },
                            { "status", executeSuccess?"COMPLETE":"FAIL"  },
                            { "task", item.Uuid  },
                            { "payload", payload  },
                        })), out var getTaskResponse);

                        if (error != null)
                        {
                            Logger.Current.Report("汇报结果时发生错误:" + error);
                        }
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
                            if (_taskQueue.Count == 0)
                            {
                                var maa = new MaaTask();
                                maa.Type = "StartUp";
                                maa.Parameter = "{\"start_game_enabled\":\"true\"}";
                                maa.Uuid = "";

                                _taskQueue.Enqueue(maa);
                            }

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
                if (dictResponse.ContainsKey("reason"))
                {
                    return dictResponse["reason"]?.ToString();
                }
                return "未知错误";
            }

            data = dictResponse;


            return null;
        }
    }
}
