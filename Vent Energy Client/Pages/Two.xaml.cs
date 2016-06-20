using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using NLog;
using System.Threading;
using System.Globalization;

namespace ventEnergy.Pages
{
    /// <summary>
    /// Interaction logic for BasicPage1.xaml
    /// </summary>
    public partial class BasicPage1 : UserControl
    {

       // private bool Init = true;

        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private List<Vents> _ventsData = new List<Vents>();
        private VentsTools _vt;
        private IntPtr _fndHwnd;
        private Thread _fndThread;
        private Thread _fvdThread;
        private DateTime _date;
        private System.Threading.Timer _delayTimer;
        DateTime _firstMonth = DateTime.MinValue;
        DateTime _lastMonth = DateTime.MinValue;

        //свойство отвечает за визуализацию прогресс бара
        private bool PBPerform
        {
            set
            {
                if (value)
                    _delayTimer = new System.Threading.Timer((Object state) => Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = true; pb1.Visibility = Visibility.Visible; })), null, 1000, Timeout.Infinite);
                else
                {
                    Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
                    _delayTimer.Dispose();
                }
            }
        }

        public BasicPage1()
        {

            InitializeComponent();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("ru-RU");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ru-RU");


            
            #region datagrid config init
            //  dataG1.AutoGenerateColumns = false;
            //  dataG1.MaxHeight = 380;
            //  //dataG1.RowHeight = 46;
            //  dataG1.CanUserAddRows = false;
            //  dataG1.CanUserDeleteRows = false;
            //  dataG1.CanUserReorderColumns = false;
            //  dataG1.CanUserResizeColumns = false;
            //  dataG1.CanUserResizeRows = false;
            //  dataG1.CanUserSortColumns = false;

            //  dataG2.AutoGenerateColumns = false;
            //  dataG2.MaxHeight = 380;
            ////  dataG2.RowHeight = 46;
            //  dataG2.CanUserAddRows = false;
            //  dataG2.CanUserDeleteRows = false;
            //  dataG2.CanUserReorderColumns = false;
            //  dataG2.CanUserResizeColumns = false;
            //  dataG2.CanUserResizeRows = false;
            //  dataG2.CanUserSortColumns = false;

            //  dataG3.AutoGenerateColumns = false;
            //  dataG3.MaxHeight = 380;
            //  //dataG3.RowHeight = 46;
            //  dataG3.CanUserAddRows = false;
            //  dataG3.CanUserDeleteRows = false;
            //  dataG3.CanUserReorderColumns = false;
            //  dataG3.CanUserResizeColumns = false;
            //  dataG3.CanUserResizeRows = false;
            //  dataG3.CanUserSortColumns = false;
            #endregion
            _fvdThread = new Thread(() => StartInit());
            _fvdThread.Start();
            _fndHwnd = _fndHwnd = IntPtr.Zero;

            
        }

        /// <summary>
        /// Производит первоначальную инициализацию данных на странице
        /// </summary>
        private void StartInit()
        {
            //запустим таймер с задержкой в 1с для отображения прогрессбара (бесячий кругалек, когда все зависло)
            PBPerform = true;
            VentsTools.currentActionString = "Подключаюсь к Базе данных";

            _vt = new VentsTools();

            VentsDataCfg(_vt); //заполним данные по вентиляторам
            DatePickerInit(_vt);

            //отобразим наш листбокс
            Dispatcher.Invoke(new Action(() =>
            {
                VentsListBox.ItemsSource = _ventsData;
                VentsListBox.Focus();
                VentsListBox.SelectedItem = VentsListBox.Items.CurrentItem;
            }));

            PBPerform = false;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentThreadId();

        private void FillNewData(DateTime date)
        {
            try
            {
                var ValuesList = new List<DateValues>();

                VentsTools.currentActionString = "Подключаюсь к Базе данных";
                _fndHwnd = GetCurrentThreadId();
                //запустим таймер с задержкой в 1с для отображения прогрессбара (бесячий кругалек, когда все зависло)
                PBPerform = true;

                string name = (string)Dispatcher.Invoke(new Func<string>(()=> (VentsListBox.SelectedItem as Vents).name )); // возвращает название выбранного вентилятора
                bool RDVres = _vt.ReadDayValuesFromDB(VentsConst.connectionString, VentsConst._DATAtb, name, date, ref ValuesList);
                if (!RDVres)
                {
                    throw new Exception(String.Format("Не удалось получить ежедневные данные для {0} за {1}", (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr;})), (string)Dispatcher.Invoke(new Func<string>(delegate { return (string)dateP.DisplayDate.ToString(); }))));
                }
                //разобьем список на несколько по 8 итемов в каждом
                //int chunks = 3;
                int valuesLenght = ValuesList.Count();
                int chunkLenght = 8;// (int)Math.Ceiling(valuesLenght / (double)chunks);
                int chunks = (int)Math.Ceiling(valuesLenght / (double)chunkLenght);
                var parts = Enumerable.Range(0, chunks).Select(i => ValuesList.Skip(i * chunkLenght).Take(chunkLenght).ToList()).ToList();
                double blockWidth = (double)Dispatcher.Invoke(new Func<double> (() => workGrid.Width / chunks));
                double blockHeight = (double)Dispatcher.Invoke(new Func<double>(() => workGrid.Height / chunkLenght));

                blockWidth = 112.5; //as constant
                #region manually generated datagrid
                //Dispatcher.Invoke(new Action(delegate
                //{
                //    if (parts[0] != null)
                //    {
                //        dataG1.ItemsSource = parts[0];
                //    }
                //    if (parts[1] != null)
                //        dataG2.ItemsSource = parts[1];
                //    if (parts[2] != null)
                //        dataG3.ItemsSource = parts[2];
                //    if (parts[3] != null)
                //        dataG4.ItemsSource = parts[3];
                //}));
                #endregion

                #region autogenerated textboxes
                Dispatcher.Invoke(new Action(delegate
                {
                    workGrid.Children.Clear();
                    for (int i = 0; i < chunks; i++)
                    {
                        Label lbtime = new Label();
                        workGrid.Children.Add(lbtime);
                        lbtime.Content = "Дата";
                        lbtime.Width = 2 * blockWidth/5;
                        lbtime.Height = 45;
                        lbtime.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        lbtime.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        lbtime.VerticalContentAlignment = VerticalAlignment.Bottom;
                        lbtime.HorizontalContentAlignment = HorizontalAlignment.Center;
                        lbtime.FontSize = 14;
                        lbtime.FontWeight = FontWeights.Bold;
                        lbtime.BorderBrush = Brushes.Black;
                        // lbtime.BorderThickness = new Thickness(1.5);

                        Label lbvalue = new Label();
                        workGrid.Children.Add(lbvalue);
                        lbvalue.Content = "Энергия\r\n(МВтч)";
                        lbvalue.Width = 3 * blockWidth / 5;
                        lbvalue.Height = 45;
                        lbvalue.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                        lbvalue.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        lbvalue.VerticalContentAlignment = VerticalAlignment.Bottom;
                        lbvalue.HorizontalContentAlignment = HorizontalAlignment.Center;
                        lbvalue.FontSize = 12;
                        lbvalue.FontWeight = FontWeights.Bold;
                        lbvalue.BorderBrush = Brushes.Black;
                        //lbvalue.BorderThickness = new Thickness(1.5);


                        lbtime.Margin = new Thickness(lbtime.Width * i + lbvalue.Width * i, 0, 0, 0);
                        lbvalue.Margin = new Thickness(lbtime.Width + (lbtime.Width + lbvalue.Width) * i, 0, 0, 0);

                        List<DateValues> listDV = parts[i];

                        for (int j = 0; j < parts[i].Count; j++)
                        {

                            if (listDV == null)
                                break;
                            TextBox tbtime = new TextBox();
                            workGrid.Children.Add(tbtime);
                            tbtime.Text = listDV[j].date;
                            tbtime.Width = 2 * blockWidth / 5;
                            tbtime.Height = 40;
                            tbtime.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                            tbtime.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            tbtime.IsReadOnly = true;
                            tbtime.VerticalContentAlignment = VerticalAlignment.Center;
                            tbtime.HorizontalContentAlignment = HorizontalAlignment.Center;
                            tbtime.FontSize = 14;
                            tbtime.FontWeight = FontWeights.Medium;
                            var fdf = tbtime.Style;
                            tbtime.Style = (Style)tbtime.FindResource("TextBoxStyleBlueBackgrReadonly");


                            TextBox tbvalue = new TextBox();
                            workGrid.Children.Add(tbvalue);
                            tbvalue.Text = ((double)listDV[j].value / 10).ToString(); //данные записываются в БД в виде x*100 квтч. поделим на 10 чтобы получить Мвтч
                            tbvalue.Width = 3 * blockWidth / 5;
                            tbvalue.Height = 40;
                            tbvalue.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                            tbvalue.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                            tbvalue.IsReadOnly = true;
                            tbvalue.VerticalContentAlignment = VerticalAlignment.Center;
                            tbvalue.HorizontalContentAlignment = HorizontalAlignment.Center;
                            tbvalue.FontSize = 14;
                            tbvalue.FontWeight = FontWeights.Medium;
                            tbvalue.Style = (Style)tbtime.FindResource("TextBoxStyleNoShadow");

                            tbtime.Margin = new Thickness(tbtime.Width * i + tbvalue.Width * i, lbtime.Height + tbtime.Height * j, 0, 0);
                            tbvalue.Margin = new Thickness(tbtime.Width + (tbtime.Width + tbvalue.Width) * i, lbvalue.Height + tbvalue.Height * j, 0, 0);

                        }
                    }
                }));
                #endregion
                #region autogenerated datagrid
                //Dispatcher.Invoke(new Action(delegate
                //{
                //    workGrid.Children.Clear();
                //    for (int i = 0; i < 3;i++ )
                //    {
                //        List<DateValues> listDV = parts[i];
                //        DataGrid dataGrid = new DataGrid();
                //        dataGrid.AutoGenerateColumns = true;
                //        dataGrid.MaxHeight = 380;
                //        //dataGrid.MaxWidth = 140;
                //        dataGrid.Width = 156;
                //        dataGrid.MaxWidth = 156;
                //        dataGrid.Margin = new Thickness(200 + dataGrid.Width * i, 59, 0, 0);
                //        dataGrid.RowHeight = 30;
                //        dataGrid.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                //        dataGrid.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                //        dataGrid.CanUserAddRows = false;
                //        dataGrid.CanUserDeleteRows = false;
                //        dataGrid.CanUserReorderColumns = false;
                //        dataGrid.CanUserResizeColumns = false;
                //        dataGrid.CanUserResizeRows = false;
                //        dataGrid.CanUserSortColumns = false;
                //        dataGrid.IsReadOnly = true;
                //        dataGrid.IsHitTestVisible = false;
                //        dataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
                //        dataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
                //       // dataGrid.HorizontalGridLinesBrush = new SolidColorBrush(Color.FromRgb(255,255,255));
                //        dataGrid.ItemsSource = listDV;
                //        dataGrid.AutoGeneratingColumn += new EventHandler<DataGridAutoGeneratingColumnEventArgs>(OnAutoGeneratingColumn);
                //        // var trtr = dataGrid.ColumnWidth;
                //        workGrid.Children.Add(dataGrid);
                //    }

                //}));
                #endregion

                //calculate monthly electric power expense
                int monthlyExpense = 0;
                if (date != _firstMonth)
                {
                    bool daylyRes = _vt.ReadMonthlyExpenseFromDB(VentsConst.connectionString, VentsConst._DATAtb, name, date, ref monthlyExpense);
                    if (!daylyRes)
                    {
                        throw new Exception(String.Format("Не удалось получить  данные для месячного расхода {0} за {1}", (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr; })), (string)Dispatcher.Invoke(new Func<string>(delegate { return (string)dateP.DisplayDate.ToString(); }))));
                    }
                }
                else
                {
                    monthlyExpense = ValuesList.Last<DateValues>().value - ValuesList.First<DateValues>().value;
                }
                monthlyExpense = monthlyExpense * 100;
                Dispatcher.Invoke(new Action(delegate { totaltb.Text = String.Format("Расход {0} за {1} равен {2} кВтч", (VentsListBox.SelectedItem as Vents).descr, date.GetDateTimeFormats('O')[0].Substring(0, 8), monthlyExpense.ToString()); }));

                //generate current action string and update content of main window textbox
                string descr = (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr; }));

                VentsTools.currentActionString = String.Format("Показания за  {0} {1}", date.Date.GetDateTimeFormats('D', CultureInfo.CreateSpecificCulture("ru-ru"))[0], descr);

                PBPerform = false;
                _fndHwnd = IntPtr.Zero;

            }
            catch (Exception ex)
            {
                VentsTools.currentActionString = "Не удалось подключиться к базе данных";
                _log.Error(ex.Message);
                PBPerform = false;
                _fndHwnd = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Заполняет список <see cref="_ventsData"/> из таблицы конфигурации в базе данных. Ожидает подключения к базе данных, если такового нет.
        /// </summary>
        /// <param name="vt"><see cref="VentsTools"/></param>
        private void VentsDataCfg(VentsTools vt)
        {
            bool connectionState = false;

            while (!connectionState)
            {
                connectionState = _vt.FillVentsData(ref _ventsData);

                if (!connectionState && this.IsVisible)
                {
                    VentsTools.currentActionString = "Нет подключения к базе данных, проверьте настройки сети";
                    Thread.Sleep(3000);
                }
                if (!this.IsVisible)
                    _fvdThread.Suspend();
            }
        }

        /// <summary>
        /// Определяет временные рамки для <see cref="dateP"/>
        /// </summary>
        /// <param name="vt"></param>
        private void DatePickerInit(VentsTools vt)
        {
            VentsTools.currentActionString = "Показания за месяц";
            //получим самую первую дату записи в БД, преобразуем её первым днем месяца
            if (!_vt.ReadOneDate(ref _firstMonth, QueuePosition.First, VentsConst.connectionString, VentsConst._DATAtb))
                _log.Warn("Не удалось получить самую первую дату");
            int days = _firstMonth.Day;
            TimeSpan ts = new TimeSpan(days - 1, 0, 0, 0);
            _firstMonth = _firstMonth.Date - ts;
            Dispatcher.Invoke(new Action(() => dateP.DisplayDateStart = _firstMonth));
            //тоже самое с последней датой, преобразовывать не обязательно
            _lastMonth = DateTime.MaxValue;
            if (!_vt.ReadOneDate(ref _lastMonth, QueuePosition.Last, VentsConst.connectionString, VentsConst._DATAtb))
                _log.Warn("Не удалось получить последнюю дату");
            Dispatcher.Invoke(new Action(() => dateP.DisplayDateEnd = _lastMonth));
            Dispatcher.Invoke(new Action(() => dateP.IsEnabled = true));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

        //[System.Runtime.InteropServices.DllImport("kernel32.dll")]
        //static extern bool TerminateThread(IntPtr hThread, uint dwExitCode);
        private void dateP_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsVisible)
                return;
            _date = (DateTime)e.AddedItems[0];
            var dp = sender as DatePicker;
           // DependencyProperty startDate = DatePicker.DisplayDateStartProperty;
            try
            {
                if (_fndHwnd != IntPtr.Zero)
                {
                    //  TerminateThread(FNDhwnd, 1);
                    _fndThread.Abort();
                }
            }
            catch
            {
                _log.Error("Не могу закрыть поток FNDthread {0}", _fndHwnd.ToString());
            }

            if (VentsListBox.SelectedItem != null)
            {
                _fndThread = new Thread(() => FillNewData(_date));
                _fndThread.Start();
                _fndHwnd = (IntPtr)_fndThread.ManagedThreadId;
            }
        }

        private void VentsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VentsListBox.IsFocused)
            {
                workGrid.Children.Clear();
                return;
            }
            try
            {
                if (_fndHwnd != IntPtr.Zero)
                {
                    _fndThread.Abort();
                }
            }
            catch
            {
                _log.Error("Не могу закрыть поток FNDthread {0}", _fndHwnd.ToString());
            }

            if (VentsListBox.SelectedItem != null && dateP.SelectedDate != null)
            {
                _fndThread = new Thread(() => FillNewData(_date));
                _fndThread.Start();
                _fndHwnd = (IntPtr)_fndThread.ManagedThreadId;
            }

        }


        private void calend_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {

        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_fvdThread.ThreadState == System.Threading.ThreadState.Suspended)
                _fvdThread.Resume();
            if (this.IsVisible &&  _fvdThread.ThreadState == System.Threading.ThreadState.Stopped)
                VentsTools.currentActionString = "Показания за месяц";
        }
        //private void _calendar_previewmouseup(object sender, mousebuttoneventargs e)
        //{
        //    if (mouse.captured is calendaritem)
        //    {
        //        mouse.capture(null);
        //    }
        //}
    }

}
