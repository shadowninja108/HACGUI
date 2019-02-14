using HACGUI.Main.TaskManager;
using HACGUI.Main.TaskManager.Tasks;
using LibHac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TitleManager
{
    public class FSView
    {
        public SwitchFs FS;
        public EventHandler Ready;
        public TitleSource Source;

        public FSView(TitleSource source)
        {
            Source = source;
        }

        public Task<SwitchFs> LoadFileSystemAsync(string title, Func<SwitchFs> fs)
        {
            Task<SwitchFs> task = new Task<SwitchFs>(() => LoadFileSystem(fs));
            TaskManagerPage.Current.Queue.Submit(new RunTaskTask(title, task));
            return task;
        }

        public SwitchFs LoadFileSystem(Func<SwitchFs> fs)
        {
            FS = fs();
            Ready(this, null);
            
            return FS;
        }

        public enum TitleSource
        {
            SD, NAND
        }
    }
}
