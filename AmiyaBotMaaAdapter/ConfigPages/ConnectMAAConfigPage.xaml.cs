using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace AmiyaBotMaaAdapter
{
    /// <summary>
    /// ConnectMAAConfigPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectMAAConfigPage : UserControl
    {
        private readonly Timer configValiateTimer;

        public ConnectMAAConfigPage()
        {
            InitializeComponent();

            configValiateTimer = new Timer();
            configValiateTimer.AutoReset = true;
            configValiateTimer.Elapsed += ConfigValidateTimer_Elapsed;
            configValiateTimer.Interval = 200;
            configValiateTimer.Start();

            txtMaaDirectory.Text = MaaAdapterConfig.CurrentConfig.MaaDirectory;
        }

        public bool Validated { get; set; }

        private void ConfigValidateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            configValiateTimer.Stop();

            try
            {
                Dispatcher.Invoke(() =>
                {
                    //检查文件是否存在
                    icoDirectoryValid.Kind = PackIconKind.Close;
                    icoDirectoryValid.Foreground = Brushes.Red;
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(txtMaaDirectory.Text);
                        if (di.Exists)
                        {
                            FileInfo fi = new FileInfo(Path.Combine(di.FullName, "MaaCore.dll"));
                            if (fi.Exists)
                            {
                                icoDirectoryValid.Kind = PackIconKind.Check;
                                icoDirectoryValid.Foreground = Brushes.Green;
                                Validated = true;
                            }
                        }
                    }
                    catch (Exception)
                    {
                        //
                    }
                });
            }
            finally
            {
                configValiateTimer.Start();
            }
        }

        private void BtnBrowse_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "MaaCore.dll|MaaCore.dll"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                txtMaaDirectory.Text = new FileInfo(dlg.FileName).Directory?.FullName ?? "";
            }
        }

        private void TxtMaaDirectory_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            MaaAdapterConfig.CurrentConfig.MaaDirectory = txtMaaDirectory.Text;
        }
    }
}
