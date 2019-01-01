using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace HACGUI.Extensions
{
    public abstract class PageExtension : Page
    {
        public PageExtension() : base()
        {

            Loaded += (owner, args) =>
            {
                NavigationWindow root = FindRoot();

                root.Title = (string)Resources["Title"] ?? GetType().Name;
                root.MinWidth = (double)(Resources["MinWidth"] ?? 0D);
                root.MinHeight = (double)(Resources["MinHeight"] ?? 0D);
                root.AllowDrop = (bool)(Resources["AllowDrop"] ?? false);
            };
        }

        public virtual void OnBack()
        {

        }

        public NavigationWindow FindRoot()
        {
            return Extensions.FindParent<NavigationWindow>(this);
        }
    }
}
