using System;
using System.Collections.Generic;
using System.Data;
using NLog;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using System.IO;
using System.Windows;
using System.ComponentModel;
using System.Windows.Controls;

namespace ventEnergy
{
    public class Vents
    {
        public bool isEnabled { get; set; }
        public int id { get; set; }         //номер
        public string name { get; set; }    //название 
        public int DB { get; set; }         //номер блока данных
        public int startBit { get; set; }   //нормер переменной в DB
        public int size { get; set; }       //размер переменной в байтах
        public int countValue { get; set; } //значение полного счетчика
        public int value { get; set; }       //значение
        public string descr { get; set; }   //описание
        public string resetM { get; set; }  //адрес меркера дял сброса счетчика частотного преобразователя
        public DateTime date { get; set; }
    }
    public enum QueuePosition
    {
        First,
        Last
    }
    public class VentsTools
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();

        public bool ReadConfigFromDB(string connectionString, string configTable, ref List<Vents> VentsData)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                ((IDisposable)connection).Dispose();
                return false;
            }

            try
            {
                SqlCommand cmd = new SqlCommand("Select * From " + configTable, connection);
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                List<Vents> ventslist = new List<Vents>();
                while (dr.Read())
                {
                    var t = dr.GetOrdinal("isEnabled");
                    ventslist.Add(new Vents {
                        id = dr.GetInt16(dr.GetOrdinal("id")),
                        name = dr.GetValue(dr.GetOrdinal("name")).ToString(),
                        DB = dr.GetInt16(dr.GetOrdinal("DB")),
                        startBit = dr.GetInt32(dr.GetOrdinal("startBit")),
                        size = (int)dr.GetByte(dr.GetOrdinal("size")),
                        countValue = dr.GetInt32(dr.GetOrdinal("countValue")), 
                        value = 0,
                        descr = dr.GetValue(dr.GetOrdinal("description")).ToString(),
                        resetM = dr.GetValue(dr.GetOrdinal("resetM")).ToString(), 
                        isEnabled = dr.GetBoolean(dr.GetOrdinal("isEnabled")) });
                }
                connection.Close();
                connection.Dispose();
                VentsData = ventslist;
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }

        }
        public bool WriteDataToDB(List<Vents> VentsData)
        {
            int eventID = 0;
            int lastEventID = 0;
            SqlConnection connection = new SqlConnection(VentsConst.connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                ((IDisposable)connection).Dispose();
                return false;
            }
            try
            {
                //получим номер последней записи
                SqlCommand cmd = new SqlCommand("Select Top 1 eventID from " + VentsConst._DATAtb + " ORDER BY eventID DESC", connection); //команда на получение последнего номера eventID
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                if (!dr.Read())
                    lastEventID = -1;
                else
                    lastEventID = dr.GetInt32(0);


                eventID = lastEventID + 1;

                connection.Close();


                try
                {
                    connection.Open();
                }
                catch (SqlException ex)
                {
                    log.Error(ex.Message);
                    ((IDisposable)connection).Dispose();
                    return false;
                }
                foreach (Vents vent in VentsData)
                {

                    //добавим новые записи
                    cmd = new SqlCommand("Insert into " + VentsConst._DATAtb + "(eventID, date, name, value) Values (@eventID, @date, @name, @value)", connection);
                    SqlParameter param = new SqlParameter();
                    param.ParameterName = "@eventID";
                    param.Value = eventID;
                    param.SqlDbType = SqlDbType.Int;
                    cmd.Parameters.Add(param);
                    eventID++;

                    param = new SqlParameter();
                    param.ParameterName = "@date";
                    param.Value = System.DateTime.Now;
                    param.SqlDbType = SqlDbType.DateTime;
                    cmd.Parameters.Add(param);

                    param = new SqlParameter();
                    param.ParameterName = "@name";
                    param.Value = vent.name;
                    param.SqlDbType = SqlDbType.VarChar;
                    cmd.Parameters.Add(param);

                    param = new SqlParameter();
                    param.ParameterName = "@value";
                    param.Value = vent.countValue + vent.value;
                    param.SqlDbType = SqlDbType.Int;
                    cmd.Parameters.Add(param);

                    try
                    {
                        cmd.ExecuteNonQuery(); //вставляем запись
                    }
                    catch
                    {
                        throw new Exception("Ошибка при выполнении запроса на добавление записи");
                    }
                }


                connection.Close();
                connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }
        /// <summary>
        /// read the last sql record from DATAtb and compare the DateTime.Hour
        /// </summary>
        /// <param name="date"></param>
        /// <returns>1 if record contains current hour, 0 if not and -1 if cant connect to DB or smth wrong</returns>
        public int LastRecordCurrentHour(DateTime date)
        {
            SqlConnection connection = new SqlConnection(VentsConst.connectionString);
            try
            { 
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                ((IDisposable)connection).Dispose();
                return -1;
            }
             try
            {
                //получим номер последней записи
                SqlCommand cmd = new SqlCommand("Select Top 1 eventID, date from " + VentsConst._DATAtb + " ORDER BY eventID DESC", connection); //команда на получение последнего номера eventID
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                dr.Read();
                if (date.Hour == dr.GetDateTime(1).Hour)
                    return 1;
                return 0;
                connection.Close();
            }
             catch (Exception ex)
             {
                 log.Error(ex.Message);
                 connection.Close();
                 connection.Dispose();
                 return -1;
             }
        }
        public bool UpdateCountValuesConfTB(Vents data)
        {
            int eventID = 0;
            int lastEventID = 0;

            SqlConnection connection = new SqlConnection(VentsConst.connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                ((IDisposable)connection).Dispose();
                return false;
            }

            try
            {
                SqlCommand cmd = new SqlCommand(String.Format("Update {0} Set countvalue = @countvalue, lastResetkWh = @lastResetkWh  Where name = @name", VentsConst._CONFtb), connection);
                //(data.countValue +data.value).ToString(), DateTime.Today, data.name )
                SqlParameter param = new SqlParameter();
                param.ParameterName = "@countvalue";
                param.Value = data.countValue + data.value;
                param.SqlDbType = SqlDbType.Int;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@lastResetkWh";
                param.Value = System.DateTime.Now;
                param.SqlDbType = SqlDbType.DateTime;
                cmd.Parameters.Add(param);

                param = new SqlParameter();
                param.ParameterName = "@name";
                param.Value = data.name;
                param.SqlDbType = SqlDbType.VarChar;
                cmd.Parameters.Add(param);
                try
                {
                    cmd.ExecuteNonQuery(); //вставляем запись
                }
                catch
                {
                    throw new Exception("Ошибка при выполнении запроса на изменение записи");
                }

                connection.Close();
                connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }
        public bool ReadHourValuesFromDB(string connectionString, string dataTable, string name, DateTime date, ref List<DateValues> hourValues)
        {
            hourValues.Clear();

            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                return false;
            }

            try
            {
                string dateStr = date.GetDateTimeFormats('O')[0].Split('T')[0]; //строковая компонента даты (без времени)
                SqlCommand cmd = new SqlCommand("Select date, value From " + dataTable + " Where date Between '" + dateStr + " 00:00:00.000' And '" + dateStr + " 23:59:59.999' And name='" + name + "'", connection);
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                while (dr.Read())
                {
                    hourValues.Add(new DateValues { date = dr.GetDateTime(0).GetDateTimeFormats('t', CultureInfo.CreateSpecificCulture("ru-ru"))[0], value = dr.GetInt32(1) });
                }
                connection.Close();
                connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }
        public bool ReadDaylyExpenseFromDB(string connectionString, string dataTable, string name, DateTime date, ref int daylyExpense)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                return false;
            }

            try
            {
                string dateStr = date.GetDateTimeFormats('O')[0].Split('T')[0]; //строковая компонента даты (без времени)
                string prevDateStr = date.AddDays(-1).GetDateTimeFormats('O')[0].Split('T')[0]; //строковая компонента даты предыдущего дня (без времени)

                SqlCommand lastDayValueCmd = new SqlCommand("Select Top 1 value From " + dataTable + " Where date Between '" + dateStr + " 00:00:00.000' And '" + dateStr + " 23:59:59.999' And name='" + name + "' ORDER BY eventID DESC", connection);
                SqlCommand lastPrevDayValueCmd = new SqlCommand("Select Top 1 value From " + dataTable + " Where date Between '" + prevDateStr + " 00:00:00.000' And '" + prevDateStr + " 23:59:59.999' And name='" + name + "' ORDER BY eventID DESC", connection);

                SqlDataReader dr = lastDayValueCmd.ExecuteReader();
                int lastDayValue = 0;
                if (dr.Read())
                    lastDayValue = dr.GetInt32(0);
                dr.Close();

                dr = lastPrevDayValueCmd.ExecuteReader(CommandBehavior.CloseConnection);
                int lastPrevDayValue = 0;
                if (dr.Read())
                    lastPrevDayValue = dr.GetInt32(0);
                dr.Close();
                connection.Close();
                connection.Dispose();

                daylyExpense = lastDayValue - lastPrevDayValue;
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }

        public bool ReadDayValuesFromDB(string connectionString, string dataTable, string name, DateTime date, ref List<DateValues> dayValues)
        {
            dayValues.Clear();
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                return false;
            }

            try
            {
                string dateStr = date.GetDateTimeFormats('O')[0].Substring(0, 8); //строковая компонента даты (без времени и дня yyyy-mm-)
                string[] yearmonth = dateStr.Split('-');
                int days = DateTime.DaysInMonth(int.Parse(yearmonth[0]), int.Parse(yearmonth[1])); //колличество дней в этом месяце
                for (int day = 1; day <= days; day++)
                {
                    SqlCommand cmd = new SqlCommand(String.Format("Select top 1 date, value From {0} Where date Between '{1}{2} 00:00:00.000' And '{1}{2} 23:59:59.999' And name='{3}'", dataTable, dateStr, day.ToString(), name), connection);
                    //SqlCommand cmd = new SqlCommand(String.Format("Select date, value From {0} As T Where date= (Select Min(T2.date) From {1} As T2 Where T.date BETWEEN '{2}01 00:00:00.000' AND '{3}{4} 23:59:59.999' AND T.name='{5}' And DateDiff(\"d\", 0, T2.date) = DateDiff(\"d\", 0, T.date))", dataTable, dataTable, dateStr, dateStr, days.ToString(), name), connection);
                    SqlDataReader dr = cmd.ExecuteReader();
                    try
                    {
                        dr.Read();
                        if (dr != null)
                            dayValues.Add(new DateValues { date = dr.GetDateTime(0).Day.ToString(), value = dr.GetInt32(1) });
                        dr.Close();
                    }
                    catch
                    {
                        dr.Close();
                    }
                }
                connection.Close();
                connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }
        public bool ReadMonthlyExpenseFromDB(string connectionString, string dataTable, string name, DateTime date, ref int monthlyExpense)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                return false;
            }

            try
            {
                string dateStr = date.GetDateTimeFormats('O')[0].Substring(0, 8); //строковая компонента даты (без времени и дня yyyy-mm-)
                string prevDateStr = date.AddMonths(-1).GetDateTimeFormats('O')[0].Substring(0, 8); //строковая компонента даты предыдущего мессяца (без времени и дня yyyy-mm-)
                string[] yearmonth = dateStr.Split('-');
                string[] yearmonthP = prevDateStr.Split('-');
                int days = DateTime.DaysInMonth(int.Parse(yearmonth[0]), int.Parse(yearmonth[1])); //колличество дней в этом месяце
                int daysPrev = DateTime.DaysInMonth(int.Parse(yearmonthP[0]), int.Parse(yearmonthP[1])); //колличество дней в прошлом месяце

                SqlCommand lastMonthValueCmd = new SqlCommand(String.Format("Select top 1 value From {0} Where date Between '{1}01 00:00:00.000' And '{1}{2} 23:59:59.999' And name='{3}' ORDER BY eventID DESC", dataTable, dateStr, days.ToString(), name), connection);
                SqlCommand lastPrevMonthValueCmd = new SqlCommand(String.Format("Select top 1 value From {0} Where date Between '{1}01 00:00:00.000' And '{1}{2} 23:59:59.999' And name='{3}' ORDER BY eventID DESC", dataTable, prevDateStr, daysPrev.ToString(), name), connection);

                SqlDataReader dr = lastMonthValueCmd.ExecuteReader();
                int lastMonthValue = 0;
                if (dr.Read())
                    lastMonthValue = dr.GetInt32(0);
                dr.Close();

                dr = lastPrevMonthValueCmd.ExecuteReader(CommandBehavior.CloseConnection);
                int lastPrevMonthValue = 0;
                if (dr.Read())
                    lastPrevMonthValue = dr.GetInt32(0);
                dr.Close();
                connection.Close();
                connection.Dispose();

                monthlyExpense = lastMonthValue - lastPrevMonthValue;
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }
        public bool FillVentsData(ref List<Vents> VentsData)
        {
            bool readCfgResult = false;
            VentsTools VT = new VentsTools();

            readCfgResult = ReadConfigFromDB(VentsConst.connectionString, VentsConst._CONFtb, ref VentsData);
            if (!readCfgResult)
            {
                log.Info("Нет подключения к базе данных, проверьте настройки сети");
            }
            //MessageBox.Show("Нет подключения к базе данных, проверьте настройки сети");
            return readCfgResult;
        }
        //читает первую или последнюю запись в таблице
        public bool ReadOneDate(ref DateTime oneday, QueuePosition where, string connectionString, string dataTable)
        {
            SqlConnection connection = new SqlConnection(connectionString);
            try
            {
                connection.Open();
            }
            catch (SqlException ex)
            {
                log.Error(ex.Message);
                return false;
            }

            try
            {
                string command = "";
                switch (where)
                {
                    case QueuePosition.First:
                        command = "Select Top 1  date From " + dataTable + " ORDER BY eventID ASC";
                        break;
                    case QueuePosition.Last:
                        command = "Select Top 1  date From " + dataTable + " ORDER BY eventID DESC";
                        break;
                }
                SqlCommand cmd = new SqlCommand(command, connection);
                SqlDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection);

                if (!dr.Read())
                    throw new Exception("в базе данных нет записей");
                oneday = dr.GetDateTime(0);
                connection.Close();
                connection.Dispose();
                return true;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                connection.Close();
                connection.Dispose();
                return false;
            }
        }

        public bool UpdateClientValues()
        {
            return false;
        }
        public delegate void del(string s);

        public static string currentActionString
        {
            get
            { return curActStr; }
            set
            {
                if (!value.Equals(curActStr))
                {
                    curActStr = value;
                    onChangeActionString(curActStr);
                }
            }
        }
        private static string curActStr;

        public static string currentSettingString
        {


            get
            { return curSetStr; }
            set
            {
                if (!value.Equals(curSetStr))
                {
                    curSetStr = value;
                    onChangeSettingsString(curSetStr);
                }
            }
        }
        private static string curSetStr;
        public static event del onChangeActionString;
        public static event del onChangeSettingsString;


        /// <summary>
        /// создает список всех видимых заданных дочерних элементов в родительском элементе 
        /// </summary>
        /// <param name="parentElement"></param>
        /// <param name="childElement"></param>
        /// <returns></returns>
        public List<FrameworkElement> ListOfCheckBoxes(FrameworkElement parentElement, Type childElementType)
        {
            List<FrameworkElement> childElemList = new List<FrameworkElement>();
            List<FrameworkElement> allElem = new List<FrameworkElement>();
            //создадим список всех элементов в родительском элементе
            ChildControls(parentElement, allElem);
            //выберем из списка только видимые и нужные по типу
            foreach (FrameworkElement elem in allElem)
            {
                if (elem.GetType() == childElementType && elem.Visibility != Visibility.Hidden)
                {
                    childElemList.Add(elem);
                }
            }
            return childElemList;
        }
        /// <summary>
        /// список всех wpf элементов в заданном родительском элементе 
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="Controls"></param>
        public void ChildControls(FrameworkElement elem, List<FrameworkElement> Controls)
        {
            foreach (FrameworkElement child in LogicalTreeHelper.GetChildren(elem))
            {
                try
                {
                    Controls.Add(child);
                    if (child is ContentControl)
                    {
                        if (!((child as ContentControl).Content is string))
                            ChildControls((FrameworkElement)(child as ContentControl).Content, Controls);
                    }
                    else ChildControls(child, Controls);
                }
                catch { }
            }
        }

    }

    public static class VentsConst
    {
        public const string _ipPLC = "172.20.199.225";
        public const int _RACK = 0;
        public const int _SLOT = 3;
        public const int _DEFAULT_CHECK_INTERVAL = 60000; //60000 - 1 min
        public const int _CLIENSETTINGSCOUNT = 6;
        public const int _MAX_ENERGY_COUNTER = 30000; // (100 квтч)
        public const int ppo2TypeProcessDataSize = 12;
        public const string connectionString = @"Integrated Security=false; Persist Security Info=False; Initial Catalog=PIC_D_DATABASE ; Data Source=172.20.199.1; User ID=ventEnergy; Password=belsolod.ventEnergy2015";
        public const string _DATABASENAME = "PIC_D_DATABASE"; //имя Базы данных
        public const string _CONFtb = "ventEnergyConf"; //имя таблицы конфигурации в БД 
        public const string _DATAtb = "ventEnergyData"; //имя таблицы данных в БД 

        public const string _PROJECTNAME = "VentEnergy";
        public const string _PROJECTGROUPLOCATION = @"Software\Belsolod";
        public const string _PROJECTGROUPNAME = @"Belsolod";
        public const string _SETTINGS = "Settings";
        public const string _PROJECTLOCATION = @"Software\Belsolod\VentEnergy";
        public const string _SETTINGSLOCATION = @"Software\Belsolod\VentEnergy\Settings";
        public const string _ADAPTERSLOCATION = @"Software\Belsolod\VentEnergy\Adapters";
        public const string _BelIP = @"belip";
        public const string _BelMSK = @"belmsk";
        public const string _BelGTW = @"belgtw";
        public const string _WNCIP = @"wncip";
        public const string _WNCBelMSK = @"wncmsk";
        public const string _WNCBelGTW = @"wncgtw";
        public const string _LASTADPTGUID = "LastUsedNetworkAdapterGUID";
        public const string _LASTAPPLYBUTTONCLICKED = "LastApplyButtonClicked";
        public const string _ADAPTERS = "Adapters";
        public const string _DEFAULTWINCOSIP = "172.20.199.20";
        public const string _DEFAULTWINCOSMASK = "255.255.255.0";
        public const string _DEFAULTWINCOSGATEWAY = "172.20.199.250";
        public const string _SERVNOTFIRSTRUN = "vES";

    }


    public class DateValues
    {
        [DisplayName("Время")]
        public string date { get; set; }
        [DisplayName("Показания")]
        public int value { get; set; }
    }

    public class ventEnergyIPSettings
    {
        private string belsIPContainer;
        public string belsIP
        {
            get { return belsIPContainer; }
            set
            {
                if (!value.Equals(belsIPContainer))
                {
                    belsIPContainer = value;
                    onBelsIPChange(belsIPContainer);
                }
                else
                    belsIPContainer = value;
            }
        }
        private string belsMaskContainer;
        public string belsMask
        {
            get { return belsMaskContainer; }
            set
            {
                if (!value.Equals(belsMaskContainer))
                {
                    belsMaskContainer = value;
                    onBelsMaskChange(belsMaskContainer);
                }
                else
                    belsMaskContainer = value;
            }
        }
        private string belsGateContainer;
        public string belsGate
        {
            get { return belsGateContainer; }
            set
            {
                if (!value.Equals(belsGateContainer))
                {
                    belsGateContainer = value;
                    onBelsGateChange(belsGateContainer);
                }
                else
                    belsGateContainer = value;
            }
        }
        private string wincIPContainer;
        public string wincIP
        {
            get { return wincIPContainer; }
            set
            {
                if (!value.Equals(wincIPContainer))
                {
                    wincIPContainer = value;
                    onWincIPChange(wincIPContainer);
                }
                else
                    wincIPContainer = value;
            }
        }
        private string wincMaskContainer;
        public string wincMask
        {
            get { return wincMaskContainer; }
            set
            {
                if (!value.Equals(wincMaskContainer))
                {
                    wincMaskContainer = value;
                    onWincMaskChange(wincMaskContainer);
                }
                else
                    wincMaskContainer = value;
            }
        }
        private string wincGateContainer;
        public string wincGate
        {
            get { return wincGateContainer; }
            set
            {
                if (!value.Equals(wincGateContainer))
                {
                    wincGateContainer = value;
                    onWincGateChange(wincGateContainer);
                }
                else
                    wincGateContainer = value;
            }
        }

        public delegate void del(string vs);
        public static event del onBelsIPChange;
        public static event del onBelsMaskChange;
        public static event del onBelsGateChange;
        public static event del onWincIPChange;
        public static event del onWincMaskChange;
        public static event del onWincGateChange;
    }
    public class AssemblyInfo
    {
        public AssemblyInfo(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");
            this.assembly = assembly;
        }

        private readonly Assembly assembly;

        /// <summary>
        /// Gets the title property
        /// </summary>
        public string ProductTitle
        {
            get
            {
                return GetAttributeValue<AssemblyTitleAttribute>(a => a.Title,
                       Path.GetFileNameWithoutExtension(assembly.CodeBase));
            }
        }

        /// <summary>
        /// Gets the application's version
        /// </summary>
        public string Version
        {
            get
            {
                string result = string.Empty;
                Version version = assembly.GetName().Version;
                if (version != null)
                    return version.ToString();
                else
                    return "1.0.0.0";
            }
        }
        public string ProductTitleAndVersion
        {
            get
            {
                return ProductTitle + " Версия: " + Version;
            }
        }
        /// <summary>
        /// Gets the description about the application.
        /// </summary>
        public string Description
        {
            get { return GetAttributeValue<AssemblyDescriptionAttribute>(a => a.Description); }
        }


        /// <summary>
        ///  Gets the product's full name.
        /// </summary>
        public string Product
        {
            get { return GetAttributeValue<AssemblyProductAttribute>(a => a.Product); }
        }

        /// <summary>
        /// Gets the copyright information for the product.
        /// </summary>
        public string Copyright
        {
            get { return GetAttributeValue<AssemblyCopyrightAttribute>(a => a.Copyright); }
        }

        /// <summary>
        /// Gets the company information for the product.
        /// </summary>
        public string Company
        {
            get { return GetAttributeValue<AssemblyCompanyAttribute>(a => a.Company); }
        }

        protected string GetAttributeValue<TAttr>(Func<TAttr,
          string> resolveFunc, string defaultResult = null) where TAttr : Attribute
        {
            object[] attributes = assembly.GetCustomAttributes(typeof(TAttr), false);
            if (attributes.Length > 0)
                return resolveFunc((TAttr)attributes[0]);
            else
                return defaultResult;
        }
    }
}
