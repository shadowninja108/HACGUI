using HACGUI.Extensions;
using HACGUI.FirstStart;
using HACGUI.Main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

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

        private string[] LightThemeDictPaths = new string[] {   };
        private string[] DarkThemeDictPaths = new string[] {  };

        private List<ResourceDictionary> LightThemeDicts, DarkThemeDicts;

        public static RootWindow Current;

        public RootWindow()
        {
            InitializeComponent();

            Current = this;

            void HandleException(object sender, UnhandledExceptionEventArgs args)
            {
                new ExceptionWindow((Exception) args.ExceptionObject).Show();
                Close(); // make sure everything has ended
            }

            AppDomain.CurrentDomain.UnhandledException += HandleException;

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
                Tuple<bool, string> result = HACGUIKeyset.IsValidInstall();
                Page nextPage;
                if (result.Item1)
                {
                    nextPage = new MainPage();
                }
                else
                {
                    string[] args = App.Args.Args;
                    nextPage = new IntroPage();
                    if (args.Length == 1)
                    {
                        if (args[0] == "continue")
                        {
                            FileInfo continueFile = HACGUIKeyset.TempContinueFileInfo;
                            if (continueFile.Exists) {
                                StreamReader reader = new StreamReader(continueFile.OpenRead());
                                Array.Copy(reader.ReadLine().ToByteArray(), HACGUIKeyset.Keyset.SecureBootKey, 0x10);
                                Array.Copy(reader.ReadLine().ToByteArray(), HACGUIKeyset.Keyset.TsecKey, 0x10);
                                byte[][] tsecRootKey =  (byte[][]) reader.ReadLine().ToByteArray().Deserialize();
                                for (int i = 0; i < tsecRootKey.Length; i++)
                                    Array.Copy(tsecRootKey[i], HACGUIKeyset.Keyset.TsecRootKeys[i], 0x10);
                                byte[][] keyblobs = (byte[][])reader.ReadLine().ToByteArray().Deserialize();
                                for (int i = 0; i < tsecRootKey.Length; i++)
                                    Array.Copy(keyblobs[i], HACGUIKeyset.Keyset.EncryptedKeyblobs[i], 0xB0);
                                PickConsolePage.ConsoleName = reader.ReadLine();
                                reader.Close();

                                HACGUIKeyset.Keyset.DeriveKeys();

                                nextPage = new PickNANDPage();
                            }
                        }
                    }
                }
                Navigate(nextPage);

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
                    if (CanGoBack && page.GetType() != typeof(DerivingPage) && page.GetType() != typeof(FinishPage)) // No button needed if there's nothing to go back to
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

        public Task<T> Submit<T>(Task<T> task)
        {
            if (!Debugger.IsAttached)
            {
                task.ContinueWith((t) => 
                {
                    Dispatcher.Invoke(() => HandleException(t.Exception));
                    return t.Result;
                }, TaskContinuationOptions.OnlyOnFaulted);
            }
            task.Start();

            return task;
        }

        public Task Submit(Task task)
        {
            if (!Debugger.IsAttached)
            {
                task.ContinueWith((t) =>
                {
                    Dispatcher.Invoke(() => HandleException(t.Exception));
                }, TaskContinuationOptions.OnlyOnFaulted);
            }

            task.Start();

            return task;
        }

        public void HandleException(Exception e)
        {
            new ExceptionWindow(e).Show();
            Close(); // make sure everything has ended
        }

        public void HandleDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
        {
            args.Handled = true; // program won't crash
            HandleException(args.Exception);
        }

        public enum Theme
        {
            Light, Dark
        }
    }
}
