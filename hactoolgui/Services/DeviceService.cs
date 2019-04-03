using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HACGUI.Extensions;
using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Main.TitleManager;
using HACGUI.Utilities;
using LibHac;
using LibHac.IO;
using LibHac.IO.Save;
using static HACGUI.Main.TitleManager.FSView;

namespace HACGUI.Services
{
    public class DeviceService
    {
        public delegate void TitlesChangedEvent(Dictionary<ulong, Application> apps, Dictionary<ulong, Title> titles, Dictionary<string, SaveDataFileSystem> saves);
        public static event TitlesChangedEvent TitlesChanged;

        public static Dictionary<ulong, Application> Applications => FsView.Applications;
        public static Dictionary<ulong, Title> Titles => FsView.Titles;
        public static Dictionary<string, SaveDataFileSystem> Saves => FsView.Saves;

        public static List<ApplicationElement> ApplicationElements => IndexTitles(Titles.Values);

        public static FSView FsView;

        private static bool Started;

        private static readonly object Lock = new object();

        public static void Start()
        {
            if (!Started)
            {
                Started = true;

                FsView = new FSView();

                int count = 0;
                FsView.Ready += (source) =>
                {
                    if (source == TitleSource.SD)
                        StatusService.SDStatus = StatusService.Status.OK;
                    else if (source == TitleSource.NAND)
                    {
                        count++;
                        if(count == 2)
                        {
                            StatusService.NANDStatus = StatusService.Status.OK;
                            Update();
                            count = 0;
                        }
                    }
                    Update();
                };

                SDService.OnSDPluggedIn += (drive) =>
                {
                    FsView.LoadFileSystemAsync("Opening SD filesystem...", () => SwitchFs.OpenSdCard(HACGUIKeyset.Keyset, new LocalFileSystem(drive.RootDirectory.FullName)), TitleSource.SD, true);
                    Update();

                    StatusService.SDStatus = StatusService.Status.Progress;
                };

                SDService.OnSDRemoved += (drive) =>
                {
                    StatusService.SDStatus = StatusService.Status.Incorrect;
                    FsView.IndexedFilesystems[TitleSource.SD].Clear();
                    Update();
                };

                NANDService.OnNANDPluggedIn += () =>
                {
                    FsView.LoadFileSystemAsync("Opening NAND user filesystem...", () => SwitchFs.OpenNandPartition(HACGUIKeyset.Keyset, NANDService.NAND.OpenUserPartition()), TitleSource.NAND, false);
                    FsView.LoadFileSystemAsync("Opening NAND system filesystem...", () => SwitchFs.OpenNandPartition(HACGUIKeyset.Keyset, NANDService.NAND.OpenSystemPartition()), TitleSource.NAND, true);
                    TaskManagerPage.Current.Queue.Submit(new DecryptTicketsTask());
                    TaskManagerPage.Current.Queue.Submit(new SaveKeysetTask(Preferences.Current.DefaultConsoleName)); // TODO

                    StatusService.NANDStatus = StatusService.Status.Progress;
                };

                NANDService.OnNANDRemoved += () =>
                {
                    StatusService.NANDStatus = StatusService.Status.Incorrect;

                    FsView.IndexedFilesystems[TitleSource.NAND].Clear();

                    Update();
                };

                SDService.Start();
                NANDService.Start();

                Update();
            }
        }

        public static void Stop()
        {
            SDService.Stop();
            NANDService.Stop();

            Started = false;
        }

        public static void Update()
        {
            lock(Lock)
            { 
                TaskManagerPage.Current.Queue.Submit(new RunTask("Updating application view...", new Task(() =>
                { 
                    TitlesChanged?.Invoke(Applications, Titles, Saves);
                })));
            }
        }


        public static List<ApplicationElement> IndexTitles(IEnumerable<Title> titles)
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
    }

}
