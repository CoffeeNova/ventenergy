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
        VentsTools vt;
        CheckBoxLP cb;
        private readonly Logger log = LogManager.GetCurrentClassLogger();
        public General()
        {
            InitializeComponent();

            VentsTools.onChangeSettingsString += this.UpdateMessageLabel;
            vt = new VentsTools();
            cb = new CheckBoxLP();

            List<FrameworkElement> onlyCheckBoxes = new List<FrameworkElement>();
            List<FrameworkElement> allElem = new List<FrameworkElement>();
            //создадим список всех элементов на форме
            vt.ChildControls(this, allElem);
            //выберем из списка только чекбоксы
            foreach (FrameworkElement elem in allElem)
            {
                if (elem.GetType() == cb.GetType())
                {
                    onlyCheckBoxes.Add(elem);
                }

            }
            try
            {
                foreach (CheckBoxLP chbx in onlyCheckBoxes)
                {
                    if (RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name) != null)
                        if (RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name).ToString() != "")
                        {
                            chbx.IsChecked = true;
                        }
                }
            }
            catch  (Exception ex)
            {
                log.Error(ex.Message + " (чтение из реестра)");
                VentsTools.currentSettingString = "Ошибка чтения реестра. Запустите программу от имени администратора";
            }
        }
        //if ((Int32)RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name) == 1)
        
        private void CheckBox_IsMouseCapturedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            okButton.IsEnabled = true;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            const string value = "1";
            okButton.IsEnabled = false;

            List<FrameworkElement> checkBoxesLP = new List<FrameworkElement>(vt.ListOfCheckBoxes(MainGrid, cb.GetType()));
            try
            {
                if (RegistryWorker.CreateSubKey(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, "Software", VentsConst._PROJECTGROUPNAME) && RegistryWorker.CreateSubKey(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._PROJECTGROUPLOCATION, VentsConst._PROJECTNAME) && RegistryWorker.CreateSubKey(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._PROJECTLOCATION, VentsConst._SETTINGS))
                {
                    //проверяем чекбоксы на IsChecked и записываем/удаляем из реестра
                    foreach (CheckBoxLP chbx in checkBoxesLP)
                    {
                        if (chbx.IsChecked == true)
                        {
                            RegistryWorker.WriteKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name, Microsoft.Win32.RegistryValueKind.DWord, value);

                        }
                        else
                        {
                            if (RegistryWorker.GetKeyValue(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name) != null)
                            {
                                RegistryWorker.DeleteKey(RegistryWorker.WhichRoot.HKEY_LOCAL_MACHINE, VentsConst._SETTINGSLOCATION, chbx.Name);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                log.Error(ex.Message);
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
