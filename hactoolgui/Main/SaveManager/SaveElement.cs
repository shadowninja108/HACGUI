using LibHac.IO.Save;
using System.Collections.Generic;

namespace HACGUI.Main.SaveManager
{
    public class SaveElement
    {
        public string Key;
        public SaveDataFileSystem Save;

        public ulong SaveId
        {
            get
            {
                ExtraData extra = Save.Header.ExtraData;
                return (extra.SaveId == 0) ? extra.TitleId : extra.SaveId;
            }
        }

        public string Owner => SystemSaveNames.GetOwner(Save);

        public string DisplayName
        {
            get
            {
                string name = SystemSaveNames.GetName(Save);
                if (name != null)
                    return $"{name}:/";
                else
                    return string.Format("{0:x16}", SaveId);
            }
        }
        public string UserId => Save.Header.ExtraData.UserId.ToString();
        public long Size => Save.Header.ExtraData.DataSize;

        public SaveElement(KeyValuePair<string, SaveDataFileSystem> kv)
        {
            Key = kv.Key;
            Save = kv.Value;
        }
    }
}
