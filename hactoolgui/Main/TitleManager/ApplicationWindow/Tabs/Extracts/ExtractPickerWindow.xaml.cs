using LibHac;
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

        protected List<SwitchFsNca> SelectedTitles;

        public ExtractPickerWindow(List<SwitchFsNca> selected)
        {
            InitializeComponent();
            Extractors = new Dictionary<string, IExtractorWindow>
            {
                { "Extract as NCAs" , new ExtractAsNCAs(selected)},
                { "Extract partitions", new ExtractPartition(selected) },
                { "Repack as NSP" , new RepackAsNSPWindow(selected) },
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
