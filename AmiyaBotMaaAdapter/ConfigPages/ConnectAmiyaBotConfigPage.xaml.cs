using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows;
using AmiyaBotMaaAdapter.Helpers;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;

namespace AmiyaBotMaaAdapter
{
    /// <summary>
    /// ConnectAmiyaBotConfigPage.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectAmiyaBotConfigPage : UserControl
    {
        public ConnectAmiyaBotConfigPage()
        {
            InitializeComponent();

            txtAmiyaBotAddress.Text = MaaAdapterConfig.CurrentConfig.Server;

            if (MaaAdapterConfig.CurrentConfig.Uuid == null || !Guid.TryParse(MaaAdapterConfig.CurrentConfig.Uuid, out _))
            {
                MaaAdapterConfig.CurrentConfig.Uuid = Guid.NewGuid().ToString("D");
            }

            if (MaaAdapterConfig.CurrentConfig.Secret == null || !Guid.TryParse(MaaAdapterConfig.CurrentConfig.Secret, out _))
            {
                MaaAdapterConfig.CurrentConfig.Secret = Guid.NewGuid().ToString("D");
            }

            if (MaaAdapterConfig.CurrentConfig.Signature != null)
            {
                dynamic newData = new
                {
                    signature = MaaAdapterConfig.CurrentConfig.Signature,
                };

                txtSignatureValue.Text = "兔兔记录MAA密钥" + JsonConvert.SerializeObject(newData, new JsonSerializerSettings
                {
                    Formatting = Formatting.None
                });
            }

            icoAddressValid.Kind = PackIconKind.Close;
            icoAddressValid.Foreground = Brushes.Red;

            if (!String.IsNullOrWhiteSpace(txtAmiyaBotAddress.Text))
            {
                var server = txtAmiyaBotAddress.Text;
                this.IsEnabled = false;

                Task.Run(() =>
                {
                    while (true)
                    {
                        try
                        {
                            //TestServer
                            var error = HttpHelper.PostAction(server + "/maa/login",
                                JsonConvert.SerializeObject(new Dictionary<string, string>()
                                {
                                    { "uuid", MaaAdapterConfig.CurrentConfig.Uuid },
                                    { "signature", MaaAdapterConfig.CurrentConfig.Signature }
                                })).GetResponseData(out _);

                            if (error == null)
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    icoAddressValid.Kind = PackIconKind.Check;
                                    icoAddressValid.Foreground = Brushes.Green;
                                    Validated = true;
                                });

                                Logger.Current.Info("成功登录到AmiyaBot Server");
                            }else
                            {
                                Logger.Current.Info($"登录AmiyaBot Server失败,错误{error}");
                            }
                        }
                        catch (Exception ex)
                        {
                            //
                        }
                        finally
                        {
                            Dispatcher.Invoke(() => { this.IsEnabled = true; });
                        }

                        if (Validated)
                        {
                            Thread.Sleep(1000*60*10);
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                });
            }
        }

        public bool Validated { get; set; }

        private void BtnTest_OnClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void BtnConnect_OnClick(object sender, RoutedEventArgs e)
        {
            MaaAdapterConfig.CurrentConfig.Server = txtAmiyaBotAddress.Text;

            if (!String.IsNullOrWhiteSpace(MaaAdapterConfig.CurrentConfig.Signature))
            {
                if (MessageBox.Show("生成新的密钥会使旧的密钥失效，确定要生成新密钥吗？", "生成密钥", MessageBoxButton.YesNo) !=
                    MessageBoxResult.Yes)
                {
                    return;
                }
            }

            try
            {
                this.IsEnabled = false;
                MaaAdapterConfig.CurrentConfig.Uuid = Guid.NewGuid().ToString("D");
                MaaAdapterConfig.CurrentConfig.Secret = Guid.NewGuid().ToString("D");

                var signature = MaaAdapterHttpHelper.GenerateSignature();


                if (!String.IsNullOrWhiteSpace(signature))
                {
                    dynamic newData = new
                    {
                        signature = MaaAdapterConfig.CurrentConfig.Signature,
                    };

                    txtSignatureValue.Text = "兔兔记录MAA密钥" + JsonConvert.SerializeObject(newData,
                        new JsonSerializerSettings
                        {
                            Formatting = Formatting.None
                        });

                    icoAddressValid.Kind = PackIconKind.Check;
                    icoAddressValid.Foreground = Brushes.Green;
                    Validated = true;

                    Logger.Current.Info($"密钥已更新:{MaaAdapterConfig.CurrentConfig.Signature}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("获取密钥出错，错误原因：" + ex.Message);
            }
            finally
            {
                this.IsEnabled = true;
            }
        }

        private void BtnCopyToClipboard_OnClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtSignatureValue.Text);
            MessageBox.Show("消息已复制到剪贴板，快去兔兔群里粘贴吧。");
        }
    }
}
