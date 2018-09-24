using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using static HACGUI.Extensions.Extensions;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Instructions : PageExtension
    {
        public static byte[] SBK;
        public static byte[][] TSECKeys; 

        public Instructions()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                SDService.Validator = IsSDCard;
                SDService.OnSDPluggedIn += (drive) =>
                {
                    foreach (DirectoryInfo info in drive.RootDirectory.GetDirectory("backup").GetDirectories())
                        if (IsValidBackupFolder(info))
                        {
                            CopyDump(drive, info);
                            Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                            {
                                btn_next.IsEnabled = true;
                            }));
                            break;
                        }
                };
                SDService.OnSDRemoved += (drive) =>
                {
                    Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                    {
                        btn_next.IsEnabled = false;
                    }));
                    SBK = null;
                    TSECKeys = null;
                };
                SDService.Start();
            };
        }

        private void btn_next_Click(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = FindRoot();

            // Reset SDService so that it's ready for later
            SDService.ResetHandlers();
            SDService.Stop();

            root.Navigate(new DerivingPage((page) => 
            {
                // setup key derivation task and execute it asynchronously on the next page

                Array.Copy(SBK, HACGUIKeyset.Keyset.SecureBootKey, 0x10);
                Array.Copy(TSECKeys[0], HACGUIKeyset.Keyset.TsecKey, 0x10);

                FileStream boot0 = HACGUIKeyset.TempBOOT0FileInfo.OpenRead();
                boot0.Seek(0x180000, SeekOrigin.Begin); // Seek to keyblob area

                for (int i = 0; i < 32; i++)
                {
                    boot0.Read(HACGUIKeyset.Keyset.EncryptedKeyblobs[i], 0, 0xB0);
                    boot0.Seek(0x150, SeekOrigin.Current); // skip empty region
                }

                boot0.Seek(0x100000, SeekOrigin.Begin);
                List<HashSearchEntry> searches = new List<HashSearchEntry>
                {
                    new HashSearchEntry(NintendoKeys.MasterKeySourceHash, 0x10),
                    new HashSearchEntry(NintendoKeys.KeyblobMacKeySourceHash, 0x10)
                };
                Dictionary<byte[], byte[]> hashes = boot0.FindKeyViaHash(searches, new SHA256Managed(), 0x10, 0x40000);
                Array.Copy(hashes[NintendoKeys.MasterKeySourceHash], HACGUIKeyset.Keyset.MasterKeySource, 0x10);
                Array.Copy(hashes[NintendoKeys.KeyblobMacKeySourceHash], HACGUIKeyset.Keyset.KeyblobMacKeySource, 0x10);

                HACGUIKeyset.Keyset.DeriveKeys();

                // Copy package1 into seperate file
                boot0.Seek(0x100000, SeekOrigin.Begin);
                FileStream pkg1stream = HACGUIKeyset.TempPkg1FileInfo.Create();
                boot0.CopyToNew(pkg1stream, 0x40000);
                boot0.Close();
                pkg1stream.Seek(0, SeekOrigin.Begin); // reset position

                HACGUIKeyset.RootTempPkg1FolderInfo.Create();
                Package1 pkg1 = new Package1(HACGUIKeyset.Keyset, pkg1stream);

                // Extracting package1 contents
                FileStream NXBootloaderStream = HACGUIKeyset.TempNXBootloaderFileInfo.Create();
                FileStream SecureMonitorStream = HACGUIKeyset.TempSecureMonitorFileInfo.Create();
                FileStream WarmbootStream = HACGUIKeyset.TempWarmbootFileInfo.Create();
                pkg1.Pk11.OpenNxBootloader().CopyToNew(NXBootloaderStream);
                pkg1.Pk11.OpenSecureMonitor().CopyToNew(SecureMonitorStream);
                pkg1.Pk11.OpenWarmboot().CopyToNew(WarmbootStream);


                searches = new List<HashSearchEntry>
                {
                    new HashSearchEntry(NintendoKeys.Pkg2KeySourceHash, 0x10),
                    new HashSearchEntry(NintendoKeys.TitleKekSourceHash, 0x10),
                    new HashSearchEntry(NintendoKeys.AesKekGenerationSourceHash, 0x10)
                };

                SecureMonitorStream.Seek(0, SeekOrigin.Begin);
                hashes = SecureMonitorStream.FindKeyViaHash(searches, new SHA256Managed(), 0x10);
                Array.Copy(hashes[NintendoKeys.Pkg2KeySourceHash], HACGUIKeyset.Keyset.Package2KeySource, 0x10);
                Array.Copy(hashes[NintendoKeys.TitleKekSourceHash], HACGUIKeyset.Keyset.TitlekekSource, 0x10);
                Array.Copy(hashes[NintendoKeys.AesKekGenerationSourceHash], HACGUIKeyset.Keyset.AesKekGenerationSource, 0x10);

                HACGUIKeyset.Keyset.DeriveKeys(); // derive the additional keys obtained from package1

                // close shit
                NXBootloaderStream.Close();
                SecureMonitorStream.Close();
                WarmbootStream.Close();
                pkg1stream.Close();

                PageExtension next = null;

                // move to next page (after the task is complete)
                page.Dispatcher.BeginInvoke(new Action(() => // move to UI thread
                {
                    next = new PickNANDPage();
                    page.FindRoot().Navigate(next);
                })).Wait(); // must wait, otherwise a race condition may occur

                return next; // return the page we are navigating to
            }));
        }

        public override void OnBack()
        {
            SDService.Stop();
        }

        public static bool IsValidBackupFolder(DirectoryInfo info)
        {
            DirectoryInfo dumpsFolder = info.GetDirectory("dumps");
            if (info.GetFile("BOOT0").Exists)
                if (dumpsFolder.Exists)
                {
                    bool fuseFileExists = dumpsFolder.GetFile("fuses.bin").Exists;
                    bool tsecFileExists = dumpsFolder.GetFile("tsec_keys.bin").Exists;
                    if (fuseFileExists && tsecFileExists)
                        return true;
                }
            return false;
        }

        private static bool IsSDCard(DirectoryInfo info)
        {
            DirectoryInfo rootBackupFolder = info.GetDirectory("backup");
            if (rootBackupFolder.Exists)
            {
                DirectoryInfo[] backupFolderListing = rootBackupFolder.GetDirectories();
                foreach (DirectoryInfo backupFolder in backupFolderListing)
                    if (IsValidBackupFolder(backupFolder))
                        return true;
            }
            return false;
        }

        private static void CopyDump(DriveInfo info, DirectoryInfo backupFolder)
        {
            DirectoryInfo dumpsFolder = backupFolder.GetDirectory("dumps"); // only called when this is already validated, so idc
            byte[] fuses = File.ReadAllBytes(dumpsFolder.GetFile("fuses.bin").FullName);
            byte[] rawTsec = File.ReadAllBytes(dumpsFolder.GetFile("tsec_keys.bin").FullName);
            SBK = fuses.Skip(0xA4).Take(0x10).ToArray();

            TSECKeys = new byte[][]
            {
                rawTsec.Take(0x10).ToArray(),
                rawTsec.Take(0x10).ToArray(),
                rawTsec.Take(0x10).ToArray()
            };

            DirectoryInfo temp = HACGUIKeyset.RootTempFolderInfo;
            temp.Create();
            FileInfo BOOT0 = backupFolder.GetFile("BOOT0");
            string exportpath = Path.Combine(temp.FullName, "BOOT0");
            File.Delete(exportpath);
            File.Copy(BOOT0.FullName, exportpath);
        }
    }
}
