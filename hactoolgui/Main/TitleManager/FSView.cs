using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using LibHac;
using LibHac.FsSystem.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HACGUI.Main.TitleManager
{
    public class FSView
    {
        //public SwitchFs FS;
        public delegate void FsReadyEvent(TitleSource source);
        public FsReadyEvent Ready;
        public Dictionary<TitleSource, List<SwitchFs>> IndexedFilesystems;

        public List<SwitchFs> Filesystems => IndexedFilesystems.Values.SelectMany(l => l).ToList();
        public Dictionary<string, SwitchFsNca> Ncas => Filesystems.SelectMany(f => f.Ncas).ToDictionary(k => k.Key, v => v.Value);
        public Dictionary<string, SaveDataFileSystem> Saves
        {
            get {
                Dictionary<string, SaveDataFileSystem> saves = new Dictionary<string, SaveDataFileSystem>();
                foreach (KeyValuePair<string, SaveDataFileSystem> kv in Filesystems.SelectMany(f => f.Saves))
                    saves[kv.Key] = kv.Value;
                return saves;
            }
        }

        public Dictionary<ulong, Title> Titles
        {
            get
            {
                Dictionary<ulong, Title> titles = new Dictionary<ulong, Title>();
                foreach(KeyValuePair<ulong, Title> kv in Filesystems.SelectMany(f => f.Titles))
                {
                    ulong titleId = kv.Key;
                    Title currentTitle = kv.Value;
                    if (titles.ContainsKey(titleId))
                    {
                        Title existingTitle = titles[titleId];
                        Title decidedTitle = existingTitle;
                        if(existingTitle.Version.Major < currentTitle.Version.Major)
                            decidedTitle = currentTitle;
                        else if (existingTitle.Version.Minor < currentTitle.Version.Minor)
                            decidedTitle = currentTitle;
                        else if (existingTitle.Version.Patch < currentTitle.Version.Patch)
                            decidedTitle = currentTitle;
                        titles.Remove(titleId); // remove old title
                        titles.Add(titleId, decidedTitle);
                    }
                    else
                        titles[titleId] = currentTitle;
                }
                return titles;
            }
        } 
        public Dictionary<ulong, Application> Applications
        {
            get
            {
                Dictionary<ulong, Application> apps = new Dictionary<ulong, Application>();
                foreach (SwitchFs fs in Filesystems)
                {
                    foreach (KeyValuePair<ulong, Application> kv in fs.Applications) {
                        ulong titleid = kv.Key;
                        Application app = kv.Value;
                        if (apps.ContainsKey(titleid))
                        {
                            if (app.Main != null)
                                apps[titleid].AddTitle(app.Main);
                            if (app.Patch != null)
                                apps[titleid].AddTitle(app.Patch);
                            foreach (Title title in new List<Title>(apps[titleid].AddOnContent))
                                if (title != null)
                                    apps[titleid].AddTitle(title);
                        }
                        else
                            apps[titleid] = app;
                    }
                }
                return apps;
            }
        }

        public FSView()
        {
            IndexedFilesystems = new Dictionary<TitleSource, List<SwitchFs>>
            {
                [TitleSource.SD] = new List<SwitchFs>(),
                [TitleSource.NAND] = new List<SwitchFs>(),
                [TitleSource.Imported] = new List<SwitchFs>()
            };
        }

        public Task LoadFileSystemAsync(string title, Func<SwitchFs> fs, TitleSource source, bool blocking)
        {
            Task task = new Task(() => LoadFileSystem(fs, source));
            ProgressTask ptask = new RunTask(title, task)
            {
                Blocking = blocking
            };
            TaskManagerPage.Current.Queue.Submit(ptask);
            return task;
        }

        public void LoadFileSystem(Func<SwitchFs> fsf, TitleSource source)
        {
            SwitchFs fs = fsf();
            Ready?.Invoke(source);
            IndexedFilesystems[source].Add(fs);
        }

        public enum TitleSource
        {
            SD, NAND, Imported
        }
    }
}
