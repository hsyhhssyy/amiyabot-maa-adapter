using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AmiyaBotMaaAdapter.Helpers;
using MaterialDesignThemes.Wpf;

namespace AmiyaBotMaaAdapter
{
    /// <summary>
    /// WizardStyleMainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class WizardStyleMainWindow : Window
    {
        private readonly Timer configValidateTimer;

        public WizardStyleMainWindow()
        {
            InitializeComponent();

            configValidateTimer = new Timer();
            configValidateTimer.AutoReset = true;
            configValidateTimer.Elapsed += ConfigValidateTimer_Elapsed;
            configValidateTimer.Interval = 200;
            configValidateTimer.Start();

            Logger.Current.OnLog += CurrentLogger_OnLog;
        }

        private void CurrentLogger_OnLog(object sender, Logger.OnLogEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                //var border = (Border)VisualTreeHelper.GetChild(txtLogs, 0);
                //var scrollViewer = (ScrollViewer)VisualTreeHelper.GetChild(border, 0);
                //bool isAtBottom = scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight;

                txtLogs.Text += $"[{e.DateTime:s}][{e.Level,-10}]{e.Message}" + Environment.NewLine;
                txtLogs.ScrollToEnd();
                //if (isAtBottom)
                //{
                //    scrollViewer.ScrollToEnd();
                //}
            });
        }

        private void ConfigValidateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            configValidateTimer.Stop();

            try
            {

                Dispatcher.Invoke(() =>
                {
                    var validated = true;
                    if (usrConnectMAAConfigPage.Validated)
                    {
                        icoConnectMAA.Kind = PackIconKind.CheckCircle;
                        icoConnectMAA.Foreground = Brushes.Green;
                    }
                    else
                    {
                        icoConnectMAA.Kind = PackIconKind.AlertCircle;
                        icoConnectMAA.Foreground = Brushes.Red;
                        validated = false;
                    }

                    if (usrConnectAmiyaBot.Validated)
                    {
                        icoConnectAmiyaBot.Kind = PackIconKind.CheckCircle;
                        icoConnectAmiyaBot.Foreground = Brushes.Green;
                    }
                    else
                    {
                        icoConnectAmiyaBot.Kind = PackIconKind.AlertCircle;
                        icoConnectAmiyaBot.Foreground = Brushes.Red;
                        validated = false;
                    }


                    if (usrMaaConfig.Validated)
                    {
                        icoMaaConfig.Kind = PackIconKind.CheckCircle;
                        icoMaaConfig.Foreground = Brushes.Green;
                    }
                    else
                    {
                        icoMaaConfig.Kind = PackIconKind.AlertCircle;
                        icoMaaConfig.Foreground = Brushes.Red;
                        validated = false;
                    }

                    if (validated)
                    {
                        MaaAdapter.CurrentAdapter.Load();
                    }
                    else
                    {
                        MaaAdapter.CurrentAdapter.Stop();
                    }
                });
            }
            finally
            {
                configValidateTimer.Start();
            }
        }
    }
}
