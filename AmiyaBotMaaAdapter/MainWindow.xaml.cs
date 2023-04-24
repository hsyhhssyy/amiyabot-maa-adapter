using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using AmiyaBotMaaAdapter.Helpers;
using AmiyaBotMaaAdapter.Interop;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace AmiyaBotMaaAdapter
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            Logger.Current.OnLog += CurrentLogger_OnLog;

            txtServer.Text = MaaAdapter.CurrentAdapter.Server;
            dynamic newData = new
            {
                signature = MaaAdapter.CurrentAdapter.Signature,
            };

            txtSignature.Text = JsonConvert.SerializeObject(newData, new JsonSerializerSettings
            {
                Formatting = Formatting.None
            });

            txtResources.Text = MaaAdapter.CurrentAdapter.Resources;

            MaaAdapter.CurrentAdapter.StartListen();
        }

        private void CurrentLogger_OnLog(object sender, Logger.OnLogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                txtLogs.Text += $"[{e.DateTime:s}][{e.Level,-10}]{e.Message}" + Environment.NewLine;
            });
        }
        
        private void BtnGenerateSignature_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(MaaAdapter.CurrentAdapter.Signature))
            {
                if (MessageBox.Show("生成新的密钥会使旧的密钥失效，确定要生成新密钥吗？", "生成密钥", MessageBoxButton.YesNo) !=
                    MessageBoxResult.Yes)
                {
                    return;
                }
            }

            var signature = MaaAdapter.CurrentAdapter.GenerateSignature();

            if (signature == null)
            {
                txtSignature.Text = "";
                return;
            }

            dynamic newData = new
            {
                signature = signature
            };

            txtSignature.Text = JsonConvert.SerializeObject(newData, new JsonSerializerSettings
            {
                Formatting = Formatting.None
            });

            MaaAdapter.CurrentAdapter.StartListen();
        }

        private void TxtServer_TextChanged(object sender, TextChangedEventArgs e)
        {
            MaaAdapter.CurrentAdapter.Server = txtServer.Text;
        }


        private void TxtResources_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            MaaAdapter.CurrentAdapter.Resources = txtResources.Text;
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
                txtResources.Text = new FileInfo(dlg.FileName).Directory?.FullName??"";
            }
        }
    }
}
