using HACGUI.Extensions;
using HACGUI.FirstStart;
using HACGUI.Main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace HACGUI
{
    /// <summary>
    /// Interaction logic for RootWindow.xaml
    /// </summary>
    public partial class RootWindow : NavigationWindow
    {
        private static Theme _theme;

        public static Theme CurrentTheme
        {
            get { return _theme; }
            set { _theme = value; CurrentThemeChanged(null, EventArgs.Empty); }
        }

        public static event EventHandler CurrentThemeChanged;

        private string[] LightThemeDictPaths = new string[] {  };
        private string[] DarkThemeDictPaths = new string[] { };

        private List<ResourceDictionary> LightThemeDicts, DarkThemeDicts;

        public RootWindow()
        {
            InitializeComponent();

            LightThemeDicts = new List<ResourceDictionary>();
            DarkThemeDicts = new List<ResourceDictionary>();

            foreach (string path in LightThemeDictPaths)         
                LightThemeDicts.Add(
                    new ResourceDictionary() { Source = new Uri($"pack://application:,,,/{path}", UriKind.RelativeOrAbsolute) }
                );

            foreach (string path in DarkThemeDictPaths)
                DarkThemeDicts.Add(
                    new ResourceDictionary() { Source = new Uri($"pack://application:,,,/{path}", UriKind.RelativeOrAbsolute) }
                );

            CurrentThemeChanged += (_0,_1) =>
            {
                Collection<ResourceDictionary> MergedDictionaries = Application.Current.Resources.MergedDictionaries;
                switch (CurrentTheme)
                {
                    case Theme.Dark:
                        Background = Brushes.Black;

                        foreach (ResourceDictionary dict in LightThemeDicts) // Remove light theme dictionaries 
                            MergedDictionaries.Remove(dict);
                        foreach (ResourceDictionary dict in DarkThemeDicts) // Add dark theme dictionaries
                            MergedDictionaries.Add(dict);
                        break;
                    case Theme.Light:
                        Background = Brushes.White;

                        foreach (ResourceDictionary dict in DarkThemeDicts) // Remove dark theme dictionaries
                            MergedDictionaries.Remove(dict);
                        foreach (ResourceDictionary dict in LightThemeDicts) // Add light theme dictionaries
                            MergedDictionaries.Add(dict);
                        break;
                }
            };

            CurrentTheme = Theme.Light;

            Loaded += (_1, _2) =>
            {
                var settings = Properties.Settings.Default;
                String path = settings.InstallPath;
                if (!String.IsNullOrEmpty(path))
                    Navigate(new MainWindow());
                else
                    Navigate(new Intro());


                // Image may be needed at any time, and loading it every time would be dumb
                Image arrowBlack = new Image() { Source = new BitmapImage(new Uri("/Resources/ArrowBlack.png", UriKind.Relative)) };
                Image arrowWhite = new Image() { Source = new BitmapImage(new Uri("/Resources/ArrowWhite.png", UriKind.Relative)) };


                // Called whenever a transition has finished
                Navigated += (_3, args) =>
                {
                    Page page = Content as Page;

                    /*Button moonButton = new Button()
                    {
                        Width = 60,
                        Height = 60,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Background = Brushes.Transparent,
                        BorderBrush = Brushes.Transparent,
                    };

                    // Update icon when theme changes
                    CurrentThemeChanged += (_4, _5) =>
                    {
                        switch (CurrentTheme)
                        {
                            case Theme.Light:
                                moonButton.Content = moonBlack;
                                break;
                            case Theme.Dark:
                                moonButton.Content = moonWhite;
                                break;
                        }
                    };

                    CurrentTheme = CurrentTheme; // invoke CurrentThemeChanged to set the initial image

                    // Change theme when pressed
                    moonButton.Click += (_4, _5) =>
                    {
                        switch (CurrentTheme)
                        {
                            case Theme.Light:
                                CurrentTheme = Theme.Dark;
                                break;
                            case Theme.Dark:
                                CurrentTheme = Theme.Light;
                                break;
                        }
                    };

                    ((Grid)page.Content).Children.Add(moonButton);
                    */
                    if (CanGoBack && page.GetType() != typeof(DerivingPage) && page.GetType() != typeof(Finish)) // No button needed if there's nothing to go back to
                        page.Loaded += (a, _4) => // Content won't exist until the page has loaded, so set up the code to run when it has
                        {
                            Button backButton = new Button()
                            {
                                Width = 60,
                                Height = 60,
                                HorizontalAlignment = HorizontalAlignment.Left,
                                VerticalAlignment = VerticalAlignment.Top,
                                Background = Brushes.Transparent,
                                BorderBrush = Brushes.Transparent,
                            };

                            CurrentThemeChanged += (_5, _6) =>
                            {
                                switch (CurrentTheme)
                                {
                                    case Theme.Light:
                                        backButton.Content = arrowBlack;
                                        break;
                                    case Theme.Dark:
                                        backButton.Content = arrowWhite;
                                        break;
                                }
                            };

                            CurrentTheme = CurrentTheme; // invoke CurrentThemeChanged to set the initial image

                            backButton.Click += (_5, _6) =>
                            {
                                ((PageExtension)page).OnBack();
                                GoBack();
                            };

                            // All page roots have a Grid, so this is an easy way to add elements
                            ((Grid) page.Content).Children.Add(backButton);
                        };
                };
            };
        }

        public enum Theme
        {
            Light, Dark
        }
    }
}
