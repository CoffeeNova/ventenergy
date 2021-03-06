﻿using System;
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
    /// Логика взаимодействия для VE_TextBox.xaml
    /// </summary>
    public partial class VE_TextBox : TextBox
    {
        public enum TextBoxType
        {
            TimeTextBox,
            IndicatorTextBox
        }
        public VE_TextBox(TextBoxType tbType, double width, double height, string text)
        {
            InitializeComponent();

            switch (tbType)
            {
                case TextBoxType.TimeTextBox:
                    InitPropForTimeTextBox();
                    break;
                case TextBoxType.IndicatorTextBox:
                    InitPropForIndicatorTextBox();
                    break;
            }
            Width = width;
            Height = height;
            Text = text;
        }

        private void InitPropForTimeTextBox()
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Top;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            IsReadOnly = true;
            VerticalContentAlignment = VerticalAlignment.Center;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            FontSize = 14;
            FontWeight = FontWeights.Medium;
            Style = (Style)this.FindResource("TextBoxStyleBlueBackgrReadonly");
        }
        private void InitPropForIndicatorTextBox()
        {
            VerticalAlignment = System.Windows.VerticalAlignment.Top;
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            IsReadOnly = true;
            VerticalContentAlignment = VerticalAlignment.Center;
            HorizontalContentAlignment = HorizontalAlignment.Center;
            FontSize = 14;
            FontWeight = FontWeights.Medium;
            Style = (Style)this.FindResource("TextBoxStyleNoShadow");
        }
    }
}
