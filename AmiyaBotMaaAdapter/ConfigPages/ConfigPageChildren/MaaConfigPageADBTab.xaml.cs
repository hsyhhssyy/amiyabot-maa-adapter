using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AmiyaBotMaaAdapter.ConfigPages.ConfigPageChildren
{
    /// <summary>
    /// MaaConfigPageADBTab.xaml 的交互逻辑
    /// </summary>
    public partial class MaaConfigPageADBTab : UserControl
    {
        private readonly Timer configValiateTimer;

        public MaaConfigPageADBTab()
        {
            InitializeComponent();

            cboAdbMode.Items.Clear();

            configValiateTimer = new Timer();
            configValiateTimer.AutoReset = true;
            configValiateTimer.Elapsed += ConfigValidateTimer_Elapsed;
            configValiateTimer.Interval = 200;
            configValiateTimer.Start();

            txtAdbFilePath.Text = MaaAdapterConfig.CurrentConfig.AdbFilePath;
            txtAdbAddress.Text = MaaAdapterConfig.CurrentConfig.AdbAddress;
            cboTouchMode.Text = MaaAdapterConfig.CurrentConfig.AdbTouchMode;
            if (String.IsNullOrWhiteSpace(cboTouchMode.Text))
            {
                cboTouchMode.Text = "minitouch";
            }
        }

        private void ConfigValidateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            configValiateTimer.Stop();

            try
            {
                if (string.IsNullOrWhiteSpace(MaaAdapterConfig.CurrentConfig.MaaDirectory))
                {
                    Validated = false;
                    return;
                }

                //读取maa的配置,找到所有的mode
                var maaCfgFile = new FileInfo(Path.Combine(MaaAdapterConfig.CurrentConfig.MaaDirectory,
                    "resource\\config.json"));
                if (!maaCfgFile.Exists)
                {
                    Validated = false;
                    return;
                }

                dynamic obj = JsonConvert.DeserializeObject(File.ReadAllText(maaCfgFile.FullName));

                List<string> names = new List<string>();
                foreach (var connection in obj.connection)
                {
                    string name = connection.configName;
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }


                Dispatcher.Invoke(() =>
                {
                    foreach (string name in names)
                    {
                        if (!cboAdbMode.Items.Contains(name))
                        {
                            cboAdbMode.Items.Add(name);
                        }
                    }

                    if (cboAdbMode.Text == "")
                    {
                        cboAdbMode.Text = MaaAdapterConfig.CurrentConfig.AdbConnectMode;
                    }

                    //检查文件是否存在
                    icoAdbValid.Kind = PackIconKind.Close;
                    icoAdbValid.Foreground = Brushes.Red;
                    try
                    {
                        FileInfo fi = new FileInfo(txtAdbFilePath.Text);
                        if (fi.Exists)
                        {
                            if (!String.IsNullOrWhiteSpace(txtAdbAddress.Text) &&
                                !string.IsNullOrWhiteSpace(cboAdbMode.Text))
                            {
                                icoAdbValid.Kind = PackIconKind.Check;
                                icoAdbValid.Foreground = Brushes.Green;

                                MaaAdapterConfig.CurrentConfig.AdbFilePath = txtAdbFilePath.Text;
                                MaaAdapterConfig.CurrentConfig.AdbAddress = txtAdbAddress.Text;
                                MaaAdapterConfig.CurrentConfig.AdbConnectMode = cboAdbMode.Text;

                                Validated = true;
                            }
                        }

                    }
                    catch (Exception)
                    {
                        //
                    }

                    MaaAdapterConfig.CurrentConfig.AdbTouchMode = cboTouchMode.Text;
                });
            }
            finally
            {
                configValiateTimer.Start();
            }
        }

        public bool Validated { get; set; }

        private void BtnBrowseAdb_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "adb.exe|adb.exe"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                txtAdbFilePath.Text = new FileInfo(dlg.FileName).FullName ?? "";
            }
        }
    }
}
