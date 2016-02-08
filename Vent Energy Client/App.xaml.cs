using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using Hardcodet.Wpf.TaskbarNotification;
using System.Diagnostics;

namespace ventEnergy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static string[] startupArguments;
        public TaskbarIcon notifyIcon;

        private void Application_startup(object sender, StartupEventArgs e)
        {
            startupArguments = e.Args;
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            Process[] pr = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if(pr.Length >1 )
               System.Diagnostics.Process.GetCurrentProcess().Kill();
            base.OnStartup(e);
            EventManager.RegisterClassHandler(typeof(DatePicker),
                DatePicker.LoadedEvent,
                new RoutedEventHandler(DatePicker_Loaded));
            //create the notifyicon (it's a resource declared in smNotify.xaml
            notifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
        }
            public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }
        void DatePicker_Loaded(object sender, RoutedEventArgs e)
        {
            var dp = sender as DatePicker;
            if (dp == null) return;

            var tb = GetChildOfType<DatePickerTextBox>(dp);
            if (tb == null) return;

            var wm = tb.Template.FindName("PART_Watermark", tb) as ContentControl;
            if (wm == null) return;

            wm.Content = "Выберите дату";
        }
        protected override void OnExit(ExitEventArgs e)
        {
            notifyIcon.Dispose(); //the icon would clean up automatically, but this is cleaner
            base.OnExit(e);
            System.Threading.Timer closeTimer = new System.Threading.Timer((object state) => System.Diagnostics.Process.GetCurrentProcess().Kill(),null,10000, System.Threading.Timeout.Infinite);
        }
    }
}
// ^ вся эта хрень выше для изменения английской надписи в Календарях