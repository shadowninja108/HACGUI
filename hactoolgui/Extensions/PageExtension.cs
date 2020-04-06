
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
                RootWindow root = FindRootWindow();

                root.Title = (string)Resources["Title"] ?? GetType().Name;
                root.MinWidth = (double)(Resources["MinWidth"] ?? 0D);
                root.MinHeight = (double)(Resources["MinHeight"] ?? 0D);
            };
        }
        public virtual void OnBack()
        {

        }
        public NavigationWindow FindNavigationWindow()
        {
            return this.FindParent<NavigationWindow>();
        }


        public RootWindow FindRootWindow()
        {
            return this.FindParent<RootWindow>();
        }
    }
}
