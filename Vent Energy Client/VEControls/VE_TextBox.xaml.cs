using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace ventEnergy.VEControls
{
    /// <summary>
    /// Логика взаимодействия для VE_TextBox.xaml
    /// </summary>
    public partial class VECalendar : UserControl
    {
        public VECalendar()
        {
            InitializeComponent();
            minD = new DateTime(1, 1, 1);
            maxD = new DateTime(9999, 12, 31);
            //_calendar.DayButtonMouseUp += new MouseButtonEventHandler(Calendar_DayButtonMouseUp);
            //_calendar.DisplayDateChanged += new EventHandler<CalendarDateChangedEventArgs>
            //                                    (Calendar_DisplayDateChanged);
            //_calendar.SelectedDatesChanged += new EventHandler<SelectionChangedEventArgs>
            //                                      (Calendar_SelectedDatesChanged);
            //_calendar.MouseLeftButtonDown += new MouseButtonEventHandler
            //                                    (Calendar_MouseLeftButtonDown);
            //_calendar.KeyDown += new KeyEventHandler(Calendar_KeyDown);
            //_calendar.SelectionMode = CalendarSelectionMode.SingleDate;
            //_calendar.SizeChanged += new SizeChangedEventHandler(Calendar_SizeChanged);
            //_calendar.IsTabStop = true;
            //#region Juan Mejia - Modification
            //_calendar.DisplayMode = this.CalendarMode;
            //_calendar.DisplayModeChanged += new EventHandler<CalendarModeChangedEventArgs>
            //                                    (Calendar_DisplayModeChanged);
            //#endregion
        }

        public Key[] digit = { Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9,
                             Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7,
                             Key.NumPad8, Key.NumPad9,};

        #region Properties
        public bool Focused
        {
            get
            {
                if (Box.IsFocused == true | butt.IsFocused == true )
                {
                    return true;
                }
                return false;
            }

        }

        private bool dateComplete;
        public bool DateComplete
        {
            get { return dateComplete; }
        }

        /// <summary>
        /// text format : mm.yyyy
        /// </summary>
        public string Text
        {
            get
            {
                return Box.Text;
            }
            set
            {
                if (value != null && IsValid(value))
                    Box.Text = value;
                else
                    Box.Text = "";
            }
        }
        private DateTime minD;
        public DateTime minDate
        {
            get{ return minD;}
            set{ if(value!=null) minD=value; }
                
        }
        private DateTime maxD;
        public DateTime maxDate
        {
            get{ return maxD;}
            set{ if(value!=null) maxD=value; }
                
        }
        public DateTime? DisplayDateStart
        {
            get { return calend.DisplayDateStart; }
            set { calend.DisplayDateStart = value; }
        }
        public DateTime? DisplayDateEnd
        {
            get { return calend.DisplayDateEnd; }
            set { calend.DisplayDateEnd = value; }
        }
        public DateTime DisplayDate
        {
            get { return calend.DisplayDate; }
            set { calend.DisplayDate = value; }
        }
        #endregion
        #region Events
       // public override event System.EventHandler<CalendarModeChangedEventArgs> DisplayModeChanged;
        #endregion
        //public CalendarMode CalendarMode
        //{
        //    get { return (CalendarMode)GetValue(CalendarModeProperty); }
        //    set { SetValue(CalendarModeProperty, value); }
        //}

        //public static readonly DependencyProperty CalendarModeProperty =
        //        DependencyProperty.Register(
        //        "CalendarMode",
        //        typeof(CalendarMode),
        //        typeof(DatePicker),
        //        new PropertyMetadata(OnCalendarModeChanged));

        //private static void OnCalendarModeChanged(DependencyObject d,
        //                                  DependencyPropertyChangedEventArgs e)
        //{
        //    DatePicker dp = d as DatePicker;

        //    // get the new value into a calendar mode variable.
        //    CalendarMode cm = (CalendarMode)e.NewValue;

        //    //if (dp != null)
        //    //{
        //    //    dp._calendar.DisplayMode = cm;
        //    //}
        //}
        /** Returns whether Box have valid date (mm.yyyy)
        * \return True if valid, false otherwise
        * */
        private bool IsValid(string s)
        {
            try
            {
                //check if 3rd char is comma
                if(s[2] != '.')
                    return false;
                //check if first number is month
                var numbers = s.Split('.');
                int month = int.Parse(numbers[0]);
                if (month < 0 || month > 12)
                    return false;
                //check if second number is year
                int year = int.Parse(numbers[1]);
                if (year < 0 || year > 2099)
                    return false;
                else
                    return true;
            }
            catch
            {
                return false;
            }
        }
        //сравнивает значение в виде даты
        private bool MinMaxCorrespond(DateTime date, DateTime max, DateTime min)
        {
            if(DateTime.Compare(date, max) <=0 && DateTime.Compare(date, min) >=0)
                return true;
            else return false;
        }
        private bool isDigit(Key key)
        {
            foreach (Key d in digit)
            {
                if (key == d)
                {
                    return true;
                }
            }
            return false;
        }



        private void Box_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.OemPeriod || isDigit(e.Key))
            {
                if(Box.Text!="" && Box.Text.Length!=Box.SelectionLength)
                {
                    if(IsValid(Box.Text))
                    {
                        string date = "01."+Box.Text;
                        DateTime dt = DateTime.ParseExact(date, "dd.MM.yyyy", CultureInfo.CurrentUICulture);
                        if (MinMaxCorrespond(dt, maxD, minD))
                        {
                            calend.DisplayDate = dt;
                            e.Handled = true;
                        }
                        else
                        {
                            Box.Text = "Дата недоступна";
                            Box.SelectAll();
                            e.Handled = true;
                        }


                    }
                    else
                    {
                        Box.Text = "Не правильный формат";
                        Box.SelectAll();
                        e.Handled = true;
                    }
                }
            }
        }
        //private void Calendar_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        //{
        //    if (this.CalendarMode == CalendarMode.Year)
        //    {
        //        if (_calendar.DisplayMode == CalendarMode.Month)
        //        {
        //            _calendar.DisplayMode = CalendarMode.Year;

        //            if (_popUp.IsOpen)
        //            {
        //                this.SelectedDate = _calendar.SelectedMonth;
        //                this._popUp.IsOpen = false;
        //            }
        //        }
        //    }
        //}
        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void calend_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            Box.Text = calend.DisplayDate.GetDateTimeFormats('Y', CultureInfo.CreateSpecificCulture("ru-ru"))[0];

        }

        private void calend_DisplayModeChanged(object sender, CalendarModeChangedEventArgs e)
        {
            //if (calend.DisplayMode != CalendarMode.Year)
            //{
            //    calend.DisplayMode = CalendarMode.Year;
 
            //}
        }

        private void calend_DisplayDateChanged(object sender, CalendarDateChangedEventArgs e)
        {
            //Box.Text = e.AddedDate.Value.GetDateTimeFormats('Y', CultureInfo.CreateSpecificCulture("ru-ru"))[0];

        }

        private void calend_MouseUp(object sender, MouseButtonEventArgs e)
        {

        }





            


    }

}
