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

        public async Task<SwitchFs> LoadFileSystemAsync(Func<SwitchFs> fs)
        {
            return await RootWindow.Current.Submit(new Task<SwitchFs>(() => LoadFileSystem(fs)));
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
