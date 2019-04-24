using HACGUI.Extensions;
using HACGUI.Utilities;
using LibHac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static HACGUI.Utilities.Native;
using static LibHac.ExternalKeys;

namespace HACGUI
{
    public class HACGUIKeyset : Keyset
    {
        public static HACGUIKeyset Keyset = new HACGUIKeyset();

        public const string
            RootFolderName = "HACGUI",
            RootConsoleFolderName = "console",
            RootTempFolderName = "temp",
            ProductionKeysFileName = "prod.keys",
            DeveloperKeysFileName = "dev.keys", // unused, dev support is a hassle within itself
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
            TempContinueFileName = "continue.txt",
            ClientCertificateFileName = "nx_tls_client_cert.pfx",
            TicketFolderName = "tickets",
            CrashZipFileName = "crash.zip",
            AccountsFolderName = "accounts";

        public static DirectoryInfo RootUserDirectory
        {
            get
            {
                if (WorkingDirectoryInfo.GetFile("portable.txt").Exists)
                    return WorkingDirectoryInfo;
                DirectoryInfo info = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                string user = GetLoggedInUser();
                if (info.Name != user)
                    info = info.Parent.GetDirectory(user);
                return info;
            }
        }
        public static DirectoryInfo UserSwitchDirectoryInfo => RootUserDirectory.GetDirectory(UserSwitchFolderName);
        public static DirectoryInfo WorkingDirectoryInfo => new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        public static DirectoryInfo RootFolderInfo => UserSwitchDirectoryInfo.GetDirectory(RootFolderName);
        public static DirectoryInfo RootConsoleFolderInfo => RootFolderInfo.GetDirectory(RootConsoleFolderName);
        public static DirectoryInfo RootTempFolderInfo => WorkingDirectoryInfo.GetDirectory(RootTempFolderName);
        public static DirectoryInfo RootTempPkg1FolderInfo => RootTempFolderInfo.GetDirectory(TempPkg1FolderName);
        public static DirectoryInfo RootTempPkg2FolderInfo => RootTempFolderInfo.GetDirectory(TempPkg2FolderName);
        public static DirectoryInfo RootTempINI1FolderInfo => RootTempPkg2FolderInfo.GetDirectory(TempINI1FolderName);
        public static DirectoryInfo AccountsFolderInfo => RootFolderInfo.GetDirectory(AccountsFolderName);


        public static FileInfo ProductionKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ProductionKeysFileName);
        public static FileInfo DeveloperKeysFileInfo => UserSwitchDirectoryInfo.GetFile(DeveloperKeysFileName);
        public static FileInfo TitleKeysFileInfo => UserSwitchDirectoryInfo.GetFile(TitleKeysFileName);
        public static FileInfo ConsoleKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ConsoleKeysFileName);
        public static FileInfo ExtraKeysFileInfo => UserSwitchDirectoryInfo.GetFile(ExtraKeysFileName);
        public static FileInfo TempBOOT0FileInfo => RootTempFolderInfo.GetFile(TempBOOT0FileName);
        public static FileInfo TempPkg1FileInfo => RootTempFolderInfo.GetFile(TempPkg1FileName);
        public static FileInfo TempNXBootloaderFileInfo => RootTempPkg1FolderInfo.GetFile(TempNXBootloaderFileName);
        public static FileInfo TempSecureMonitorFileInfo => RootTempPkg1FolderInfo.GetFile(TempSecureMonitorFileName);
        public static FileInfo TempWarmbootFileInfo => RootTempPkg1FolderInfo.GetFile(TempWarmbootFileName);
        public static FileInfo TempPkg2FileInfo => RootTempFolderInfo.GetFile(TempPkg2FileName);
        public static FileInfo TempKernelFileInfo => RootTempPkg2FolderInfo.GetFile(TempKernelFileName);
        public static FileInfo TempINI1FileInfo => RootTempPkg2FolderInfo.GetFile(TempINI1FileName);
        public static FileInfo TempPRODINFOFileInfo => RootTempFolderInfo.GetFile(TempPRODINFOFileName);

        public static FileInfo PreferencesFileInfo => RootFolderInfo.GetFile(PreferencesFileName);

        public HACGUIKeyset()
        {
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
            Array.Copy(NintendoKeys.MasterKeySource, MasterKeySource, 0x10);
            Array.Copy(NintendoKeys.KeyblobMacKeySource, KeyblobMacKeySource, 0x10);
            for (int i = 0; i < NintendoKeys.MasterKekSources.Length; i++)
                Array.Copy(NintendoKeys.MasterKekSources[i], MasterKekSources[i], 0x10);
            Array.Copy(NintendoKeys.Pkg2KeySource, Package2KeySource, 0x10);
            Array.Copy(NintendoKeys.TitleKekSource, TitleKekSource, 0x10);
            Array.Copy(NintendoKeys.AesKekGenerationSource, AesKekGenerationSource, 0x10);
        }

        public void LoadCommon()
        {
            if(ExtraKeysFileInfo.Exists)
                ReadKeyFile(Keyset, ExtraKeysFileInfo.FullName);
            string prodPath = ProductionKeysFileInfo.Exists ? ProductionKeysFileInfo.FullName : null;
            string titlePath = TitleKeysFileInfo.Exists ? TitleKeysFileInfo.FullName : null;
            ReadKeyFile(Keyset, prodPath, titlePath);
            Keyset.DeriveKeys();
        }

        public void LoadPersonal(string consoleName)
        {
            ReadKeyFile(Keyset, GetConsoleKeysFileInfoByName(consoleName).FullName);
            Keyset.DeriveKeys();
        }

        public void LoadAll()
        {
            LoadCommon();
            LoadPersonal(Preferences.Current.DefaultConsoleName);
        }

        internal static DirectoryInfo GetConsoleFolderInfo(string name)
        {
            DirectoryInfo d = RootConsoleFolderInfo.GetDirectory(name);
            d.Create();
            return d;
        }

        public static FileInfo GetConsoleKeysFileInfoByName(string name)
        {
            return GetConsoleFolderInfo(name).GetFile(ConsoleKeysFileName);
        }

        public static FileInfo GetClientCertificateByName(string name)
        {
            return GetConsoleFolderInfo(name).GetFile(ClientCertificateFileName);
        }

        public static DirectoryInfo GetTicketsDirectory(string name)
        {
            return GetConsoleFolderInfo(name).GetDirectory(TicketFolderName);
        }

        public static FileInfo GetCrashZip()
        {
            RootTempFolderInfo.Create();
            return RootTempFolderInfo.GetFile(DateTimeOffset.UtcNow.ToUnixTimeSeconds() + CrashZipFileName);
        }

        public static void SetConsole(string name)
        {

        }

        public static Tuple<bool, string> IsValidInstall()
        {
            if (UserSwitchDirectoryInfo.Exists) // Check if ~/.switch exists
            {
                if (RootFolderInfo.Exists) // Check if ~/.switch/HACGUI exists
                {
                    if (!PreferencesFileInfo.Exists) // Check if ~/.switch/HACGUI/preferences.ini exists
                        return new Tuple<bool, string>(false, "Preferences file does not exist.");

                    if(!ProductionKeysFileInfo.Exists)
                        return new Tuple<bool, string>(false, "Prod keys file does not exist.");

                    if(!TitleKeysFileInfo.Exists)
                        return new Tuple<bool, string>(false, "Title keys file does not exist.");

                    if(!ExtraKeysFileInfo.Exists)
                        return new Tuple<bool, string>(false, "Extra keys file does not exist.");


                    if (RootConsoleFolderInfo.Exists) // check if ~/.switch/HACGUI/console exists
                    {
                        if (!RootConsoleFolderInfo.GetDirectories().Any()) // Check if the console directory is empty
                            return new Tuple<bool, string>(false, $"No consoles registered.");

                        foreach (DirectoryInfo consoleDir in RootConsoleFolderInfo.GetDirectories())
                        {
                            string consoleName = consoleDir.Name;

                            if (!GetClientCertificateByName(consoleName).Exists)
                                return new Tuple<bool, string>(false, $"Console \"{consoleName}\" does not have a client certificate.");

                            if (!GetConsoleKeysFileInfoByName(consoleName).Exists)
                                return new Tuple<bool, string>(false, $"Console \"{consoleName}\" does not have a {ConsoleKeysFileName} file.");

                            if(!GetTicketsDirectory(consoleName).Exists)
                                return new Tuple<bool, string>(false, $"Console \"{consoleName}\" does not have a {TicketFolderName} folder.");

                            /*if (!GetAccountsDirectory(consoleName).Exists)
                                return new Tuple<bool, string>(false, $"Console \"{consoleName}\" does not have a {AccountsFolderName} folder.");*/
                        }
                    }
                    else
                        return new Tuple<bool, string>(false, "Console directory does not exist.");

                    return new Tuple<bool, string>(true, "Install valid.");
                }
                else
                    return new Tuple<bool, string>(false, "~/.switch/HACGUI does not exist.");
            }
            return new Tuple<bool, string>(false, "~/.switch folder does not exist.");
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
            "per_console_key_source"
        };

        public static string PrintCommonKeys(Keyset keyset, bool hactoolFriendly)
        {
            Dictionary<string, KeyValue> dict = new Dictionary<string, KeyValue>(CommonKeyDict);
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
