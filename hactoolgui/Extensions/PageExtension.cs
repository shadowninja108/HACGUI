using System.Windows.Controls;
using System.Windows.Navigation;

namespace HACGUI.Extensions
{
    public abstract class PageExtension : Page
    {
        public PageExtension() : base()
        {

            Loaded += (owner, args) =>
            {
                NavigationWindow root = FindNavigationWindow();

                root.Title = (string)Resources["Title"] ?? GetType().Name;
                root.MinWidth = (double)(Resources["MinWidth"] ?? 0D);
                root.MinHeight = (double)(Resources["MinHeight"] ?? 0D);
                root.AllowDrop = (bool)(Resources["AllowDrop"] ?? false);
            };
        }

        public virtual void OnBack()
        {

        }

        public NavigationWindow FindNavigationWindow()
        {
            return Extensions.FindParent<NavigationWindow>(this);
        }

        public RootWindow FindRootWindow()
        {
            return Extensions.FindParent<RootWindow>(this);
        }
    }
}
