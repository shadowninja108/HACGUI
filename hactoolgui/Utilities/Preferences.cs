using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Utilities
{
    public class Preferences
    {
        public static Preferences Current = new Preferences();

        private readonly FileIniDataParser parser;
        public IniData data;

        public KeyDataCollectionAccessor General => new KeyDataCollectionAccessor(data["General"]);
        public KeyDataCollectionAccessor SdIdentifiers => new KeyDataCollectionAccessor(data["SdIdentifiers"]);
        public KeyDataCollectionAccessor UserIds => new KeyDataCollectionAccessor(data["UserIds"]);

        public string DefaultConsoleName {
            get
            {
                return General["DefaultConsole"];
            }
            set
            {
                General["DefaultConsole"] = value;
            }
        }

        public Preferences()
        {
            parser = new FileIniDataParser();
            FileInfo info = HACGUIKeyset.PreferencesFileInfo;
            if(!info.Exists)
                info.Create().Close(); // ensure it's created
            data = parser.ReadFile(info.FullName);
        }

        public void Write()
        {
            parser.WriteFile(HACGUIKeyset.PreferencesFileInfo.FullName, data);
        }

        public class KeyDataCollectionAccessor
        {
            private readonly KeyDataCollection Internal;

            public KeyDataCollectionAccessor(KeyDataCollection i)
            {
                Internal = i;
            }

            public string this[string i]
            {
                get
                {
                    return Internal[i];
                }
                set
                {
                    Internal[i] = value;
                }
            }

            public bool ContainsKey(string i)
            {
                return Internal.ContainsKey(i);
            }
        }

    }
}
