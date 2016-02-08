using System;
using System.Windows;

namespace ventEnergy
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class LogoWindow : Window
    {
        System.Threading.Timer delayTimer;
        public LogoWindow(MainWindow mw)
        {
            InitializeComponent();
            delayTimer = new System.Threading.Timer((Object state) =>
            {
                Dispatcher.Invoke(new Action(delegate
                    {
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window.GetType() == typeof(MainWindow))
                            {
                                (window as MainWindow).Visibility = Visibility.Visible;
                            }
                        }
                        this.Close();
                    }));
                
            }, null, 5000, System.Threading.Timeout.Infinite);

            //System.Threading.Thread.Sleep(6000);
            //
            //
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            delayTimer.Dispose();
        }
    }
}
