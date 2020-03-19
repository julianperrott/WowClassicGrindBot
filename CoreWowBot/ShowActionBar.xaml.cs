using Libs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;
using System.Timers;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Extensions.Logging;

namespace Powershell
{
    /// <summary>
    /// Interaction logic for ShowActionBar.xaml
    /// </summary>
    public partial class ShowActionBar : Window, ILogger, IDisposable
    {
        public ActionBarStatus timeNow = new ActionBarStatus("Time");

        public ActionBarStatus isUsableActionUsable_1 = new ActionBarStatus("isUsable 1-24");
        public ActionBarStatus isUsableActionUsable_2 = new ActionBarStatus("isUsable 25-48");
        public ActionBarStatus isUsableActionUsable_3 = new ActionBarStatus("isUsable 49-72");
        public ActionBarStatus isUsableActionUsable_4 = new ActionBarStatus("isUsable 73-96");

        private WowData addonThread;

        List<ActionBarStatus> items = new List<ActionBarStatus>();

        public ShowActionBar()
        {
            InitializeComponent();

            items = new List<ActionBarStatus>()
            {
                timeNow,
                isUsableActionUsable_1,
                isUsableActionUsable_2,
                isUsableActionUsable_3,
                isUsableActionUsable_4
            };

            McDataGrid.ItemsSource = items;


            var colorReader = new WowScreen();

            var config = new DataFrameConfiguration(colorReader);

            var frames = config.ConfigurationExists()
                ? config.LoadConfiguration()
                : config.CreateConfiguration(WowScreen.GetAddonBitmap());

            

            this.addonThread = new WowData(colorReader, frames, this);

            Record_Click(this, new RoutedEventArgs());
        }

        private System.Timers.Timer? timer;
        private List<WowPoint> locations = new List<WowPoint>();

        private void Record_Click(object sender, RoutedEventArgs e)
        {
            if (timer != null)
            {
                timer.Enabled = false;
                timer.Dispose();
                timer = null;
            }
            else
            {
                timer = new System.Timers.Timer(500);
                timer.Elapsed += OnTimedEvent;
                timer.AutoReset = true;
                timer.Enabled = true;

                RefreshData();
            }
        }

        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            RefreshData();
        }

        private void RefreshData()
        {
            this.addonThread.AddonReader.Refresh();

            isUsableActionUsable_1.value = this.addonThread.PlayerReader.ActionBarUseable_1To24.value;
            isUsableActionUsable_2.value = this.addonThread.PlayerReader.ActionBarUseable_25To48.value;
            isUsableActionUsable_3.value = this.addonThread.PlayerReader.ActionBarUseable_49To72.value;
            isUsableActionUsable_4.value = this.addonThread.PlayerReader.ActionBarUseable_73To96.value;

            timeNow.name = DateTime.Now.ToString("HH:mm:ss");

            Application.Current.Dispatcher.Invoke(new Action(() => { McDataGrid.Items.Refresh(); }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            RefreshData();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //throw new NotImplementedException();
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return false;
            //throw new NotImplementedException();
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
            //throw new NotImplementedException();
        }

        public void Dispose()
        {
            
        }
    }
}
