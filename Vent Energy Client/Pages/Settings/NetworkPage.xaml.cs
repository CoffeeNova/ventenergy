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
using System.Linq;

namespace ventEnergy.Pages.Settings
{
    /// <summary>
    /// Interaction logic for NetworkPage.xaml
    /// </summary>
    public partial class NetworkPage : UserControl
    {
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        Thread _cfgthread;
        NetworkAdapters _netwAdapters = new NetworkAdapters();
        ObservableCollection<NetworkAdapters> _cbaItemSource = new ObservableCollection<NetworkAdapters>();
        private System.Threading.Timer _delayTimer;

        private ventEnergyIPSettings _vipsContainer;
        private ventEnergyIPSettings Vips
        {
            get { return _vipsContainer; }
            set
            {
                if (!value.Equals(_vipsContainer))
                {
                    _vipsContainer = value;
                    onVipsChange(_vipsContainer);
                }
                else
                    _vipsContainer = value;
            }
        }

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

        public delegate void del(ventEnergyIPSettings vs);
        public static event del onVipsChange;

        public NetworkPage()
        {
            InitializeComponent();
            VentsTools.onChangeSettingsString += this.UpdateMessageLabel;
            onVipsChange += NetworkPage_onVipsChange;
            Vips = new ventEnergyIPSettings();
            ventEnergyIPSettings.onBelsIPChange += UpdateBelsIP;
            ventEnergyIPSettings.onBelsMaskChange += UpdateBelsMask;
            ventEnergyIPSettings.onBelsGateChange += UpdateBelsGate;
            ventEnergyIPSettings.onWincIPChange += UpdateWincIP;
            ventEnergyIPSettings.onWincMaskChange += UpdateWincMask;
            ventEnergyIPSettings.onWincGateChange += UpdateWincGate;

            //заполним комбобокс всеми сетевыми адаптерами в системе
            if ((_cbaItemSource = _netwAdapters.GetAdapters()) != null)
            {
                belsIPadr.IsEnabled = false;
                belsMask.IsEnabled = false;
                belsGate.IsEnabled = false;
                wincIPadr.IsEnabled = false;
                wincMask.IsEnabled = false;
                wincGate.IsEnabled = false;
                applyBelsbutt.IsEnabled = false;
                applyWincbutt.IsEnabled = false;

                comboBoxAdapters.ItemsSource = _cbaItemSource;
                _cfgthread = new Thread(() => Config());
                _cfgthread.Start();
            }
            else
            {
                _log.Info("Нет доступных сетевых адаптеров");
                VentsTools.currentSettingString = "Нет доступных сетевых адаптеров";
            }

        }

        private void Config()
        {
            ProgressBarPerform = new KeyValuePair<bool, int>(true, 100);

            //прочитаем из реестра guid последнего использованого адаптера, если такового нет оставим пустое и будем ждать пока пользователь выберет на комбобоксе нужный
            string adaptGUID = String.Empty;
            try
            {
                adaptGUID = (string)RegistryWorker.GetKeyValue<string>(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, VentsConst._LASTADPTGUID);
            }
            catch (System.IO.IOException) { }
            catch (Exception ex)
            {
                _log.Error(ex.Message + " (чтение из реестра)");
                VentsTools.currentSettingString = "Ошибка чтения реестра";
            }
            if (adaptGUID == String.Empty)
            {
                VentsTools.currentSettingString = "Выберите сетевой адаптер";
                Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged += new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                _cfgthread.Suspend();
                Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged -= new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
            }
            else
            {
                int index = 0;
                foreach (NetworkAdapters adp in comboBoxAdapters.ItemsSource)
                {
                    if (adaptGUID == adp.GUID)
                    {
                        Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectedIndex = index));
                        break;
                    }
                    index++;
                }
                if ((bool)Dispatcher.Invoke(new Func<bool>(() => comboBoxAdapters.SelectedIndex == -1)))
                {
                    _log.Info(String.Format("Сетевой адаптер GUID={0} недоступен", adaptGUID));
                    VentsTools.currentSettingString = "Последний использованный адаптер недоступен, выберите другой";
                    VentsTools.currentSettingString = "Выберите сетевой адаптер";
                    Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged += new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                    _cfgthread.Suspend();
                    Dispatcher.Invoke(new Action(() => comboBoxAdapters.SelectionChanged -= new SelectionChangedEventHandler(comboBoxAdapters_SelectedIndexChangedSuspended)));
                }
            }
            Vips = GetIPSettings((string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
            bool state1 = false;
            bool state2 = false;
            bool state3 = false;
            NetworkManagement NM = new NetworkManagement();
            //Получим текущие настройки на машине, для wincos network запишем дефолтные
            try
            {
                if (Vips.belsIP == null)
                    Vips.belsIP = NM.GetIP(ref state1, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.belsMask == null)
                    Vips.belsMask = NM.GetSubnetMask(ref state2, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.belsGate == null)
                    Vips.belsGate = NM.GetGateway(ref state3, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.wincIP == null)
                    Vips.wincIP = VentsConst._DEFAULTWINCOSIP;
                if (Vips.wincMask == null)
                    Vips.wincMask = VentsConst._DEFAULTWINCOSMASK;
                if (Vips.wincGate == null)
                    Vips.wincGate = VentsConst._DEFAULTWINCOSGATEWAY;

            }
            catch (Exception ex)
            { _log.Error(ex); }

            Dispatcher.Invoke(new Action(() =>
            {
                belsIPadr.IsEnabled = true;
                belsMask.IsEnabled = true;
                belsGate.IsEnabled = true;
                wincIPadr.IsEnabled = true;
                wincMask.IsEnabled = true;
                wincGate.IsEnabled = true;
                applyBelsbutt.IsEnabled = true;
                applyWincbutt.IsEnabled = true;
            }));

            //загрузим последнюю нажатую кнопку
            string applyButtonClicked = String.Empty;
            try
            {
                applyButtonClicked = RegistryWorker.GetKeyValue<string>(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, VentsConst._LASTAPPLYBUTTONCLICKED);
            }
            catch (System.IO.IOException) { }
            catch (Exception ex)
            {
                _log.Error(ex.Message + " (чтение из реестра)");
                VentsTools.currentSettingString = "Ошибка чтения реестра";
            }
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
                ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
                return;
            }
            VentsTools.currentSettingString = String.Format("Последние использованые настройки: {0}", applyButtonClicked == "BELSOLOD" ? "Сеть Белсолод" : "Сеть Wincos");
            ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
        }

        private ventEnergyIPSettings GetIPSettings(string guid)
        {
            List<string> settingsList = new List<string>();
            ventEnergyIPSettings settings = new ventEnergyIPSettings();
            string[] settingsArr;
            try
            {
                settingsArr = RegistryWorker.GetKeyValue<string[]>(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._ADAPTERSLOCATION, guid);
            }
            catch (System.IO.IOException) { settingsArr = null; }
            catch (Exception ex)
            {
                _log.Error(ex.Message + " (чтение из реестра)");
                VentsTools.currentSettingString = "Ошибка чтения реестра";
                settingsArr = null;
            }
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
            return settings;
        }
        private void UpdateMessageLabel(string currentAction)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
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
            if (_cfgthread.ThreadState == ThreadState.Running || _cfgthread.ThreadState == ThreadState.Suspended)
                return;
            else
            {
                bool state = false;
                NetworkManagement NM = new NetworkManagement();
                Vips = GetIPSettings((comboBoxAdapters.SelectedItem as NetworkAdapters).GUID);
                if (Vips.belsIP == null)
                    Vips.belsIP = NM.GetIP(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.belsMask == null)
                    Vips.belsMask = NM.GetSubnetMask(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.belsGate == null)
                    Vips.belsGate = NM.GetGateway(ref state, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)))[0];
                if (Vips.wincIP == null)
                    Vips.wincIP = VentsConst._DEFAULTWINCOSIP;
                if (Vips.wincMask == null)
                    Vips.wincMask = VentsConst._DEFAULTWINCOSMASK;
                if (Vips.wincGate == null)
                    Vips.wincGate = VentsConst._DEFAULTWINCOSGATEWAY;
            }

        }
        private void comboBoxAdapters_SelectedIndexChangedSuspended(object sender, SelectionChangedEventArgs e)
        {
            _cfgthread.Resume();
        }
        private void comboBoxAdapters_LostFocus(object sender, RoutedEventArgs e)
        {

        }
        private void ApplyBelsolod()
        {
            ProgressBarPerform = new KeyValuePair<bool, int>(true, 100);

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
                    ipmark = NM.SetIP(Vips.belsIP, Vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                    // Thread.Sleep(3000);
                    if (!ipmark)
                    {
                        _log.Error(String.Format("Не удалось изменить параметры сети IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", Vips.belsIP, Vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять IP адрес";

                    }
                    //Установим шлюз
                    gatemark = NM.SetGateway(Vips.belsGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                    // Thread.Sleep(3000);
                    if (!gatemark)
                    {
                        _log.Error(String.Format("Не удалось изменить параметры сети Gateway={0}, adaperName={1}, adapterGuid={2}", Vips.belsGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
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
                        Dispatcher.Invoke(new Action(() => wincRect.Stroke = new SolidColorBrush(Colors.Transparent)));
                        Dispatcher.Invoke(new Action(() => wincRect.Fill = new SolidColorBrush(Colors.Transparent)));

                        //Запишем настройки в реестр
                        //начнем c гуида сетевой
                        try
                        {
                            RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.String, VentsConst._LASTADPTGUID, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
                            //запишем настройки в стринг массиве для этого гуида
                            string[] settArr = new string[] { Vips.belsIP, Vips.belsMask, Vips.belsGate, Vips.wincIP, Vips.wincMask, Vips.wincGate };
                            RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._ADAPTERSLOCATION, Microsoft.Win32.RegistryValueKind.MultiString, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)), settArr);
                            //запишем последнюю нажатую кнопку (выбранную сеть)
                            RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.String, VentsConst._LASTAPPLYBUTTONCLICKED, "BELSOLOD");
                        }
                        catch (Exception ex) { _log.Error(ex.Message); }
                    }
                }
                catch
                {
                    _log.Error(String.Format("Недопустимое значение маски и IP адреса IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", Vips.belsIP, Vips.belsMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                    VentsTools.currentSettingString = "Недопустимое значение маски и IP адреса!";
                    ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
                    Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
                }
            }
            else
            {
                _log.Info("Нажата кнопка \"Применить настройки Белсолод\" без выбранного сетевого адаптера");
                VentsTools.currentSettingString = "Сначала Выберите сетевой адаптер!";

            }
            ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
            Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
        }
        private void ApplyWincos()
        {
            ProgressBarPerform = new KeyValuePair<bool, int>(true, 100);

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

                    ipmark = NM.SetIP(Vips.wincIP, Vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                    //   Thread.Sleep(3000);
                    if (!ipmark)
                    {
                        _log.Error(String.Format("Не удалось изменить параметры сети IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", Vips.wincIP, Vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
                        VentsTools.currentSettingString = "Не удалось поменять IP адрес";

                    }
                    gatemark = NM.SetGateway(Vips.wincGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)));
                    //  Thread.Sleep(3000);
                    if (!gatemark)
                    {
                        _log.Error(String.Format("Не удалось изменить параметры сети Gateway={0}, adaperName={1}, adapterGuid={2}", Vips.wincGate, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID))));
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
                        if (netbiosmark == false)
                            message += "\r\nНе удалось поменять настройки Netbios";
                        VentsTools.currentSettingString = message;
                        Dispatcher.Invoke(new Action(() => wincRect.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() => wincRect.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE8F5FC"))));
                        Dispatcher.Invoke(new Action(() => belsRect.Stroke = new SolidColorBrush(Colors.Transparent)));
                        Dispatcher.Invoke(new Action(() => belsRect.Fill = new SolidColorBrush(Colors.Transparent)));
                        //Запишем настройки в реестр
                        //начнем а гуида сетевой
                        RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.String, VentsConst._LASTADPTGUID, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)));
                        //запишем настройки в стринг массиве для этого гуида
                        string[] settArr = new string[] { Vips.belsIP, Vips.belsMask, Vips.belsGate, Vips.wincIP, Vips.wincMask, Vips.wincGate };
                        RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._ADAPTERSLOCATION, Microsoft.Win32.RegistryValueKind.MultiString, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).GUID)), settArr);
                        //запишем последнюю нажатую кнопку (выбранную сеть)
                        RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.String, VentsConst._LASTAPPLYBUTTONCLICKED, "WINCOS");
                    }

                }
                catch
                {
                    _log.Error(String.Format("Недопустимое значение маски и IP адреса IP={0}, mask={1}, adaperName={2}, adapterGuid={3}", Vips.wincIP, Vips.wincMask, (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull)), (string)Dispatcher.Invoke(new Func<string>(() => (comboBoxAdapters.SelectedItem as NetworkAdapters).AdapterDescriptionFull))));
                    VentsTools.currentSettingString = "Недопустимое значение маски и IP адреса!";
                    ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
                    Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
                }
            }
            else
            {
                _log.Info("Нажата кнопка \"Применить настройки Белсолод\" без выбранного сетевого адаптера");
                VentsTools.currentSettingString = "Сначала Выберите сетевой адаптер!";
            }
            ProgressBarPerform = new KeyValuePair<bool, int>(false, 100);
            Dispatcher.Invoke(new Action(() => { applyBelsbutt.IsEnabled = true; applyWincbutt.IsEnabled = true; }));
        }
        private void applyBelsbutt_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            applyWincbutt.IsEnabled = false;
            _cfgthread = new Thread(() => ApplyBelsolod());
            _cfgthread.Start();
        }

        private void applyWincsbutt_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            applyBelsbutt.IsEnabled = false;
            _cfgthread = new Thread(() => ApplyWincos());
            _cfgthread.Start();
        }

        private void belsIPadr_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.belsIP = belsIPadr.Text;
        }

        private void belsMask_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.belsMask = belsMask.Text;
        }

        private void belsGate_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.belsGate = belsGate.Text;
        }

        private void wincIPadr_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.wincIP = wincIPadr.Text;
        }

        private void wincMask_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.wincMask = wincMask.Text;
        }

        private void wincGate_LostFocus(object sender, RoutedEventArgs e)
        {
            Vips.wincGate = wincGate.Text;
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
