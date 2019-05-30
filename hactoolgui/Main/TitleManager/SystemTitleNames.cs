using LibHac;

namespace HACGUI.Main.TitleManager
{
    public class SystemTitleNames
    {
        public static string GetNameFromTitle(Title title)
        {
            ulong id = title.Id;
            if (title.MainNca == null)
                return "Unknown";
            if (id == 0x0100000000000031)
            {
                if (title.MainNca.Nca.Header.KeyAreaKeyIndex == 0)
                    return "arp"; // 1.0.0
                else
                    return "glue"; // 2.0.0 +
            }

            return GetName(id);
        }

        public static string GetName(ulong titleId)
        {
            titleId -= 0x0100000000000000;
            switch (titleId)
            {
                // System Modules
                case 0x0:
                    return "fs";
                case 0x1:
                    return "ldr";
                case 0x2:
                    return "ncm";
                case 0x3:
                    return "pm";
                case 0x4:
                    return "sm";
                case 0x5:
                    return "boot";
                case 0x6:
                    return "usb";
                case 0x7:
                    return "tma";
                case 0x8:
                    return "boot2"; // idk how to detect if debug, retail or factory
                case 0x9:
                    return "settings";
                case 0xA:
                    return "bus";
                case 0xB:
                    return "bluetooth";
                case 0xC:
                    return "bcat";
                case 0xD:
                    return "dmnt";
                case 0xE:
                    return "friends";
                case 0xF:
                    return "nifm";
                case 0x10:
                    return "ptm";
                case 0x11:
                    return "shell";
                case 0x12:
                    return "bsdsocket";
                case 0x13:
                    return "hid";
                case 0x14:
                    return "audio";
                case 0x15:
                    return "LogManager";
                case 0x16:
                    return "wlan";
                case 0x17:
                    return "cs";
                case 0x18:
                    return "ldn";
                case 0x19:
                    return "nvservices";
                case 0x1A:
                    return "pcv";
                case 0x1B:
                    return "ppc";
                case 0x1C:
                    return "nvnflinger";
                case 0x1D:
                    return "pcie";
                case 0x1E:
                    return "account";
                case 0x1F:
                    return "ns";
                case 0x20:
                    return "nfc";
                case 0x21:
                    return "psc";
                case 0x22:
                    return "capsrv";
                case 0x23:
                    return "am";
                case 0x24:
                    return "ssl";
                case 0x25:
                    return "nim";
                case 0x26:
                    return "cec";
                case 0x27:
                    return "tspm";
                //spl is bundled with kernel
                case 0x29:
                    return "lbl";
                case 0x2A:
                    return "btm";
                case 0x2B:
                    return "erpt";
                case 0x2C:
                    return "time";
                case 0x2D:
                    return "vi";
                case 0x2E:
                    return "pctl";
                case 0x2F:
                    return "npns";
                case 0x30:
                    return "eupld";
                
                case 0x32:
                    return "eclct";
                case 0x33:
                    return "es";
                case 0x34:
                    return "fatal";
                case 0x35:
                    return "grc";
                case 0x36:
                    return "creport";
                case 0x37:
                    return "ro";
                case 0x38:
                    return "profiler";
                case 0x39:
                    return "sdb";
                case 0x3A:
                    return "migration";
                case 0x3B:
                    return "jit";
                case 0x3C:
                    return "jpegdec";
                case 0x3D:
                    return "safemode";
                case 0x3E:
                    return "olsc";
                case 0x3F:
                    return "dt";
                case 0x40:
                    return "nd";

                // System Data Archives
                case 0x800:
                    return "CertStore";
                case 0x801:
                    return "ErrorMessage";
                case 0x802:
                    return "MiiModel";
                case 0x803:
                    return "BrowserDll";
                case 0x804:
                    return "Help";
                case 0x805:
                    return "SharedFont";
                case 0x806:
                    return "NgWord";
                case 0x807:
                    return "SsidList";
                case 0x808:
                    return "Dictionary";
                case 0x809:
                    return "SystemVersion";
                case 0x80A:
                    return "AvatarImage";
                case 0x80B:
                    return "LocalNews";
                case 0x80C:
                    return "Eula";
                case 0x80D:
                    return "UrlBlackList";
                case 0x80E:
                    return "TimeZoneBinary";
                case 0x80F:
                    return "CertStoreCruiser";
                case 0x810:
                    return "FontNintendoExtension";
                case 0x811:
                    return "FontStandard";
                case 0x812:
                    return "FontKorean";
                case 0x813:
                    return "FontChineseTraditional";
                case 0x814:
                    return "FontChineseSimple";
                case 0x815:
                    return "FontBfcpx";
                case 0x816:
                    return "SystemUpdate";
                case 0x818:
                    return "FirmwareDebugSettings";
                case 0x819:
                    return "BootImagePackage";
                case 0x81A:
                    return "BootImagePackageSafe";
                case 0x81B:
                    return "BootImagePackageExFat";
                case 0x81C:
                    return "BootImagePackageExFatSafe";
                case 0x81D:
                    return "FatalMessage";
                case 0x81E:
                    return "ControllerIcon";
                case 0x81F:
                    return "PlatformConfigIcosa";
                case 0x820:
                    return "PlatformConfigCopper";
                case 0x821:
                    return "PlatformConfigHoag";
                case 0x822:
                    return "ControllerFirmware";
                case 0x823:
                    return "NgWord2";
                case 0x824:
                    return "PlatformConfigIcosaMariko";
                case 0x825:
                    return "ApplicationBlackList";
                case 0x826:
                    return "RebootlessSystemUpdateVersion";
                case 0x827:
                    return "ContentActionTable";
                case 0x828:
                    return "FunctionBlackList";

                // System Applets
                case 0x1000:
                    return "qlaunch";
                case 0x1001:
                    return "auth";
                case 0x1002:
                    return "cabinet";
                case 0x1003:
                    return "controller";
                case 0x1004:
                    return "dataErase";
                case 0x1005:
                    return "error";
                case 0x1006:
                    return "netConnect";
                case 0x1007:
                    return "playerSelect";
                case 0x1008:
                    return "swkbd";
                case 0x1009:
                    return "miiEdit";
                case 0x100A:
                    return "web";
                case 0x100B:
                    return "shop";
                case 0x100C:
                    return "overlayDisp";
                case 0x100D:
                    return "photoViewer";
                case 0x100E:
                    return "set";
                case 0x100F:
                    return "offlineWeb";
                case 0x1010:
                    return "loginShare";
                case 0x1011:
                    return "wifiWebAuth";
                case 0x1012:
                    return "starter";
                case 0x1013:
                    return "myPage";
                case 0x1014:
                    return "PlayReport";
                case 0x1015:
                    return "MaintenanceMenu";
                case 0x101B:
                    return "DummyECApplet";
                case 0x1020:
                    return "story";
                case 0x1FFF:
                    return "EndOceanProgramId";

                // System Applications
                case 0x8BB00013C000:
                    return "flog";

                default:
                    return "Unknown";

                    //TODO: type out rest of this shit
            }
        }
    }
}
