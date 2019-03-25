using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibHac.IO.Save;

namespace HACGUI.Main.SaveManager
{
    public class SystemSaveNames
    {
        public static string GetName(SaveDataFileSystem save)
        {
            ulong id = save.Header.ExtraData.SaveId;
            id -= 0x8000000000000000;
            switch (id)
            {
                case 0x0:
                    return "saveDataIxrDb";
                case 0x10:
                    return "account";
                case 0x11:
                    return "idgen";
                case 0x20:
                    return "data";
                case 0x30:
                case 0x31:
                    return "mii";
                case 0x40:
                    return "apprecdb";
                case 0x41:
                    return "nsaccache";
                case 0x43:
                    return "ns_appman";
                case 0x44:
                    return "ns_sysup";
                case 0x45:
                    return "vmdb";
                case 0x46:
                    return "dtlman";
                case 0x47:
                    return "nx_exfat";
                case 0x48:
                    return "ns_systemseed";
                case 0x49:
                    return "ns_ssversion";
                case 0x50:
                    return "SystemSettings";
                case 0x51:
                    return "FwdbgSettingsS";
                case 0x52:
                    return "PrivateSettings";
                case 0x53:
                    return "DeviceSettings";
                case 0x54:
                    return "ApplnSettings";
                case 0x60:
                    return "SslSave";
                case 0x70:
                    return "nim_sys";
                case 0x71:
                    return "nim_net";
                case 0x72:
                    return "nim_tmp";
                case 0x73:
                    return "nim_dac";
                case 0x74:
                    return "nim_delta";
                case 0x75:
                    return "nim_vac";
                case 0x76:
                    return "nim_local";
                case 0x77:
                    return "nim_lsys";
                case 0x78:
                    return "nim_eca_dbg";
                case 0x80:
                    return "friends";
                case 0x81:
                    return "friends-sys";
                case 0x82:
                    return "friends-image";
                case 0x90:
                    return "news";
                case 0x91:
                    return "news-sys";
                case 0x92:
                    return "news-dl";
                case 0xA0:
                    return "prepo-sys";
                case 0xA1:
                    return "prepo";
                case 0xA2:
                    return "prepo-ap";
                case 0xB0:
                    return "nsdsave";
                case 0xC1:
                    return "bcat-sys";
                case 0xC2:
                    return "bcat-dl";
                case 0xD1:
                    return "save";
                case 0xE0:
                    return "escertificate";
                case 0xE1:
                    return "escommon";
                case 0xE2:
                    return "espersonalized";
                case 0xE3:
                    return "esmetarecord";
                case 0xE4:
                    return "eselicense";
                case 0xF0:
                    return "pdm";
                case 0x100:
                    return "pctlss";
                case 0x110:
                    return "npns_save";
                case 0x130:
                    return "state";
                case 0x131:
                    return "context";
                case 0x140:
                    return "TM";
                default:
                    return null;
            }

        }

        public static string GetOwner(SaveDataFileSystem save)
        {
            ulong id = save.Header.ExtraData.SaveId;
            id -= 0x8000000000000000;
            if (id == 0)
                return "fs";
            if (id < 0x20)
                return "account";
            if (id == 0x20)
                return "nfc";
            if (id < 0x50 || id == 0xF0)
                return "ns";
            if (id < 0x60)
                return "settings";
            if (id == 0x60)
                return "ssl";
            if (id < 0x80)
                return "nim";
            if (id < 0x90)
                return "friends";
            if (id == 0xB0)
                return "bsdsockets";
            if (id < 0xD1)
                return "bcat";
            if (id == 0xD1)
                return "erpt";
            if (id < 0x100)
                return "es";
            if (id == 0x100)
                return "pctl";
            if (id == 0x110)
                return "npns";
            if(id == 0x122)
                return "Unknown";
            if (id < 0x140)
                return "migration";
            switch (id)
            {
                case 0x140:
                    return "capsrv";
                case 0x1010:
                    return "qlaunch";
                case 0x1020:
                    return "swkbd";
            }
            if (id < 0x1040)
                return "Unknown";
            if (id < 0x1060)
                return "miiEdit";
            if (id < 0x1070)
                return "shop";
            if (id < 0x1090)
                return "web";
            switch (id)
            {
                case 0x1091:
                    return "loginShare";
                case 0x10B0:
                    return "playerSelect";
                case 0x10C0:
                    return "myPage";
            }

            return "Unknown";
        }
    }
}
