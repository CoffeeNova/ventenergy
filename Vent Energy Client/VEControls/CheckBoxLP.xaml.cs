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
using System.Windows.Controls.Primitives;

namespace ventEnergy.VEControls
{
    /// <summary>
    /// Логика взаимодействия для CheckBoxLP.xaml
    /// </summary>
    public partial class CheckBoxLP : CheckBox
    {
        private string[] tag = new string[5];
        Action anAction;
        public enum Action
        {
            //использовать для записи в реестр
            Create = 0,
            //использовать для удаления из реестра
            Delete = 1,
        }
        public CheckBoxLP()
        {
            Name = "CheckBoxLP";
            Content = "ChecBoxLP";
            InitializeComponent();
        }

        public Action ToAction
        {
            get
            {
                return anAction;
            }
            set
            {
                anAction = value;
            }
        }
        public string[] Tags
        {
            get
            {
                return tag;
            }
            set
            {
                tag = value;
            }

        }

    }
}
