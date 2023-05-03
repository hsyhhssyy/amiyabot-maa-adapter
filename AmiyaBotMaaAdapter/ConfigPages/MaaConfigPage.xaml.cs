using System;
using System.Collections.Generic;
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

namespace AmiyaBotMaaAdapter
{
    /// <summary>
    /// MaaConfigPage.xaml 的交互逻辑
    /// </summary>
    public partial class MaaConfigPage : UserControl
    {
        private readonly Timer configValiateTimer;

        public MaaConfigPage()
        {
            InitializeComponent();

            configValiateTimer = new Timer();
            configValiateTimer.AutoReset = true;
            configValiateTimer.Elapsed += ConfigValidateTimer_Elapsed;
            configValiateTimer.Interval = 200;
            configValiateTimer.Start();
        }

        private void ConfigValidateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            configValiateTimer.Stop();

            try
            {
                this.Validated = usrAdbTab.Validated;
            }
            finally
            {
                configValiateTimer.Start();
            }
        }

        public bool Validated { get; set; }

        private void BtnBrowseAdb_OnClick(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
