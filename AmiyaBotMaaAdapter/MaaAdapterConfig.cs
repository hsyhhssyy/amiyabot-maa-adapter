using AmiyaBotMaaAdapter.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiyaBotMaaAdapter
{
    public class MaaAdapterConfig
    {
        public static MaaAdapterConfig CurrentConfig { get; } = new MaaAdapterConfig();
        
        // -- general config --
        
        private string _server;
        private string _signature;
        private string _resources;
        private string _uuid;
        private string _secret;
        private string _maaDirectory;

        // -- adb config --


        private string _adbAddress;
        private string _adbFilePath;
        private string _adbConnectMode;
        private string _adbTouchMode;

        //

        public MaaAdapterConfig()
        {
            var exeFile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (exeFile.Exists && exeFile.DirectoryName != null)
            {
                var maaPathCfgFile = new FileInfo(Path.Combine(exeFile.DirectoryName, "MaaDir.json"));

                if (maaPathCfgFile.Exists)
                {
                    _maaDirectory = File.ReadAllText(maaPathCfgFile.FullName);
                    LoadConfig();
                }
            }
        }
        
        public string Server
        {
            get => _server;
            set
            {
                if (_server == value) return;
                _server = value;
                SaveConfig();
            }
        }

        public string Signature
        {
            get => _signature;
            set
            {
                if (_signature == value) return;
                _signature = value;
                SaveConfig();
            }
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

        public string Uuid
        {
            get => _uuid;
            set
            {
                if (_uuid == value) return;
                _uuid = value;
                SaveConfig();
            }
        }

        public string Secret
        {
            get => _secret;
            set
            {
                if (_secret == value) return;
                _secret = value;
                SaveConfig();
            }
        }

        public string MaaDirectory
        {
            get => _maaDirectory;
            set
            {
                if (_maaDirectory == value) return;
                //这个属性要特别保存
                var exeFile = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (exeFile.Exists&&exeFile.DirectoryName!=null)
                {
                    var maaPathCfgFile = new FileInfo(Path.Combine(exeFile.DirectoryName, "MaaDir.json"));

                    _maaDirectory = value;
                    File.WriteAllText(maaPathCfgFile.FullName,value);
                    LoadConfig();
                }
            }
        }

        public string AdbAddress
        {
            get => _adbAddress;
            set
            {
                if (_adbAddress == value) return;
                _adbAddress = value;
                SaveConfig();
            }
        }

        public string AdbFilePath
        {
            get => _adbFilePath;
            set
            {
                if (_adbFilePath == value) return;
                _adbFilePath = value;
                SaveConfig();
            }
        }

        public string AdbConnectMode
        {
            get => _adbConnectMode;
            set
            {
                if (_adbConnectMode == value) return;
                _adbConnectMode = value;
                SaveConfig();
            }
        }

        public string AdbTouchMode
        {
            get => _adbTouchMode;
            set
            {
                if (_adbTouchMode == value) return;
                _adbTouchMode = value;
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            var cfgDirectory = new DirectoryInfo(Path.Combine(_maaDirectory, "AmiyaBotAdapterData"));
            if (!cfgDirectory.Exists)
            {
                cfgDirectory.Create();
                return;
            }

            var cfgFile = new FileInfo(Path.Combine(cfgDirectory.FullName, "config.json"));

            dynamic data = new
            {
                server = _server,
                signature = _signature,
                resources = _resources,
                uuid = _uuid,
                secret = _secret,
                maaDirectory = _maaDirectory,
                adbAddress = _adbAddress,
                adbFilePath = _adbFilePath,
                adbConnectMode = _adbConnectMode,
                adbTouchMode = _adbTouchMode
            };

            string json = JsonConvert.SerializeObject(data);

            using StreamWriter w = new StreamWriter(cfgFile.FullName);
            w.Write(json);
        }

        private void LoadConfig()
        {
            var cfgDirectory = new DirectoryInfo(Path.Combine(_maaDirectory,"AmiyaBotAdapterData"));
            if (!cfgDirectory.Exists)
            {
                cfgDirectory.Create();
                return;
            }

            var cfgFile = new FileInfo(Path.Combine(cfgDirectory.FullName, "config.json"));
            if (!cfgFile.Exists)
            {
                return;
            }

            using (StreamReader r = new StreamReader(cfgFile.FullName))
            {
                string json = r.ReadToEnd();
                dynamic data = JsonConvert.DeserializeObject(json);
                _server = data.server;
                _signature = data.signature;
                _resources = data.resources;
                _uuid = data.uuid;
                _secret = data.secret;
                _maaDirectory = data.maaDirectory;
                _adbAddress = data.adbAddress;
                _adbFilePath = data.adbFilePath;
                _adbConnectMode = data.adbConnectMode;
                _adbTouchMode = data.adbTouchMode;
            }
        }
    }
}
