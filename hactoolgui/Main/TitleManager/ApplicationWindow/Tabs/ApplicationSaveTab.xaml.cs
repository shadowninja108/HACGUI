using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs
{
    /// <summary>
    /// Interaction logic for ApplicationSaveTab.xaml
    /// </summary>
    public partial class ApplicationSaveTab : UserControl
    {
        private ApplicationElement Element => ApplicationWindow.Current?.Element;

        public ApplicationSaveTab()
        {
            InitializeComponent();

            if (DesignMode.IsInDesignMode(this))
                return;

            Content = new SaveManager.SaveManagerPage(Element.BaseTitleId);
        }
    }
}
