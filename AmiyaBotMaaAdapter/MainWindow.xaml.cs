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
using AmiyaBotMaaAdapter.Interop;

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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //AstInterop.AsstSetUserDir("")
            AsstInterop.AsstLoadResource("E:\\Tools\\MAA");
            //var handle = AstInterop.AsstCreateEx(callback, IntPtr.Zero);
            var handle = AsstInterop.AsstCreate();
            AsstInterop.AsstSetInstanceOption(handle, (int)InstanceOptionType.touch_type, "adb");
            var success =
                AsstInterop.AsstConnect(handle, "E:\\LeiDian\\LDPlayer9\\adb.exe", "emulator-5556", "LDPlayer");
            if (success)
            {
                AsstInterop.AsstAppendTask(handle, "Fight", "{\"stage\": \"1-7\"}");
                AsstInterop.AsstStart(handle);
            }

        }

        private void callback(int msg, string details_json, IntPtr custom_arg)
        {
            
        }
    }
}
