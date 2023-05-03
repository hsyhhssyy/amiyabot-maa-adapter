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
using Path = System.IO.Path;

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

        public static MaaAdapter CurrentAdapter { get; set; } = new();

        private static readonly AsstInterop.AsstApiCallback AsstCallback = Asst_OnCallback;
        private static int _asstLastCallback ;
        
        private bool _listening;

        private readonly ConcurrentQueue<MaaTask> _taskQueue=new();
        private readonly List<String> _receivedTask = new();
        private IntPtr _handle;

        private Thread _executeMaaTaskThread;
        private Thread _poolingTaskFromAmiyaBotThread;

        private Task _singleThreadTask = Task.CompletedTask;

        public void Load()
        {
            lock (this)
            {
                _singleThreadTask = _singleThreadTask.ContinueWith(_ =>
                {
                    if (_executeMaaTaskThread == null)
                    {
                        _executeMaaTaskThread = new Thread(ExecuteMaaTaskThreadWorker)
                        {
                            IsBackground = true
                        };
                        _executeMaaTaskThread.Start();
                    }


                    if (_poolingTaskFromAmiyaBotThread == null)
                    {
                        _poolingTaskFromAmiyaBotThread = new Thread(PollingTaskFromAmiyaBotThreadWorker)
                        {
                            IsBackground = true
                        };
                        _poolingTaskFromAmiyaBotThread.Start();
                    }

                    if (_listening)
                    {
                        return;
                    }

                    Logger.Current.Info("正在连接模拟器.....");

                    Directory.SetCurrentDirectory(MaaAdapterConfig.CurrentConfig.MaaDirectory);

                    //打开Gui.json
                    //var guiPath = Path.Combine(MaaAdapterConfig.CurrentConfig.MaaDirectory, "config/gui.json");

                    //var guiJson = JsonConvert.DeserializeObject<Dictionary<String, object>>(File.ReadAllText(guiPath));
                    
                    //AstInterop.AsstSetUserDir("")
                    AsstInterop.AsstLoadResource(MaaAdapterConfig.CurrentConfig.MaaDirectory);
                    _handle = AsstInterop.AsstCreateEx(AsstCallback, IntPtr.Zero);
                    //_handle = AsstInterop.AsstCreate();

                    AsstInterop.AsstSetInstanceOption(_handle, (int)InstanceOptionType.touch_type,
                        MaaAdapterConfig.CurrentConfig.AdbTouchMode);

                    var success =
                        AsstInterop.AsstConnect(_handle, MaaAdapterConfig.CurrentConfig.AdbFilePath,
                            MaaAdapterConfig.CurrentConfig.AdbAddress, MaaAdapterConfig.CurrentConfig.AdbConnectMode);
                
                    
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

            
            
        }


        private void UploadGuiJson()
        {
            Logger.Current.Info("正在上传GuiJson");

            var guiPath = Path.Combine(MaaAdapterConfig.CurrentConfig.MaaDirectory, "config/gui.json");

            var error = HttpHelper.PostAction(MaaAdapterConfig.CurrentConfig.Server + "/maa/guiJson", JsonConvert.SerializeObject(new Dictionary<string, string>()
            {
                { "uuid", MaaAdapterConfig.CurrentConfig.Uuid },
                { "signature", MaaAdapterConfig.CurrentConfig.Signature  },
                { "gui_json", File.ReadAllText(guiPath) }
            })).GetResponseData(out _);

            if (error != null)
            {
                Logger.Current.Report("上传GuiJson发生错误:" + error);
            }
            else
            {
                Logger.Current.Info("上传GuiJson完成");
            }
        }

        private static void Asst_OnCallback(int msg, string detailsJson, IntPtr customArg)
        {
            _asstLastCallback = msg;
            Logger.Current.Info("Callback:"+detailsJson);
        }
        
        private void ExecuteMaaTaskThreadWorker()
        {
            while (true)
            {
                Thread.Sleep(100);
                if (!_listening)
                {
                    continue;
                }

                if (_taskQueue.TryDequeue(out var currentExecutingTask))
                {
                    switch (currentExecutingTask.Type)
                    {
                        case "CaptureImage":
                            CaptureImage(currentExecutingTask);
                            break;
                        default:
                            bool executeSuccess = false;
                            String payload = "";
                            try
                            {

                                // 执行某个操作
                                AsstInterop.AsstAppendTask(_handle, currentExecutingTask.Type, currentExecutingTask.Parameter);
                                Logger.Current.Info($"开始执行任务{currentExecutingTask.Type}({currentExecutingTask.Uuid})。");
                                AsstInterop.AsstStart(_handle);
                                while (AsstInterop.AsstRunning(_handle) != 0
                                       && (_asstLastCallback != 10002 && _asstLastCallback != 3))
                                {
                                    Thread.Sleep(1000);
                                }

                                _asstLastCallback = 0;

                                if (AsstInterop.AsstRunning(_handle) != 0)
                                {
                                    AsstInterop.AsstStop(_handle);
                                }

                                executeSuccess = true;
                                Logger.Current.Report("任务执行成功");
                            }
                            catch (Exception exp)
                            {
                                Logger.Current.Critical(Logger.Current.FormatException(exp, ""));
                            }
                            finally
                            {
                                if (!string.IsNullOrWhiteSpace(currentExecutingTask.Uuid))
                                {
                                    //汇报任务进度
                                    var error = HttpHelper.PostAction(MaaAdapterConfig.CurrentConfig.Server + "/maa/reportStatus",
                                        JsonConvert.SerializeObject(new Dictionary<string, string>()
                                        {
                                            { "uuid", MaaAdapterConfig.CurrentConfig.Uuid },
                                            { "signature", MaaAdapterConfig.CurrentConfig.Signature },
                                            { "status", executeSuccess ? "COMPLETE" : "FAIL" },
                                            { "task", currentExecutingTask.Uuid },
                                            { "payload", payload },
                                        })).GetResponseData( out _);

                                    if (error != null)
                                    {
                                        Logger.Current.Report("汇报结果时发生错误:" + error);
                                    }

                                }
                            }

                            break;
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void PollingTaskFromAmiyaBotThreadWorker()
        {
            int sleepTime = 15 * 1000;
            while (true)
            {
                if (sleepTime < 500)
                {
                    sleepTime = 15 * 1000;
                }
                Thread.Sleep(sleepTime);
                if (!_listening)
                {
                    continue;
                }

                //联网获取任务
                var error = HttpHelper.PostAction(MaaAdapterConfig.CurrentConfig.Server + "/maa/getTask", JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    { "uuid", MaaAdapterConfig.CurrentConfig.Uuid },
                    { "signature", MaaAdapterConfig.CurrentConfig.Signature }
                })).GetResponseData(out var getTaskResponse);

                if (error != null)
                {
                    Logger.Current.Report("联网获取任务报错:"+error);
                    continue;
                }

                if (!getTaskResponse.ContainsKey("task"))
                {
                    Logger.Current.Report("联网获取任务报错。");
                    continue;
                }

                if (!int.TryParse(getTaskResponse.GetValueOrDefault("tick")?.ToString(),out sleepTime))
                {
                    sleepTime = 15 * 1000;
                }

                if (getTaskResponse.GetValueOrDefault("task") is List<object> tasks)
                {
                    var validTasks = tasks.OfType<Dictionary<String, object>>().ToList();
                    
                    if (validTasks.Any())
                    {
                        var taskToEnqueue = new List<MaaTask>();

                        foreach (var task in validTasks)
                        {

                            var maa = new MaaTask
                            {
                                Type = task["type"]?.ToString(),
                                Parameter = task["parameter"].ToString(),
                                Uuid = task["uuid"]?.ToString()
                            };

                            if (!_receivedTask.Contains(maa.Uuid))
                            {
                                _receivedTask.Add(maa.Uuid);
                                taskToEnqueue.Add(maa);
                            }
                        }

                        if (taskToEnqueue.Any())
                        {

                            /*//如果队列是空的，并且添加的task的第一个不是唤醒，并且不是截图，默认添加一个唤醒
                            if (_taskQueue.Count == 0&&taskToEnqueue.FirstOrDefault()?.Type!= "StartUp"
                                                     && taskToEnqueue.FirstOrDefault()?.Type != "Custom"
                                && !taskToEnqueue.All(t=>t.Type.StartsWith("Capture")))
                            {
                                var maa = new MaaTask
                                {
                                    Type = "StartUp",
                                    Parameter = "{\"start_game_enabled\":\"true\"}",
                                    Uuid = ""
                                };

                                _taskQueue.Enqueue(maa);
                            }*/

                            foreach (var maaTask in taskToEnqueue)
                            {
                                switch (maaTask.Type)
                                {
                                    case "CaptureImageNow":
                                    {
                                        CaptureImage(maaTask);
                                        break;
                                    }
                                    default:
                                        _taskQueue.Enqueue(maaTask);
                                        break;
                                }
                            }

                            Logger.Current.Info("联网获取了" + tasks.Count + "个任务。");
                        }
                    }
                }
            }
            // ReSharper disable once FunctionNeverReturns
        }

        private void CaptureImage(MaaTask maaTask)
        {
            Logger.Current.Info("插入截图任务");
            ulong buffSize = 1024 * 1024 * 50;
            IntPtr buff = Marshal.AllocHGlobal((int)buffSize);
            var actualSize = AsstInterop.AsstGetImage(_handle, buff, buffSize);

            var managedBuff = new byte[actualSize];
            Marshal.Copy(buff, managedBuff, 0, (int)actualSize);
            Marshal.FreeHGlobal(buff);

            var payload = Convert.ToBase64String(managedBuff);

            Logger.Current.Info("截图任务执行完成");

            //汇报任务进度
            var error = HttpHelper.PostAction(MaaAdapterConfig.CurrentConfig.Server + "/maa/reportStatus",
                JsonConvert.SerializeObject(new Dictionary<string, string>()
                {
                    { "uuid", MaaAdapterConfig.CurrentConfig.Uuid },
                    { "signature", MaaAdapterConfig.CurrentConfig.Signature },
                    { "status", "COMPLETE" },
                    { "task", maaTask.Uuid },
                    { "payload", payload },
                })).GetResponseData(out var _);

            if (error != null)
            {
                Logger.Current.Report("汇报结果时发生错误:" + error);
            }
        }
        
        public void Stop()
        {
            
        }
    }
}
