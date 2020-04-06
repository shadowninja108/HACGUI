using HACGUI.Main.TitleManager;
using HACGUI.Utilities;
using LibHac.Fs;
using LibHac.FsSystem.Save;
using System;
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

        public string Owner
        {
            get
            {
                long titleId = SystemSaveNames.GetOwner(Save);
                if (titleId == -1)
                    return "Unknown";
                return SystemTitleNames.GetName((ulong)titleId);
            }
        }

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
        public Guid UserId => Save.Header.ExtraData.UserId;
        public string UserString
        {
            get
            {
                string uid = UserId.ToString();
                if (Preferences.Current.UserIds.ContainsKey(uid))
                {
                    string name = Preferences.Current.UserIds[uid];
                    return string.IsNullOrEmpty(name) ? "None" : name;
                }
                else if (UserId.Equals(Guid.Empty))
                    return "None";
                else
                    return uid;
            }
        }

        public long Size => Save.Header.ExtraData.DataSize;

        public SaveDataType Type => Save.Header.ExtraData.Type;

        public SaveElement(KeyValuePair<string, SaveDataFileSystem> kv)
        {
            Key = kv.Key;
            Save = kv.Value;
        }
    }
}
