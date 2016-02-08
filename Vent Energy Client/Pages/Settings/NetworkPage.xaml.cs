using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using NWTweak;
using NLog;
using System.Threading;
using System.Collections.ObjectModel;

namespace ventEnergy.Pages.Settings
{
    /// <summary>
    /// Interaction logic for NetworkPage.xaml
    /// </summary>
    public partial class NetworkPage : UserControl
    {
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        Thread Cfgthread;
        NetworkAdapters netwAdapters = new NetworkAdapters();
        ObservableCollection<NetworkAdapters> CBAItemSource = new ObservableCollection<NetworkAdapters>();
        private System.Threading.Timer waitingModeDelay; 
      
        private ventEnergyIPSettings vipsContainer;
        private ventEnergyIPSettings vips
        {
            get { return vipsContainer; }
            set
            {
                if (!value.Equals(vipsContainer))
                {
                    vipsContainer = value;
                    onVipsChange(vipsContainer);
                }
                else
                    vipsContainer = value;
            }
        }
        public delegate void del(ventEnergyIPSettings vs);
        public static event del onVipsChange;

        public NetworkPage()
        {
            InitializeComponent();
            VentsTools.onChangeSettingsString += this.UpdateMessageLabel;
            onVipsChange += NetworkPage_onVipsChange;
            vips = new ventEnergyIPSettings();
            ventEnergyIPSettings.onBelsIPChange += UpdateBelsIP;
            ventEnergyIPSettings.onBelsMaskChange += UpdateBelsMask;
            ventEnergyIPSettings.onBelsGateChange += UpdateBelsGate;
            ventEnergyIPSettings.onWincIPChange += UpdateWincIP;
            ventEnergyIPSettings.onWincMaskChange += UpdateWincMask;
            ventEnergyIPSettings.onWincGateChange += UpdateWincGate;
            

            //displayedSettings = new ventEnergyIPSettings();

            
            //заполним комбобокс всеми сетевыми адаптерами в системе
            if((CBAItemSource = netwAdapters.GetAdapters()) !=null )
            {
                belsIPadr.IsEnabled = false;
                belsMask.IsEnabled = false;
                belsGate.IsEnabled = false;
                wincIPadr.IsEnabled = false;
                wincMask.IsEnabled = false;
                wincGate.IsEnabled = false;

                //belsCurValIPButt.IsEnabled = false;
                //belsCurValMaskButt.IsEnabled = false;
                //belsCurValGatewayButt.IsEnabled = false;
                //wincCurValIPButt.IsEnabled = false;
                //wincCurValMaskButt.IsEnabled = false;
                //wincCurValGatewayButt.IsEnabled = false;

                applyBelsbutt.IsEnabled = false;
                applyWincbutt.IsEnabled = false;

                 comboBoxAdapters.ItemsSource = CBAItemSource;
                 Cfgthread = new Thread(() => Config());
                 Cfgthread.Start();
            }
            else
            {
                log.Info("Нет доступных сетевых адаптеров");
                VentsTools.currentSettingString = "Нет доступных сетевых адаптеров";
            }
            
        }

        private void Config()
        {
            //запустим таймер с задержкой в 1с для отображения прогрессбара (бесячий кругалек, когда все зависло)
            waitingModeDelay = new System.Threading.Timer((Object state) =>  Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = true; pb1.Visibility = Visibility.Visible; })), null, 1000, Timeout.Infinite);
            

            //прочитаем из реестра guid последнего использованого адаптера, если такового нет оставим пустое и будем ждать пока пользователь выберет на комбобоксе нужный
            string adaptGUID = (string)RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._PROJECTLOCATION, VentsConst._SETTINGS, VentsConst._LASTADPTGUID);
            if (adaptGUID == null)
            {
                VentsTools.currentSettingString = "Выберите сетевой адаптер";
                Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged += new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                Cfgthread.Suspend();
                Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged -= new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
            }
            else
            {
                int index=0;
                foreach(NetworkAdapters adp in comboBoxAdapters.ItemsSource)
                {
                    if(adaptGUID == adp.GUID)
                    {
                        Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectedIndex = index));
                        break;
                    }
                    index++;
                }
                if((bool)Dispatcher.Invoke(new Func<bool>(()=>comboBoxAdapters.SelectedIndex == -1)))
                {
                    log.Info(String.Format("Сетевой адаптер GUID={0} недоступен", adaptGUID));
                    VentsTools.currentSettingString = "Последний использованный адаптер недоступен, выберите другой";
                    VentsTools.currentSettingString = "Выберите сетевой адаптер";
                    Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged += new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                    Cfgthread.Suspend();
                    Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged -= new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                }
            }
             vips = GetIPSettings((string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
             bool state1 = false;
            bool state2 = false;
            bool state3 = false;
             NetworkManagement NM = new NetworkManagement();
             //Получим текущие настройки на машине, для wincos network запишем дефолтные
             try
             {
                 if (vips.belsIP == null)
                     vips.belsIP = NM.GetIP(ref state1, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                 if (vips.belsMask == null)
                     vips.belsMask = NM.GetSubnetMask(ref state2, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                 if (vips.belsGate == null)
                     vips.belsGate = NM.GetGateway(ref state3, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                 if (vips.wincIP == null)
                     vips.wincIP = VentsConst._DEFAULTWINCOSIP;
                 if (vips.wincMask == null)
                     vips.wincMask = VentsConst._DEFAULTWINCOSMASK;
                 if (vips.wincGate == null)
                     vips.wincGate = VentsConst._DEFAULTWINCOSGATEWAY;
                
             }
            catch(Exception ex)
             { log.Error(ex); }
             Dispatcher.Invoke(new Action(() => {
                 belsIPadr.IsEnabled = true;
                 belsMask.IsEnabled = true;
                 belsGate.IsEnabled = true;
                 wincIPadr.IsEnabled = true;
                 wincMask.IsEnabled = true;
                 wincGate.IsEnabled = true;

                 //belsCurValIPButt.IsEnabled = true;
                 //belsCurValMaskButt.IsEnabled = true;
                 //belsCurValGatewayButt.IsEnabled = true;
                 //wincCurValIPButt.IsEnabled = true;
                 //wincCurValMaskButt.IsEnabled = true;
                 //wincCurValGatewayButt.IsEnabled = true;

                 applyBelsbutt.IsEnabled = true;
                 applyWincbutt.IsEnabled = true;
             }));

            //загрузим последнюю нажатую кнопку
            string applyButtonClicked = (string)RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._PROJECTLOCATION, VentsConst._SETTINGS, VentsConst._LASTAPPLYBUTTONCLICKED);
            if (applyButtonClicked == "BELSOLOD")
                Dispatcher.Invoke(new Action(() =>
                {
                    belsRect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"));
                    belsRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"));
                }));
           else if (applyButtonClicked == "WINCOS")
                Dispatcher.Invoke(new Action(() =>
                {
                    wincRect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"));
                    wincRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"));
                }));
            else
            {
                VentsTools.currentSettingString = "Выберите сеть для работы";
                Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
                waitingModeDelay.Dispose();
                return;
            }
            VentsTools.currentSettingString = String.Format("Последние использованые настройки: {0}", applyButtonClicked == "BELSOLOD" ? "Сеть Белсолод" : "Сеть Wincos");
            Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
            waitingModeDelay.Dispose();
        }

        private ventEnergyIPSettings GetIPSettings(string guid)
        {
            List<string> settingsList = new List<string>();
            ventEnergyIPSettings settings = new ventEnergyIPSettings();
            string[] settingsArr;           
                try
                {              
                    settingsArr = (string[])RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._PROJECTLOCATION, VentsConst._ADAPTERS, guid);
                    if (settingsArr != null)
                    {
                        for (int i = 0; i < VentsConst._CLIENSETTINGSCOUNT; i++)
                        {
                            if (i < settingsArr.Length)
                                settingsList.Add(settingsArr[i]);
                            else
                                settingsList.Add("");
                        }
                        settings.belsIP = settingsList[0];
                        settings.belsMask = settingsList[1];
                        settings.belsGate = settingsList[2];
                        settings.wincIP = settingsList[3];
                        settings.wincMask = settingsList[4];
                        settings.wincGate = settingsList[5];
                    }
                    //else
                    //    settings = null;
                }
                catch (Exception ex)
                {
                    log.Error(ex.Message);
                }
            return settings;
        }
        private void UpdateMessageLabel(string currentAction)
        {
             Dispatcher.BeginInvoke(new Action(() => {
                     MsgLabel.Text = currentAction;
             }));
        }
        #region UPDATE NETWORK PARAMETRS
        private void UpdateBelsIP(string ip)
        {
            Dispatcher.Invoke(new Action(() => belsIPadr.Text = ip));
        }
        private void UpdateBelsMask(string ip)
        {
            Dispatcher.Invoke(new Action(() => belsMask.Text = ip));

        }
        private void UpdateBelsGate(string ip)
        {
            Dispatcher.Invoke(new Action(() => belsGate.Text = ip));

        }
        private void UpdateWincIP(string ip)
        {
            Dispatcher.Invoke(new Action(() => wincIPadr.Text = ip));

        }
        private void UpdateWincMask(string ip)
        {
            Dispatcher.Invoke(new Action(() => wincMask.Text = ip));
        }
        private void UpdateWincGate(string ip)
        {
            Dispatcher.Invoke(new Action(() => wincGate.Text = ip));
        }
        #endregion
        private void comboBoxAdapters_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Cfgthread.ThreadState == ThreadState.Running || Cfgthread.ThreadState == ThreadState.Suspended)
                return;
            else
            {
                bool state = false;
                NetworkManagement NM = new NetworkManagement();
                vips = GetIPSettings((comboBoxAdapters.SelectedItem as NetworkAdapters).GUID);
                if (vips.belsIP == null)
                    vips.belsIP = NM.GetIP(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (vips.belsMask == null)
                    vips.belsMask = NM.GetSubnetMask(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (vips.belsGate == null)
                    vips.belsGate = NM.GetGateway(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (vips.wincIP == null)
                    vips.wincIP = VentsConst._DEFAULTWINCOSIP;
                if (vips.wincMask == null)
                    vips.wincMask = VentsConst._DEFAULTWINCOSMASK;
                if (vips.wincGate == null)
                    vips.wincGate = VentsConst._DEFAULTWINCOSGATEWAY;
            }

        }
        private void comboBoxAdapters_SelectedIndexChangedSuspended(object sender, SelectionChangedEventArgs e)
        {
            Cfgthread.Resume();
        }
        private void comboBoxAdapters_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        private void ApplyBelsolod()
        {
            //запустим таймер с задержкой в 0.1с для отображения прогрессбара (бесячий кругалек, когда все зависло)
            waitingModeDelay = new System.Threading.Timer((Object state) => Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = true; pb1.Visibility = Visibility.Visible; })), null, 100, Timeout.Infinite);
            
            if ((object)Dispatcher.Invoke(new Func<object>(() => { return comboBoxAdapters.SelectedValue; })) != null)
            {
                NetworkManagement NM = new NetworkManagement();
                try
                {
                    bool ipmark = false;
                    bool gatemark = false;
                    bool netbiosmark = true;

                    //Проверим состояние NetBios (если не Enabled, то включим)
                    if (NM.GetTcpipNetbiosState((string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull))) != 1)
                        netbiosmark = NM.SetTcpipNetbios(1, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));

                    //Установим IP
                    ipmark = NM.SetIP(vips.belsIP, vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                   // Thread.Sleep(3000);
                    if (!ipmark)
                    {
                        log.Error(String.Format("Не удалось изменить параметры сети IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", vips.belsIP, vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять IP адрес";

                    }
                    //Установим шлюз
                    gatemark = NM.SetGateway(vips.belsGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                   // Thread.Sleep(3000);
                    if (!gatemark)
                    {
                        log.Error(String.Format("Не удалось изменить параметры сети Gateway={0}, adaperName={1}, adapterGuid={2}", vips.belsGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять шлюз";
                    }
                    

                    if (!ipmark && !gatemark)
                        VentsTools.currentSettingString = "Не удалось изменить настройки сети";
                    else
                    {
                        bool compl = false;
                        string message = "Текущий IP адрес - ";

                        message += NM.GetIP(ref compl, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                        message += "\r\nТекущая сеть: Белсолод";
                        if (netbiosmark == false)
                            message += "\r\nНе удалось поменять настройки Netbios";
                        VentsTools.currentSettingString = message;
                        Dispatcher.Invoke(new Action(() => belsRect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() => belsRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() =>  wincRect.Stroke = new SolidColorBrush(Colors.Transparent)));
                        Dispatcher.Invoke(new Action(() => wincRect.Fill = new SolidColorBrush(Colors.Transparent)));

                        //Запишем настройки в реестр
                        //начнем c гуида сетевой
                        try
                        {
                            RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, VentsConst._LASTADPTGUID, Microsoft.Win32.RegistryValueKind.String, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
                            //запишем настройки в стринг массиве для этого гуида
                            string[] settArr = new string[] { vips.belsIP, vips.belsMask, vips.belsGate, vips.wincIP, vips.wincMask, vips.wincGate };
                            RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._ADAPTERSLOCATION, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)), Microsoft.Win32.RegistryValueKind.MultiString, settArr);
                            //запишем последнюю нажатую кнопку (выбранную сеть)
                            RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, VentsConst._LASTAPPLYBUTTONCLICKED, Microsoft.Win32.RegistryValueKind.String, "BELSOLOD");
                        }
                        catch (Exception ex) { log.Error(ex.Message); }
                    }
                }
                catch
                {
                    log.Error(String.Format("Недопустимое значение маски и IP адреса IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", vips.belsIP, vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                    VentsTools.currentSettingString = "Недопустимое значение маски и IP адреса!";
                    Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
                    waitingModeDelay.Dispose();
                    Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
                }
            }
            else
            {
                log.Info("Нажата кнопка \"Применить настройки Белсолод\" без выбранного сетевого адаптера");
                VentsTools.currentSettingString = "Сначала Выберите сетевой адаптер!";

            }
            Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
            waitingModeDelay.Dispose();
            Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
        }
        private void ApplyWincos()
        {
            //запустим таймер с задержкой в 0.1с для отображения прогрессбара (бесячий кругалек, когда все зависло)
            waitingModeDelay = new System.Threading.Timer((Object state) => Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = true; pb1.Visibility = Visibility.Visible; })), null, 100, Timeout.Infinite);

            if ((object)Dispatcher.Invoke(new Func<object>(() => { return comboBoxAdapters.SelectedValue; })) != null)
            {
                NetworkManagement NM = new NetworkManagement();
                try
                {
                    bool ipmark = false;
                    bool gatemark = false;
                    bool netbiosmark = true;

                    //Проверим Netbios, если не равен DISABLED, то выключим
                    if (NM.GetTcpipNetbiosState((string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull))) != 2)
                        netbiosmark = NM.SetTcpipNetbios(2, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));

                    ipmark = NM.SetIP(vips.wincIP, vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                 //   Thread.Sleep(3000);
                    if (!ipmark)
                    {
                        log.Error(String.Format("Не удалось изменить параметры сети IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", vips.wincIP, vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять IP адрес";

                    }
                    gatemark = NM.SetGateway(vips.wincGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                  //  Thread.Sleep(3000);
                    if (!gatemark)
                    {
                        log.Error(String.Format("Не удалось изменить параметры сети Gateway={0}, adaperName={1}, adapterGuid={2}", vips.wincGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять шлюз";
                    }
                    
      
                    if (!ipmark && !gatemark)
                        VentsTools.currentSettingString = "Не удалось изменить настройки сети";
                    else
                    {
                        bool compl = false;
                        string message = "Текущий IP адрес - ";
                        message += NM.GetIP(ref compl, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                        message += "\r\nТекущая сеть: Wincos";
                        if(netbiosmark == false)
                            message += "\r\nНе удалось поменять настройки Netbios";
                        VentsTools.currentSettingString = message;
                        Dispatcher.Invoke(new Action(() => wincRect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() => wincRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() => belsRect.Stroke = new SolidColorBrush(Colors.Transparent)));
                        Dispatcher.Invoke(new Action(() => belsRect.Fill = new SolidColorBrush(Colors.Transparent)));
                        //Запишем настройки в реестр
                        //начнем а гуида сетевой
                        RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, VentsConst._LASTADPTGUID, Microsoft.Win32.RegistryValueKind.String, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
                        //запишем настройки в стринг массиве для этого гуида
                        string[] settArr = new string[] { vips.belsIP, vips.belsMask, vips.belsGate, vips.wincIP, vips.wincMask, vips.wincGate };
                        RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._ADAPTERSLOCATION, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)), Microsoft.Win32.RegistryValueKind.MultiString, settArr);
                        //запишем последнюю нажатую кнопку (выбранную сеть)
                        RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, VentsConst._LASTAPPLYBUTTONCLICKED, Microsoft.Win32.RegistryValueKind.String, "WINCOS");
                    }

                }
                catch
                {
                    log.Error(String.Format("Недопустимое значение маски и IP адреса IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", vips.wincIP, vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull))));
                    VentsTools.currentSettingString = "Недопустимое значение маски и IP адреса!";
                    Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
                    waitingModeDelay.Dispose();
                    Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
                }
            }
            else
            {
                log.Info("Нажата кнопка \"Применить настройки Белсолод\" без выбранного сетевого адаптера");
                VentsTools.currentSettingString = "Сначала Выберите сетевой адаптер!";
            }
            Dispatcher.Invoke(new Action(delegate { pb1.IsIndeterminate = false; pb1.Visibility = Visibility.Hidden; }));
            waitingModeDelay.Dispose();
            Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
        }
        private void applyBelsbutt_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            applyWincbutt.IsEnabled = false;
            Cfgthread = new Thread(() => ApplyBelsolod());
            Cfgthread.Start();
        }

        private void applyWincsbutt_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            applyBelsbutt.IsEnabled = false;
            Cfgthread = new Thread(() => ApplyWincos());
            Cfgthread.Start();
        }

        private void belsIPadr_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.belsIP = belsIPadr.Text;
        }

        private void belsMask_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.belsMask = belsMask.Text;
        }

        private void belsGate_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.belsGate = belsGate.Text;
        }

        private void wincIPadr_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.wincIP = wincIPadr.Text;
        }

        private void wincMask_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.wincMask = wincMask.Text;
        }

        private void wincGate_LostFocus(object sender, RoutedEventArgs e)
        {
            vips.wincGate = wincGate.Text;
        }
        
        private void NetworkPage_onVipsChange(ventEnergyIPSettings v)
        {
            Dispatcher.Invoke(new Action(() => belsIPadr.Text = v.belsIP));
            Dispatcher.Invoke(new Action(() => belsMask.Text = v.belsMask));
            Dispatcher.Invoke(new Action(() => belsGate.Text = v.belsGate));
            Dispatcher.Invoke(new Action(() => wincIPadr.Text = v.wincIP));
            Dispatcher.Invoke(new Action(() => wincMask.Text = v.wincMask));
            Dispatcher.Invoke(new Action(() => wincGate.Text = v.wincGate));
        }
    }
    
}
