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
using ventEnergy.Extensions;
using ventEnergy.VEControls;

namespace ventEnergy.Pages
{
    /// <summary>
    /// Interaction logic for Home.xaml
    /// </summary>
    public partial class Home : UserControl
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        private List<Vents> _ventsData = new List<Vents>();
        private VentsTools _vt;
        private IntPtr _fndHwnd;
        private Thread _fndThread;
        private Thread _fvdThread;
        private DateTime _date;
        private System.Threading.Timer _delayTimer;
        DateTime _firstDay = DateTime.MinValue;
        DateTime _lastDay = DateTime.MinValue;

        public static Visibility _testVisib;

        /// <summary>
        /// Cвойство отвечает за визуализацию прогресс бара. Key отвечает за запуск остановку прогресс бара, Value за задержку запуска
        /// </summary>
        private KeyValuePair<bool, int> ProgressBarPerform
        {
            set
            {
                if (value.Key)
                    _delayTimer = new System.Threading.Timer((Object state) => Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = true; pb1.Visibility = Visibility.Visible; })), null, value.Value, Timeout.Infinite);
                else
                {
                    Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
                    _delayTimer.Dispose();
                }
            }
        }

        public Home()
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
            ProgressBarPerform = new KeyValuePair<bool, int>(true, 1000);
            VentsTools.currentActionString = "Подключаюсь к Базе данных";

            _vt = new VentsTools();

            VentsDataCfg(_vt); //заполним данные по вентиляторам в _ventsData
            VentsTools.currentActionString = "Показания за день";
            DatePickerInit(_vt);

            //отобразим наш листбокс
            Dispatcher.Invoke(new Action(() =>
            {
                VentsListBox.ItemsSource = _ventsData;
                VentsListBox.Focus();
                VentsListBox.SelectedItem = VentsListBox.Items.CurrentItem;
            }));

            ProgressBarPerform = new KeyValuePair<bool, int>(false, 1000);
        }


        /// <summary>
        /// Заполнение новых данных
        /// </summary>
        /// <param name="date"></param>
        private void UpdatePage(DateTime date)
        {
            try
            {
                var ValuesList = new List<DateValues>();
                VentsTools.currentActionString = "Подключаюсь к Базе данных";
                _fndHwnd = NativeMethods.GetCurrentThreadId();

                ProgressBarPerform = new KeyValuePair<bool, int>(true, 1000);

                string ventName = (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).name; })); // возвращает название выбранного вентилятора
                bool RDVres = _vt.ReadHourValuesFromDB(VentsConst.connectionString, VentsConst._DATAtb, ventName, date, ref ValuesList);
                if (!RDVres)
                    throw new Exception(String.Format("Не удалось получить ежечасные данные для {0} за {1}", (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr; })), (string)Dispatcher.Invoke(new Func<string>(delegate { return (string)dateP.DisplayDate.ToString(); }))));

                //разобьем список на несколько по _VALUE_ROWS_HOURTABLE итемов в каждом
                var parts = ValuesList.DivideByLenght(VentsConst._VALUE_ROWS_HOURTABLE);
                double cellWidth = (double)Dispatcher.Invoke(new Func<double>(() => (workGrid.Width) / VentsConst._MAXIMUM_COLUMNS_HOURTABLE));
                double cellHeight = (double)Dispatcher.Invoke(new Func<double>(() => (workGrid.Height - 4) / VentsConst._MAXIMUM_ROWS_HOURTABLE));

                Dispatcher.Invoke(new Action(delegate { BuildHourTable(parts, cellWidth, cellHeight); })); //построим таблицу

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

                //calculate dayly electric power expense
                int daylyExpense = 0;

                if (date != _firstDay)
                {
                    bool daylyRes = _vt.ReadDaylyExpenseFromDB(VentsConst.connectionString, VentsConst._DATAtb, ventName, date, ref daylyExpense);
                    if (!daylyRes)
                    {
                        throw new Exception(String.Format("Не удалось получить  данные для дневного расхода {0} за {1}", (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr; })), (string)Dispatcher.Invoke(new Func<string>(delegate { return (string)dateP.DisplayDate.ToString(); }))));
                    }
                }
                else
                    daylyExpense = ValuesList.Last<DateValues>().value - ValuesList.First<DateValues>().value;
                daylyExpense = daylyExpense * 100;
                Dispatcher.Invoke(new Action(delegate { totaltb.Text = String.Format("Расход {0} за {1} равен {2} кВтч", (VentsListBox.SelectedItem as Vents).descr, date.GetDateTimeFormats('O')[0].Split('T')[0], daylyExpense.ToString()); }));

                //generate current action string and update content of main window textbox
                string descr = (string)Dispatcher.Invoke(new Func<string>(delegate { return (VentsListBox.SelectedItem as Vents).descr; }));

                VentsTools.currentActionString = String.Format("Показания за  {0} {1}", date.Date.GetDateTimeFormats('D', CultureInfo.CreateSpecificCulture("ru-ru"))[0], descr);

                ProgressBarPerform = new KeyValuePair<bool, int>(false, 1000);
                _fndHwnd = IntPtr.Zero;
            }
            catch (Exception ex)
            {
                VentsTools.currentActionString = "Не удалось подключиться к базе данных";
                _log.Error(ex.Message);
                ProgressBarPerform = new KeyValuePair<bool, int>(false, 1000);
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
            if (!_vt.ReadOneDate(ref _firstDay, QueuePosition.First, VentsConst.connectionString, VentsConst._DATAtb))
                _log.Warn("Не удалось получить самую первую дату");
            Dispatcher.Invoke(new Action(() => dateP.DisplayDateStart = _firstDay));
            _firstDay = DateTime.MaxValue;
            if (!_vt.ReadOneDate(ref _lastDay, QueuePosition.Last, VentsConst.connectionString, VentsConst._DATAtb))
                _log.Warn("Не удалось получить последнюю дату");
            Dispatcher.Invoke(new Action(() => dateP.DisplayDateEnd = _lastDay));
            Dispatcher.Invoke(new Action(() => dateP.IsEnabled = true));
        }

        /// <summary>
        /// Строит часовую таблицу и заполняет ее данными из массива <paramref name="data"/>.
        /// </summary>
        /// <param name="data">Массив данных.</param>
        /// <param name="cellWidth">Ширина ячейки время+значение.</param>
        /// <param name="cellHeight">Высота ячейки время+значение.</param>
        private void BuildHourTable(List<List<DateValues>> data, double cellWidth, double cellHeight)
        {
            workGrid.Children.Clear();
            for (int i = 0; i < data.Count; i++)
            {
                var lbtime = new VE_Label(VE_Label.LabelType.TimeLabel, cellWidth * 2 / 5, 45, "Время");
                var lbvalue = new VE_Label(VE_Label.LabelType.IndicatorLabel, cellWidth * 3 / 5, 45, "Показания\r\n  (МВтч)");
                workGrid.Children.Add(lbtime);
                workGrid.Children.Add(lbvalue);

                lbtime.Margin = new Thickness(lbtime.Width * i + lbvalue.Width * i, 0, 0, 0);
                lbvalue.Margin = new Thickness(lbtime.Width + (lbtime.Width + lbvalue.Width) * i, 0, 0, 0);

                List<DateValues> listDV = data[i];

                for (int j = 0; j < data[i].Count; j++)
                {
                    if (listDV == null) break;

                    VE_TextBox tbtime = new VE_TextBox(VE_TextBox.TextBoxType.TimeTextBox, cellWidth * 2 / 5, cellHeight, listDV[j].date);
                    VE_TextBox tbvalue = new VE_TextBox(VE_TextBox.TextBoxType.IndicatorTextBox, cellWidth * 3 / 5, cellHeight, ((double)listDV[j].value / 10).ToString()); //данные записываются в БД в виде x*100 квтч. поделим на 10 чтобы получить Мвтч
                    workGrid.Children.Add(tbtime);
                    workGrid.Children.Add(tbvalue);

                    tbtime.Margin = new Thickness(tbtime.Width * i + tbvalue.Width * i, lbtime.Height + tbtime.Height * j, 0, 0);
                    tbvalue.Margin = new Thickness(tbtime.Width + (tbtime.Width + tbvalue.Width) * i, lbvalue.Height + tbvalue.Height * j, 0, 0);
                }
            }
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
                _fndThread = new Thread(() => UpdatePage(_date));
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
                    _fndThread.Abort();
            }
            catch
            {
                _log.Error("Не могу закрыть поток FNDthread {0}", _fndHwnd.ToString());
            }

            if (VentsListBox.SelectedItem != null && dateP.SelectedDate != null)
            {
                _fndThread = new Thread(() => UpdatePage(_date));
                _fndThread.Start();
                _fndHwnd = (IntPtr)_fndThread.ManagedThreadId;
            }

        }

        private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName.StartsWith("hour"))
            {
                e.Column.Header = "Время";
                e.Column.Width = 60;
            }
            if (e.PropertyName.StartsWith("value"))
            {
                e.Column.Header = "Показания";
                e.Column.Width = 90;
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_fvdThread.ThreadState == System.Threading.ThreadState.Suspended)
                _fvdThread.Resume();
            if (this.IsVisible && _fvdThread.ThreadState == System.Threading.ThreadState.Stopped)
                VentsTools.currentActionString = "Показания за день";
        }

    }
    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        internal static extern IntPtr GetCurrentThreadId();
    }

}
