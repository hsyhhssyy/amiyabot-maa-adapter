using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using AmiyaBotMaaAdapter.Helpers;
using AmiyaBotMaaAdapter.Interop;
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

        private void callback(int msg, string details_json, IntPtr custom_arg)
        {
            
        }

        private void BtnGenerateSignature_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
