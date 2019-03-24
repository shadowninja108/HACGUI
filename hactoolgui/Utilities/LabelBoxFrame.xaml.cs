using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HACGUI.Utilities
{
    /// <summary>
    /// Interaction logic for LabelBoxFrame.xaml
    /// </summary>
    public partial class LabelBoxFrame : UserControl
    {
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
            "Label", typeof(string), typeof(LabelBoxFrame));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(LabelBoxFrame));

        public static readonly DependencyProperty Ratio1Property = DependencyProperty.Register(
            "Ratio1", typeof(int), typeof(LabelBoxFrame));

        public static readonly DependencyProperty Ratio2Property = DependencyProperty.Register(
            "Ratio2", typeof(int), typeof(LabelBoxFrame));


        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public int Ratio1
        {
            get => (int)GetValue(Ratio1Property);
            set => SetValue(Ratio1Property, value);
        }

        public int Ratio2
        {
            get => (int)GetValue(Ratio2Property);
            set => SetValue(Ratio2Property, value);
        }

        public LabelBoxFrame()
        {
            InitializeComponent();

            Loaded += (_, __) => Setup();
        }

        public LabelBoxFrame(string label, string text, int ratio1, int ratio2)
        {
            InitializeComponent();

            Label = label;
            Text = text;
            Ratio1 = ratio1;
            Ratio2 = ratio2;
            SetValue(Ratio2Property, ratio2);

            Loaded += (_, __) => Setup();
        }

        private void Setup()
        {
            ColumnDefinition c1 = new ColumnDefinition()
            {
                Width = new GridLength(Ratio1, GridUnitType.Star)
            };
            ColumnDefinition c2 = new ColumnDefinition()
            {
                Width = new GridLength(Ratio2, GridUnitType.Star)
            };

            Frame.ColumnDefinitions.Add(c1);
            Frame.ColumnDefinitions.Add(c2);

        }
    }
}
