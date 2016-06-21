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

namespace ventEnergy.VEControls
{
    /// <summary>
    /// Логика взаимодействия для VE_Label.xaml
    /// </summary>
    public partial class VE_Label : Label
    {
        public enum LabelType
        {
            TimeLabel,
            IndicatorLabel
        }

        public VE_Label(LabelType lType, double width, double height, string content)
        {
            InitializeComponent();

            switch (lType)
            {
                case LabelType.TimeLabel:
                    InitPropForTimeLabel();
                    break;
                case LabelType.IndicatorLabel:
                    InitPropForIndicatorLabel();
                    break;
            }
            Width = width;
            Height = height;
            Content = content;
        }
        private void InitPropForTimeLabel()
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Top;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            VerticalContentAlignment = VerticalAlignment.Top;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            FontSize = 14;
            FontWeight = FontWeights.Bold;
            BorderBrush = Brushes.Black;
        }

        private void InitPropForIndicatorLabel()
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Top;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            VerticalContentAlignment = VerticalAlignment.Top;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            FontSize = 14;
            FontWeight = FontWeights.Bold;
            BorderBrush = Brushes.Black;
        }
    }
}
