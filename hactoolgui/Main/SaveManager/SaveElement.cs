using LibHac.IO.Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.SaveManager
{
    public class SaveElement
    {
        public string Key;
        public SaveDataFileSystem Save;

        public ulong SaveId => Save.Header.ExtraData.SaveId;
        public ulong TitleId => Save.Header.ExtraData.TitleId;
        public string UserId => Save.Header.ExtraData.UserId.ToString();
        public long Size => Save.Header.ExtraData.DataSize;

        public SaveElement(KeyValuePair<string, SaveDataFileSystem> kv)
        {
            Key = kv.Key;
            Save = kv.Value;
        }
    }
}
