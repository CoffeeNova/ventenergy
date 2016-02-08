using FirstFloor.ModernUI.Windows.Controls;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using NLog;
using NWTweak;


namespace ventEnergy
{   
    
    delegate bool Bbking();
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ModernWindow
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
       
        public MainWindow()
        {
            bool startWithLogo = true;

            InitializeComponent();
            onChangeNavigateLink += gotoNextContent;  
            

            foreach (string arg in App.startupArguments)
            {
                switch (arg)
                {
                    case "-nologo":
                        startWithLogo = false;
                        break;
                }
            }

            bool tempLogo = true;
            if (RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, "StartLogoCHKB") == null)
            {
                tempLogo=true;
            }
            else
            {
                if (RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, "StartLogoCHKB").ToString() == "")
                    tempLogo= true;
                else
                    tempLogo= false;
            }

            if (startWithLogo && tempLogo)
            {
                LogoWindow LW = new LogoWindow(main);
                LW.ShowInTaskbar = false;
                //Uri iconUri = new Uri("pack://application:,,,,Icons/belsolodLogo.ico", UriKind.RelativeOrAbsolute);
                //LW.Icon = BitmapFrame.Create(iconUri);
                LW.Show();
            }
            else
                main.Visibility = Visibility.Visible;

            VentsTools.onChangeActionString += this.UpdateMainLinkGroup;

            VentsTools.currentActionString = "";
            PathGeometry p1 = new PathGeometry();
            PathGeometry p2 = new PathGeometry();
            PathGeometry p3 = new PathGeometry();
            PathGeometry p4 = new PathGeometry();
            PathGeometry p5 = new PathGeometry();
           // Path p6 = new Path();
            p1.AddGeometry(Geometry.Parse("M 0.00 0.00 L 83.00 0.00 L 83.00 77.00 L 0.00 77.00 L 0.00 0.00 M 39.23 5.21 C 34.51 6.32 32.01 12.80 35.61 16.40 C 38.69 20.70 45.89 19.57 47.75 14.70 C 50.19 9.57 44.54 3.42 39.23 5.21 M 19.54 50.14 C 22.69 58.90 30.27 66.88 40.00 67.47 C 40.00 62.86 40.01 58.26 40.00 53.66 C 36.61 53.02 33.37 51.41 31.24 48.64 C 24.19 39.84 25.15 25.06 34.45 18.18 C 31.95 15.45 31.32 11.70 32.19 8.15 C 18.07 16.32 13.93 35.56 19.54 50.14 M 49.91 8.41 C 50.59 11.93 49.96 15.57 47.47 18.26 C 56.72 25.09 57.63 39.78 50.70 48.55 C 48.66 51.29 45.48 52.87 42.22 53.63 C 42.22 58.21 42.26 62.79 42.18 67.38 C 48.76 66.86 54.67 62.79 58.31 57.40 C 62.61 51.44 64.32 44.07 64.63 36.83 C 61.99 35.78 59.31 34.81 56.74 33.58 C 59.19 32.32 61.63 31.05 64.01 29.67 C 62.18 21.15 57.63 12.88 49.91 8.41 M 35.87 21.10 C 35.89 30.40 35.87 39.70 35.87 49.00 C 39.40 48.96 42.92 48.97 46.45 49.00 C 46.48 39.69 46.47 30.37 46.45 21.05 C 43.14 22.84 39.16 23.06 35.87 21.10 Z"));
            //p1.Data = Geometry.Parse("M 0.00 0.00 L 83.00 0.00 L 83.00 77.00 L 0.00 77.00 L 0.00 0.00 M 39.23 5.21 C 34.51 6.32 32.01 12.80 35.61 16.40 C 38.69 20.70 45.89 19.57 47.75 14.70 C 50.19 9.57 44.54 3.42 39.23 5.21 M 19.54 50.14 C 22.69 58.90 30.27 66.88 40.00 67.47 C 40.00 62.86 40.01 58.26 40.00 53.66 C 36.61 53.02 33.37 51.41 31.24 48.64 C 24.19 39.84 25.15 25.06 34.45 18.18 C 31.95 15.45 31.32 11.70 32.19 8.15 C 18.07 16.32 13.93 35.56 19.54 50.14 M 49.91 8.41 C 50.59 11.93 49.96 15.57 47.47 18.26 C 56.72 25.09 57.63 39.78 50.70 48.55 C 48.66 51.29 45.48 52.87 42.22 53.63 C 42.22 58.21 42.26 62.79 42.18 67.38 C 48.76 66.86 54.67 62.79 58.31 57.40 C 62.61 51.44 64.32 44.07 64.63 36.83 C 61.99 35.78 59.31 34.81 56.74 33.58 C 59.19 32.32 61.63 31.05 64.01 29.67 C 62.18 21.15 57.63 12.88 49.91 8.41 M 35.87 21.10 C 35.89 30.40 35.87 39.70 35.87 49.00 C 39.40 48.96 42.92 48.97 46.45 49.00 C 46.48 39.69 46.47 30.37 46.45 21.05 C 43.14 22.84 39.16 23.06 35.87 21.10 Z");
            //p1.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFFFF"));
            //p2.Data = Geometry.Parse("M 39.23 5.21 C 44.54 3.42 50.19 9.57 47.75 14.70 C 45.89 19.57 38.69 20.70 35.61 16.40 C 32.01 12.80 34.51 6.32 39.23 5.21 Z");
            p2.AddGeometry(Geometry.Parse("M 39.23 5.21 C 44.54 3.42 50.19 9.57 47.75 14.70 C 45.89 19.57 38.69 20.70 35.61 16.40 C 32.01 12.80 34.51 6.32 39.23 5.21 Z"));
            //p2.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFE8541"));
           // p3.Data = Geometry.Parse("M 35.87 21.10 C 39.16 23.06 43.14 22.84 46.45 21.05 C 46.47 30.37 46.48 39.69 46.45 49.00 C 42.92 48.97 39.40 48.96 35.87 49.00 C 35.87 39.70 35.89 30.40 35.87 21.10 Z");
            p3.AddGeometry(Geometry.Parse("M 35.87 21.10 C 39.16 23.06 43.14 22.84 46.45 21.05 C 46.47 30.37 46.48 39.69 46.45 49.00 C 42.92 48.97 39.40 48.96 35.87 49.00 C 35.87 39.70 35.89 30.40 35.87 21.10 Z"));
            // p3.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFE8541"));
            //p4.Data = Geometry.Parse("M 19.54 50.14 C 13.93 35.56 18.07 16.32 32.19 8.15 C 31.32 11.70 31.95 15.45 34.45 18.18 C 25.15 25.06 24.19 39.84 31.24 48.64 C 33.37 51.41 36.61 53.02 40.00 53.66 C 40.01 58.26 40.00 62.86 40.00 67.47 C 30.27 66.88 22.69 58.90 19.54 50.14 Z");
            p4.AddGeometry(Geometry.Parse("M 19.54 50.14 C 13.93 35.56 18.07 16.32 32.19 8.15 C 31.32 11.70 31.95 15.45 34.45 18.18 C 25.15 25.06 24.19 39.84 31.24 48.64 C 33.37 51.41 36.61 53.02 40.00 53.66 C 40.01 58.26 40.00 62.86 40.00 67.47 C 30.27 66.88 22.69 58.90 19.54 50.14 Z"));
           // p4.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF038244"));
            //p5.Data = Geometry.Parse("M 49.91 8.41 C 57.63 12.88 62.18 21.15 64.01 29.67 C 61.63 31.05 59.19 32.32 56.74 33.58 C 59.31 34.81 61.99 35.78 64.63 36.83 C 64.32 44.07 62.61 51.44 58.31 57.40 C 54.67 62.79 48.76 66.86 42.18 67.38 C 42.26 62.79 42.22 58.21 42.22 53.63 C 45.48 52.87 48.66 51.29 50.70 48.55 C 57.63 39.78 56.72 25.09 47.47 18.26 C 49.96 15.57 50.59 11.93 49.91 8.41 Z");
            p5.AddGeometry(Geometry.Parse("M 49.91 8.41 C 57.63 12.88 62.18 21.15 64.01 29.67 C 61.63 31.05 59.19 32.32 56.74 33.58 C 59.31 34.81 61.99 35.78 64.63 36.83 C 64.32 44.07 62.61 51.44 58.31 57.40 C 54.67 62.79 48.76 66.86 42.18 67.38 C 42.26 62.79 42.22 58.21 42.22 53.63 C 45.48 52.87 48.66 51.29 50.70 48.55 C 57.63 39.78 56.72 25.09 47.47 18.26 C 49.96 15.57 50.59 11.93 49.91 8.41 Z"));
            //p5.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF038244"));
            p1.FillRule = FillRule.Nonzero;
            p2.FillRule = FillRule.Nonzero;
            p3.FillRule = FillRule.Nonzero;
            p4.FillRule = FillRule.Nonzero;
            p5.FillRule = FillRule.Nonzero;


            Geometry g1 = Geometry.Combine(p2, p3, GeometryCombineMode.Union, null);
            Geometry g2 = Geometry.Combine(g1, p4, GeometryCombineMode.Union, null);
            Geometry g3 = Geometry.Combine(g2, p5, GeometryCombineMode.Union, null);

            main.LogoData = g3;
        }
        private delegate void tagDel(Uri c);
        private static event tagDel onChangeNavigateLink;
        private static Uri navigateLnk = new Uri("", UriKind.Relative);

        public static Uri navigateLink
        {
            get { return navigateLnk; }
            set 
            {
                if (!value.Equals(navigateLnk))
                {
                    navigateLnk = value;
                    onChangeNavigateLink(navigateLnk);
                }
            }
        }
        private void gotoNextContent(Uri content)
        {
            main.ContentSource = content;
        }
        private void UpdateMainLinkGroup(string currentAction)
        {
            this.mainLinkGroup.DisplayName = currentAction;

        } 

        private void main_Closed(object sender, EventArgs e)
        {
            //Application ap = Application.Current;
            //ap.Shutdown();
            ////System.Threading.Thread.Sleep(3000);
            //System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void main_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void main_Closed_1(object sender, EventArgs e)
        {

        }
    }

}
