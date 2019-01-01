using LibHac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace HACGUI.Main.TitleManager.ApplicationWindow.Tabs.Extracts.Extractors
{
    public abstract partial class IExtractorWindow : UserControl
    {
        protected List<Title> SelectedTitles;

        public IExtractorWindow(List<Title> selected)
        {
            SelectedTitles = selected;
        }
    }
}
