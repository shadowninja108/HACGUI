using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static HACGUI.Main.TitleManager.FSView;
using Application = LibHac.Application;
using Image = System.Windows.Controls.Image;

namespace HACGUI.Main.TitleManager
{
    /// <summary>
    /// Interaction logic for MainTitleManagerPage.xaml
    /// </summary>
    public partial class MainTitleManagerPage : PageExtension
    {
        static FSView SDTitleView, NANDSystemTitleView, NANDUserTitleView;

        public MainTitleManagerPage()
        {
            InitializeComponent();

            NANDSystemTitleView = new FSView(TitleSource.NAND);
            NANDUserTitleView = new FSView(TitleSource.NAND);
            SDTitleView = new FSView(TitleSource.SD);

            SDService.OnSDPluggedIn += (drive) =>
            {
                SDTitleView.Ready += (_, __) =>
                {
                    Dispatcher.BeginInvoke(new Action(() => 
                    {
                        StatusService.SDStatus = StatusService.Status.OK;

                        RefreshListView();
                    }));
                };
                RootWindow.Current.Submit(new Task(() => SDTitleView.LoadFileSystem(() => SwitchFs.OpenSdCard(HACGUIKeyset.Keyset, new LocalFileSystem(drive.RootDirectory.FullName)))));


                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusService.SDStatus = StatusService.Status.Progress;
                }));
            };

            SDService.OnSDRemoved += (drive) =>
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusService.SDStatus = StatusService.Status.Incorrect;

                    SDTitleView.FS = null;

                    RefreshListView();
                }));
            };

            NANDService.OnNANDPluggedIn += () =>
            {
                void onComplete()
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        StatusService.NANDStatus = StatusService.Status.OK;

                        RefreshListView();
                    }));
                };

                int count = 0;
                NANDSystemTitleView.Ready += (_, __) =>
                {
                    count++;
                    if (count >= 2)
                        onComplete();
                };
                NANDUserTitleView.Ready += (_, __) =>
                {
                    count++;
                    if (count >= 2)
                        onComplete();
                };

                RootWindow.Current.Submit(new Task(() => 
                {
                    NANDSystemTitleView.LoadFileSystem(() => SwitchFs.OpenNandPartition(HACGUIKeyset.Keyset, NANDService.NAND.OpenSystemPartition()));
                    NANDUserTitleView.LoadFileSystem(() => SwitchFs.OpenNandPartition(HACGUIKeyset.Keyset, NANDService.NAND.OpenUserPartition()));
                }));

                //Task.Run(() => NANDSystemTitleView.LoadFileSystem(() => new SwitchFs(HACGUIKeyset.Keyset, NANDService.NAND.OpenSystemPartition())));
                //Task.Run(() => NANDUserTitleView.LoadFileSystem(() => new SwitchFs(HACGUIKeyset.Keyset, NANDService.NAND.OpenUserPartition())));

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusService.NANDStatus = StatusService.Status.Progress;
                }));
            };

            NANDService.OnNANDRemoved += () =>
            {

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StatusService.NANDStatus = StatusService.Status.Incorrect;

                    NANDSystemTitleView.FS = null;
                    NANDUserTitleView.FS = null;

                    RefreshListView();
                }));

            };

            SDService.Start();
            NANDService.Start();
        }

        public void RefreshListView()
        {
            ListView.Items.Clear();

            Tuple<Dictionary<ulong, LibHac.Application>, Dictionary<ulong, Title>> info = GetAppsAndTitles();

            List<ApplicationElement> elements = IndexTitles(info.Item2.Values.ToList());

            foreach (ApplicationElement element in elements)
            {
                element.Load();
                ListView.Items.Add(element);
            }
        }

        public Tuple<Dictionary<ulong, LibHac.Application>, Dictionary<ulong, Title>> GetAppsAndTitles()
        {
            Dictionary<ulong, LibHac.Application> totalApps = new Dictionary<ulong, LibHac.Application>();
            Dictionary<ulong, Title> totalTitles = new Dictionary<ulong, Title>();

            if (SDTitleView.FS != null)
            {
                lock (totalApps)
                {
                    foreach (KeyValuePair<ulong, LibHac.Application> kv in SDTitleView.FS.Applications)
                    {
                        ulong titleid = kv.Key;
                        LibHac.Application app = kv.Value;
                        if (totalApps.ContainsKey(titleid))
                        {
                            totalApps[titleid].AddTitle(app.Main);
                            totalApps[titleid].AddTitle(app.Patch);
                            foreach (Title title in totalApps[titleid].AddOnContent)
                                totalApps[titleid].AddTitle(title);
                        }
                        else
                            totalApps[titleid] = app;
                    }
                    totalTitles.AddRange(SDTitleView.FS.Titles);
                }
            }
            if (NANDSystemTitleView.FS != null)
            {
                totalApps.AddRange(NANDSystemTitleView.FS.Applications);
                totalTitles.AddRange(NANDSystemTitleView.FS.Titles);
            }
            if (NANDUserTitleView.FS != null)
            {
                lock (totalApps) // ensure threads don't try to modify list while iterating through it
                {
                    foreach (KeyValuePair<ulong, LibHac.Application> kv in NANDUserTitleView.FS.Applications)
                    {
                        ulong titleid = kv.Key;
                        LibHac.Application app = kv.Value;
                        if (totalApps.ContainsKey(titleid))
                        {
                            if (app.Main != null)
                                totalApps[titleid].AddTitle(app.Main);
                            if (app.Patch != null)
                                totalApps[titleid].AddTitle(app.Patch);
                            foreach (Title title in totalApps[titleid].AddOnContent)
                                if (title != null)
                                    totalApps[titleid].AddTitle(title);
                        }
                        else
                            totalApps[titleid] = app;
                    }
                    totalTitles.AddRange(NANDUserTitleView.FS.Titles);
                }
            }

            return new Tuple<Dictionary<ulong, LibHac.Application>, Dictionary<ulong, Title>>(totalApps, totalTitles);
        }

        /*public void StatusUpdated(object sender, NotifyCollectionChangedEventArgs e)
        {
            StatusBar.Items.Clear();
            foreach (string str in Status)
            {
                StatusBarItem item = new StatusBarItem();
                TextBlock text = new TextBlock();
                text.Text = str;
                item.Content = text;
                StatusBar.Items.Add(item);
            }
        }*/

        public static List<ApplicationElement> IndexTitles(List<Title> titles)
        {
            List<ApplicationElement> elements = new List<ApplicationElement>();

            foreach (Title title in titles)
            {
                if (title != null)
                {
                    ApplicationElement searchedApp = elements.FirstOrDefault(x => x?.BaseTitleId == title.GetBaseTitleID());
                    if (searchedApp != null)
                        searchedApp.Titles.Add(title);
                    else
                    {
                        ApplicationElement app = new ApplicationElement();
                        app.Titles.Add(title);
                        elements.Add(app);
                    }
                }
            }

            return elements;
        }


        private void ApplicationDoubleClicked(object sender, MouseButtonEventArgs e)
        {
            ApplicationElement element = ((ApplicationElement)((ListView)sender).SelectedItem);

            if (element != null)
            {
                Application.ApplicationWindow window = new Application.ApplicationWindow(element, GetAppsAndTitles().Item1);
                window.ShowDialog();
            }
        }

    }
}
