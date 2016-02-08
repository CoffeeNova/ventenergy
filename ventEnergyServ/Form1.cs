using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Snap7;
using NLog;
using System.Threading;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Globalization;
using System.Reflection;
using System.IO;
using NWTweak;
using ventEnergy;

namespace ventEnergy
{
    public partial class MainServerForm : Form
    {

        struct ReadData
        {
            public const int defaultHour = 9;
            public const int defaultMinute = 00;
        }

        private bool debugBit = false;    // set true to see how to fill database
        private bool tryToConnect = false;
        
        private S7Client Client;
        private System.Threading.Timer readDataTimer;
        private readonly Logger log = LogManager.GetCurrentClassLogger();      
        private int checkInterval;
        private int hour;
        private int minute;
        private List<Vents> VentsData;
        private delegate bool timeDelegate(int time, int rangeMin, int rangeMax);

        public MainServerForm(string[] args)
        {

            InitializeComponent();
            VentsData = new List<Vents>();


            checkInterval = VentsConst._DEFAULT_CHECK_INTERVAL;
            hour = System.DateTime.Now.Hour;
            minute = ReadData.defaultMinute;
            #region args
            bool firstRunArgsBit = false;
            foreach (string arg in args)
            {
                if(arg.StartsWith("-int"))
                {
                    int i = VentsConst._DEFAULT_CHECK_INTERVAL;
                    try
                    {
                        i = Convert.ToInt32(arg.Split('=')[1]);
                    }   
                    catch
                    {
                        log.Error("Задан не числовой аргумент -int");
                        Process.GetCurrentProcess().Kill();
                    }
                    checkInterval = i;
                }
                //if(arg.StartsWith("-hour"))
                //{
                //    int h = ReadData.defaultHour;
                //    try
                //    {
                //        h = Convert.ToInt32(arg.Split('=')[1]);
                //        if (h < 0 || h > 23)
                //            throw new Exception();
                //    }   
                //    catch
                //    {
                //        log.Error("Задан не верный числовой аргумент -hour");
                //        Process.GetCurrentProcess().Kill();
                //    }
                //    hour= h;
                //}
                if (arg.StartsWith("-min"))
                {
                    int m = ReadData.defaultMinute;
                    try
                    {
                        m = Convert.ToInt32(arg.Split('=')[1]);
                        if (m < 0 || m > 59)
                            throw new Exception();
                    }
                    catch
                    {
                        log.Error("Задан не верный числовой аргумент -min");
                        Process.GetCurrentProcess().Kill();
                    }
                    minute = m;
                }

                if (arg.StartsWith("-debug"))
                {
                    debugBit = true;
                    checkInterval = Int32.MaxValue;
                }
                if (arg.StartsWith("-firstrun"))
                    firstRunArgsBit = true;

            #endregion
            }

            try
            {
                Client = new S7Client();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                Process.GetCurrentProcess().Kill();
            }

            bool firsRunRegBit = false;
            try
            {
                
                var notFirstRunValue = RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, VentsConst._SERVNOTFIRSTRUN);
                if (notFirstRunValue == null)
                    firsRunRegBit = true;
                else if ((bool)notFirstRunValue == true)
                    firsRunRegBit = false;
                else
                    firsRunRegBit = true;
            }
            catch
            {
                log.Error("Unable to read registry");
            }

            //if (firstRunArgsBit || firsRunRegBit || debugBit)
            //{
            //    string question = "Внимание! Программа запускается первый раз. При первом запуске производится очистка всех значений базы данных. Для запуска без очистки базы нажмите \"No\",  для выхода из программы нажмите \"Cancel\". ";
            //    string caption = "Первый запуск";
            //    MessageBoxButtons MsgButtons = MessageBoxButtons.YesNoCancel;
            //    DialogResult MsgResult = MessageBox.Show(question, caption, MsgButtons, MessageBoxIcon.Question, MessageBoxDefaultButton.Button3);
            //    if(MsgResult == DialogResult.Yes)
            //    {   
            //        /*удалить значения в базе данных
            //        надо считать текущие данные из контроллера
            //        и записать их в таблицу конфигурации в начальные значения
            //        после чего выполнить для каждого девайса сброс счетчика (параметр 16.12 = 2 )*/
            //    }
            //    if(MsgResult == DialogResult.Cancel)
            //    {
            //        Application.Exit();
            //    }
            //}
            readDataTimer = new System.Threading.Timer(ReadDataEvent, null, 0, checkInterval);

        }

        private void ReadDataEvent(Object state)
        {
            //Stopwatch watch = new Stopwatch();
            //watch.Start();
            //timeDelegate func = (x, min, max) => { if (x >= min && x <= max) return true; return false; };
            //func(System.DateTime.Now.Hour, 0, 23) &&
            if ((minute == System.DateTime.Now.Minute && !tryToConnect) || (debugBit && !tryToConnect))
                {
                    int tempConnected;

                    tryToConnect = false;
                    // 0 - connection successfull 
                    tempConnected = Client.ConnectTo(VentsConst._ipPLC, VentsConst._RACK, VentsConst._SLOT);
                    if (tempConnected != 0)
                    {
                        ShowError(tempConnected);
                        tryToConnect = true;
                        return;
                    }
                    else
                    {
                        Client.Disconnect();
                        new Thread(DirtyJob).Start();  
                    }

                }
                
            if (tryToConnect)
                {
                    int tempConnected;

                    tempConnected = Client.ConnectTo(VentsConst._ipPLC, VentsConst._RACK, VentsConst._SLOT);
                    if (tempConnected != 0)
                    {
                        //ShowError(tempConnected); уберем ошибку из лога, будет повторяться очень часто одинаковая
                        Func<int, int> compare = x => { if (x == -1) return 59; return x; };
                        if (System.DateTime.Now.Minute == compare(minute - 1))
                            tryToConnect = false;
                    }
                    else
                    {
                        Client.Disconnect();
                        tryToConnect = false;
                        new Thread(DirtyJob).Start();
                    }
                
                //readDataTimer.Change( Math.Max( 0, _CHECK_INTERVAL - watch.ElapsedMilliseconds ), Timeout.Infinite );
                //watch.Stop();
            }

        }
        private delegate string ventdel(int DB, int start, int size);
        private void DirtyJob()
        {
            VentsTools VT = new VentsTools();
            bool refreshValues = false;
            if (VT.ReadConfigFromDB(VentsConst.connectionString, VentsConst._CONFtb, ref VentsData))
                if (ReadDataFromPLC())
                {
                    //проверим нет ли значений превышающих _MAX_ENERGY_COUNTER (*100 квтч), если есть - добавим число к общему значению счетчика (ventEnergyConf.tb 'countvalue')
                    //также проверим нет ли нулевых значений (что может означать неисправность, например "нет связи")
                    //если есть ноль, проверим сообщение об ошибке, и если есть ошибка заменим значение на -1
                    foreach(Vents record in VentsData)
                    {
                        
                        if (record.value >= VentsConst._MAX_ENERGY_COUNTER)
                        {
                            #region лямбда
                            ventdel vd = (DB, start, size) =>
                            {
                                string RDY_REF_state = "";
                                try
                                {
                                    if (Client.ConnectTo(VentsConst._ipPLC, VentsConst._RACK, VentsConst._SLOT) != 0)
                                        throw new Exception("Cant connect to PLC");

                                    int result;
                                    byte[] buffer = new byte[1];
                                    System.Collections.BitArray ba;

                                    result = Client.DBRead(DB, start+1, size, buffer); //vent.startBit+1,- secont byte of Input Stateword (RDY_REF bit is a 3rd bit)
                                    if (result != 0)
                                        ShowError(result);
                                    else
                                    {
                                        Client.Disconnect();
                                        ba = new System.Collections.BitArray(new byte[] { buffer[0] });
                                        return ba.Get(2) ? RDY_REF_state = "Ebabled" : RDY_REF_state = "Disabled";
                                    }
                                    return RDY_REF_state;
                                }
                                catch (Exception ex)
                                {
                                    log.Error(ex.Message);
                                    return RDY_REF_state;
                                }
                            };
                        #endregion
                            //обновим значение счетчика в БД, перед этим проверим не включен ли вентилятор
                            if (vd(record.DB, record.startBit, 1) == "Disabled")
                            {
                                if (VT.UpdateCountValuesConfTB(record))
                                    //сбросим счетчик на частотном преобразователе
                                    if (SendResetBitToPLC(record.resetM))
                                    {
                                        refreshValues = true;
                                        Thread.Sleep(1000);
                                    }
                            }
                        }
                        if(record.value == 0)
                        {

                        }
                    }
                    //если обнаружено превышение счетчика, то не будем записывать считанные данные, а подождем немного и выполнил "грязную работу" еще раз
                    if (refreshValues)
                    {
                        Thread.Sleep(1000);
                        new Thread(DirtyJob).Start();
                    }
                    else
                    {
                        //сохраним значения в БД, если за этот час не было записей
                        int LRCHres = VT.LastRecordCurrentHour(System.DateTime.Now);
                        if(LRCHres== 0 )
                            VT.WriteDataToDB(VentsData);
                        else if (debugBit)
                            VT.WriteDataToDB(VentsData);
                    }
                }
        }
        //read config from ventEnergyConf.tb
        
        private bool ReadDataFromPLC()
        {
            try             
            {
                if (Client.ConnectTo(VentsConst._ipPLC, VentsConst._RACK, VentsConst._SLOT) != 0)
                    throw new Exception("Cant connect to PLC");
                foreach(Vents vent in VentsData)
                {
                    int result;
                    
                    //get all input data getted from FC
                    //0 and 1 bytes is an Input State Word
                    //2 and 3 bytes is an Actual Value (speed)
                    //4 and 5 bytes is a PZD3 (actual current)
                    //6 and 7 bytes is a PZD4 (energy counter)
                    //7 and 8 bytes is a PZD5 (not used) 
                    //etc.
                   
                    byte[] buffer = new byte[VentsConst.ppo2TypeProcessDataSize];
                    byte[] etalonZeroBuffer = Enumerable.Repeat<byte>(0, VentsConst.ppo2TypeProcessDataSize).ToArray();

                    result = Client.DBRead(vent.DB, vent.startBit, VentsConst.ppo2TypeProcessDataSize, buffer); //vent.startBit+6,- PZD4 address
                    if (result != 0)
                        ShowError(result);
                    else
                        //check buffer, if all bytes are 0 - symptom that PLC couldn't be connected to Frequency Converter
                        vent.value = (Enumerable.SequenceEqual(buffer, etalonZeroBuffer) || vent.isEnabled == false) ? -1 : buffer[6] << 8 | buffer[7];
                }
                VentsData.RemoveAll(x => x.value == -1);
                int discResult = Client.Disconnect();
                if (discResult != 0)
                { 
                    ShowError(discResult);
                    return false;
                }
                return true;
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
                return false;
            }
        }
       
        /// <summary>
        /// Send reset M bit to PLC, when PLC get it must reset kWh counter at frequency converter ABB ASC800 -> 16.12 parameter 
        /// </summary>
        /// <param name="mBit"></param>
        /// <returns></returns>
        private bool SendResetBitToPLC(string mBit)
        {
            int discResult;
            bool state = false;
            try
            {
                if (Client.ConnectTo(VentsConst._ipPLC, VentsConst._RACK, VentsConst._SLOT) != 0)
                    throw new Exception("Cant connect to PLC");
                int startByteInt;
                int result;
               // byte[] buffer = new byte[1];
                byte mBitb;
                //we can to write only whole merker byte in PLC (strange library), we need to know start byte and parse our buffer from mBit
                try
                {
                    string startByteStr = mBit.Split('.').First().Remove(0, 1);
                    int bitNumb = Int32.Parse(mBit.Split('.').Last());
                    startByteInt = Int32.Parse(startByteStr);
                    switch (bitNumb)
                    {
                        case 0:
                            mBitb = 1;
                            break;
                        case 1:
                            mBitb = 2;
                            break;
                        case 2:
                            mBitb = 4;
                            break;
                        case 3:
                            mBitb = 8;
                            break;
                        case 4:
                            mBitb = 16;
                            break;
                        case 5:
                            mBitb = 32;
                            break;
                        case 6:
                            mBitb = 64;
                            break;
                        case 7:
                            mBitb = 128;
                            break;
                        default:
                            throw new Exception();
                    }
                }
                catch
                {
                    throw new Exception("Неправильный формат записи столбца 'ResetM'");
                }
                byte[] buffer = new byte[1];
                buffer[0] = mBitb;
                result = Client.MBWrite(startByteInt, 1, buffer);
                if (result != 0)
                {
                    ShowError(result);
                    state = false;
                }
                else
                    state = true;

                discResult = Client.Disconnect();
                if (discResult != 0)
                {
                    ShowError(discResult);
                }
                return state;
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
                discResult = Client.Disconnect();
                if (discResult != 0)
                {
                    ShowError(discResult);
                }
                return false;
            }
        }
        private void ShowError(int Result)
        {
            // This function returns a textual explaination of the error code
            log.Error(Client.ErrorText(Result));
        }
        private void Form1_Shown(Object sender, EventArgs e)
        {

            this.Hide();

        }
    }

}

