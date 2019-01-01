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

        private FileIniDataParser parser;
        public IniData data;

        public string DefaultConsoleName {
            get
            {
                return data["General"]["DefaultConsole"];
            }
            set
            {
                data["General"]["DefaultConsole"] = value;
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

    }
}
