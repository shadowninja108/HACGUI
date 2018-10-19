using HACGUI.Extensions;
using LibHac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LibHac.ExternalKeys;

namespace HACGUI
{
    public class HACGUIKeyset : Keyset
    {
        public static HACGUIKeyset Keyset = new HACGUIKeyset();

        public const string
            RootFolderName = "HACGUI",
            RootKeyFolderName = "keys",
            RootConsoleFolderName = "console",
            RootTempFolderName = "temp",
            NandKeysFileName = "nand.keys",
            ProductionKeysFileName = "prod.keys",
            DeveloperKeysFileName = "dev.keys",
            TitleKeysFileName = "title.keys",
            ConsoleKeysFileName = "console.keys",
            ExtraKeysFileName = "extra.keys",
            PreferencesFileName = "preferences.ini",
            UserSwitchFolderName = ".switch",
            TempBOOT0FileName = "BOOT0",
            TempPkg1FileName = "pkg1.bin",
            TempPkg1FolderName = "pkg1",
            TempNXBootloaderFileName = "NX_Bootloader.bin",
            TempSecureMonitorFileName = "Secure_Monitor.bin",
            TempWarmbootFileName = "Warmboot.bin",
            TempPkg2FileName = "pkg2.bin",
            TempPkg2FolderName = "pkg2",
            TempKernelFileName = "Kernel.bin",
            TempINI1FileName = "INI1.bin",
            TempINI1FolderName = "INI1",
            TempPRODINFOFileName = "PRODINFO.bin",
            ClientCertificateFileName = "nx_tls_client_cert.pfx";

        public static DirectoryInfo UserSwitchDirectoryInfo => new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).GetDirectory(UserSwitchFolderName);
        public static DirectoryInfo WorkingDirectoryInfo => new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        public static DirectoryInfo RootFolderInfo => UserSwitchDirectoryInfo.GetDirectory(RootFolderName);
        public static DirectoryInfo RootKeyFolderInfo => RootFolderInfo.GetDirectory(RootKeyFolderName);
        public static DirectoryInfo RootConsoleFolderInfo => RootKeyFolderInfo.GetDirectory(RootConsoleFolderName);
        public static DirectoryInfo RootTempFolderInfo => WorkingDirectoryInfo.GetDirectory(RootTempFolderName);
        public static DirectoryInfo RootTempPkg1FolderInfo => RootTempFolderInfo.GetDirectory(TempPkg1FolderName);
        public static DirectoryInfo RootTempPkg2FolderInfo => RootTempFolderInfo.GetDirectory(TempPkg2FolderName);
        public static DirectoryInfo RootTempINI1Folder => RootTempPkg2FolderInfo.GetDirectory(TempINI1FolderName);


        public static FileInfo ProductionKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ProductionKeysFileName);
        public static FileInfo DeveloperKeysFileInfo => UserSwitchDirectoryInfo.GetFile(DeveloperKeysFileName);
        public static FileInfo TitleKeysFileInfo => UserSwitchDirectoryInfo.GetFile(TitleKeysFileName);
        public static FileInfo ConsoleKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ConsoleKeysFileName);
        public static FileInfo ExtraKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ExtraKeysFileName);
        public static FileInfo TempBOOT0FileInfo => RootTempFolderInfo.GetFile(TempBOOT0FileName);
        public static FileInfo TempPkg1FileInfo => RootTempFolderInfo.GetFile(TempPkg1FileName);
        public static FileInfo TempNXBootloaderFileInfo = RootTempPkg1FolderInfo.GetFile(TempNXBootloaderFileName);
        public static FileInfo TempSecureMonitorFileInfo = RootTempPkg1FolderInfo.GetFile(TempSecureMonitorFileName);
        public static FileInfo TempWarmbootFileInfo = RootTempPkg1FolderInfo.GetFile(TempWarmbootFileName);
        public static FileInfo TempPkg2FileInfo = RootTempFolderInfo.GetFile(TempPkg2FileName);
        public static FileInfo TempKernelFileInfo = RootTempPkg2FolderInfo.GetFile(TempKernelFileName);
        public static FileInfo TempINI1FileInfo = RootTempPkg2FolderInfo.GetFile(TempINI1FileName);
        public static FileInfo TempPRODINFOFileInfo = RootTempFolderInfo.GetFile(TempPRODINFOFileName);

        public static FileInfo PreferencesFileInfo => RootFolderInfo.GetFile(PreferencesFileName);

        public byte[] SslRsaKek { get; } = new byte[0x10];

        public HACGUIKeyset()
        {
            if(IsValidInstall().Item1)
                Refresh();

            // Init with static keys
            Array.Copy(NintendoKeys.DeviceKeySource, PerConsoleKeySource, 0x10);
            for(int i = 0; i < NintendoKeys.BisKeySources.Length; i++)
                Array.Copy(NintendoKeys.BisKeySources[i], BisKeySource[i], NintendoKeys.BisKeySources[i].Length);
            Array.Copy(NintendoKeys.BisKekSource, BisKekSource, 0x10);
            Array.Copy(NintendoKeys.PersonalizedAesKeySource, AesKeyGenerationSource, 0x10);
            Array.Copy(NintendoKeys.PersonalizedAesKekSource, AesKekGenerationSource, 0x10);
            Array.Copy(NintendoKeys.RetailSpecificAesKeySource, RetailSpecificAesKeySource, 0x10);
            for(int i = 0; i < NintendoKeys.KeyblobSources.Length; i++)
                Array.Copy(NintendoKeys.KeyblobSources[i], KeyblobKeySources[i], NintendoKeys.KeyblobSources[i].Length);
            
        }

        public void Refresh()
        {

        }

        internal static DirectoryInfo GetConsoleFolderInfo(string name)
        {
            return RootConsoleFolderInfo.GetDirectory(name);
        }

        public static FileInfo GetConsoleKeysFileInfoByName(string name)
        {
            return GetConsoleFolderInfo(name).GetFile(ConsoleKeysFileName);
        }

        public static FileInfo GetClientCertificateByName(string name)
        {
            return GetConsoleFolderInfo(name).GetFile(ClientCertificateFileName);
        }

        public static void SetConsole(string name)
        {

        }

        public Tuple<bool, string> IsValidInstall()
        {
            DirectoryInfo root = WorkingDirectoryInfo.GetDirectory(RootFolderName);
            if (root.Exists) // Check if HACGUI folder exists
            {
                DirectoryInfo rootKeysDir = RootKeyFolderInfo;
                if (rootKeysDir.Exists) // Check if root key folder exists
                {
                    DirectoryInfo rootConsoleDir = RootConsoleFolderInfo;
                    if (rootConsoleDir.Exists)
                    {
                        if (!rootConsoleDir.GetDirectories().Any()) // Check if the consoles directory is empty
                            return new Tuple<bool, string>(false, $"There are no consoles stored!");

                        foreach (DirectoryInfo consoleDir in rootConsoleDir.GetDirectories())
                        {
                            if (!consoleDir.ContainsFile(NandKeysFileName)) // Check that every console has a nand.keys associated with it
                                return new Tuple<bool, string>(false, $"Console {consoleDir.Name} does not have a {NandKeysFileName} file!");
                        }
                    }
                    else
                        return new Tuple<bool, string>(false, "Console directory does not exist!");

                    if (!root.ContainsFile(PreferencesFileName))
                        return new Tuple<bool, string>(false, "Preferences file does not exist!");

                    return new Tuple<bool, string>(true, "Install valid.");
                }
                else
                    return new Tuple<bool, string>(false, "Keys directory does not exist!");
            }
            return new Tuple<bool, string>(false, "Install folder does not exist!");
        }

        public static string[] HactoolNonFriendlyKeys = new string[]
        {
            "bis_kek_source",
            "bis_key_source_00",
            "bis_key_source_01",
            "bis_key_source_02",
            "eticket_rsa_kek",
            "ssl_rsa_kek",
            "retail_specific_aes_key_source",
            "save_mac_kek_source",
            "save_mac_key",
            "save_mac_key_source",
            "per_console_key_source"
        };

        public static string PrintCommonKeys(Keyset keyset, bool hactoolFriendly)
        {
            Dictionary<string, KeyValue> dict = new Dictionary<string, KeyValue>(CommonKeyDict);
            dict.Add("ssl_rsa_kek", new KeyValue("ssl_rsa_kek", 0x10, set => ((HACGUIKeyset)set).SslRsaKek));
            List<string> keysToBeRemoved = new List<string>();
            if (hactoolFriendly)
                foreach (KeyValuePair<string, KeyValue> kv in dict)
                    if (HactoolNonFriendlyKeys.Contains(kv.Key))
                        keysToBeRemoved.Add(kv.Key);
            foreach (string key in keysToBeRemoved)
                dict.Remove(key);
            return PrintKeys(keyset, dict);
        }

        public static string PrintCommonWithoutFriendlyKeys(Keyset keyset)
        {
            Dictionary<string, KeyValue> dict = new Dictionary<string, KeyValue>(CommonKeyDict);
            dict.Add("ssl_rsa_kek", new KeyValue("ssl_rsa_kek", 0x10, set => ((HACGUIKeyset)set).SslRsaKek));
            List<string> keysToBeRemoved = new List<string>();
            foreach (KeyValuePair<string, KeyValue> kv in dict)
                if (!HactoolNonFriendlyKeys.Contains(kv.Key))
                    keysToBeRemoved.Add(kv.Key);
            foreach (string key in keysToBeRemoved)
                dict.Remove(key);
            return PrintKeys(keyset, dict);
        }

        /*public new virtual void SetSdSeed(byte[] id)
        {
            base.SetSdSeed(SdSeeds[id]);
        }*/
    }
}
