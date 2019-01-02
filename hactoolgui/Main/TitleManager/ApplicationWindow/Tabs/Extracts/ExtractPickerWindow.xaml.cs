using HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors;
using LibHac;
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
using System.Windows.Shapes;

namespace HACGUI.Main.TitleManager.Application.Tabs.Extracts.Extractors
{
    /// <summary>
    /// Interaction logic for ExtractWindow.xaml
    /// </summary>
    public partial class ExtractPickerWindow : Window
    {
        public Dictionary<string, IExtractorWindow> Extractors { get; set; }
        public string Selected { get; set; }

        protected List<Nca> SelectedTitles;

        public ExtractPickerWindow(List<Nca> selected)
        {
            InitializeComponent();
            Extractors = new Dictionary<string, IExtractorWindow>
            {
                {"Extract as NCAs" , new ExtractAsNCAs(selected)},
                {"Repack as NSP" , new RepackAsNSPWindow(selected)}
            };

            SelectedTitles = selected;

            DataContext = this;

            Selector.SelectionChanged += (_, __) =>
            {
                Frame.Content = Extractors[Selected];
            };
        }
    }
}
