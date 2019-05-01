using LibHac.IO.NcaUtils;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
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
                { "Extract as NCAs" , new ExtractAsNCAs(selected)},
                { "Repack as NSP" , new RepackAsNSPWindow(selected)}
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
