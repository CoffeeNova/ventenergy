using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using ventEnergy.VEControls;
using NWTweak;
using NLog;

namespace ventEnergy.Pages.Settings
{
    /// <summary>
    /// Логика взаимодействия для General.xaml
    /// </summary>
    public partial class General : UserControl
    {
        VentsTools _vt;
        CheckBoxLP _cb;
        private readonly Logger _log = LogManager.GetCurrentClassLogger();
        public General()
        {
            InitializeComponent();

            VentsTools.onChangeSettingsString += this.UpdateMessageLabel;
            _vt = new VentsTools();
            _cb = new CheckBoxLP();

            var onlyCheckBoxes = new List<FrameworkElement>();
            var allElem = new List<FrameworkElement>();
            //создадим список всех элементов на форме
            _vt.ChildControls(this, allElem);
            //выберем из списка только чекбоксы
            foreach (FrameworkElement elem in allElem)
            {
                if (elem.GetType() == _cb.GetType())
                    onlyCheckBoxes.Add(elem);
            }
            try
            {
                foreach (CheckBoxLP chbx in onlyCheckBoxes)
                {
                    try
                    {
                        if (RegistryWorker.GetKeyValue<int>(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, chbx.Name) != 0)
                            chbx.IsChecked = true;
                    }
                    catch (System.IO.IOException ex)
                    {
                        chbx.IsChecked = false;
                    }

                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message + " (чтение из реестра)");
                VentsTools.currentSettingString = "Ошибка чтения реестра";
            }
        }

        private void CheckBox_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            okButton.IsEnabled = true;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            okButton.IsEnabled = false;

            var checkBoxesLP = new List<FrameworkElement>(_vt.ListOfCheckBoxes(MainGrid, _cb.GetType()));
            try
            {
                if (RegistryWorker.CreateSubKey(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION))
                {
                    //проверяем чекбоксы на IsChecked и записываем/удаляем из реестра
                    foreach (CheckBoxLP chbx in checkBoxesLP)
                    {
                        if (chbx.IsChecked == true)
                            RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.DWord, chbx.Name, 1);
                        else
                            RegistryWorker.WriteKeyValue(Microsoft.Win32.RegistryHive.LocalMachine, VentsConst._SETTINGSLOCATION, Microsoft.Win32.RegistryValueKind.DWord, chbx.Name, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex.Message);
            }
        }
        private void UpdateMessageLabel(string currentAction)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                MsgLabel.Text = currentAction;
                VentsTools.currentSettingString = "Ошибка записи/чтения реестра. Запустите программу от имени администратора";
            }));
        }
    }
}
