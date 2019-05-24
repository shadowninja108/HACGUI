using HACGUI.Extensions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace HACGUI.FirstStart
{
    public delegate PageExtension DerivingPageAction(DerivingPage page);

    /// <summary>
    /// Interaction logic for DerivingPage.xaml
    /// </summary>
    public partial class DerivingPage : PageExtension
    {

        public DerivingPage(DerivingPageAction Run)
        {
            InitializeComponent();

            Loaded += (_, __) =>
                FindRootWindow().Submit(new Task(() =>
                {
                    PageExtension next = Run(this); // asyncronously run the derivation task
                    Dispatcher.Invoke(() => // move to UI thread
                    {
                        next.Loaded += (___, ____) => // wait until the next page has fully loaded (so that it becomes a child of NavigationWindow)
                            next.FindNavigationWindow().RemoveBackEntry(); // remove DerivingPage from backstack so that the user can't see it
                    });
                }));
        }
    }
}
