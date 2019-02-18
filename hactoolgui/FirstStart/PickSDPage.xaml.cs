using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.IO;
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
    public partial class PickSDPage : PageExtension
    {
        public static byte[] SBK;
        public static byte[][] TSECKeys;

        private FileInfo[] _infos = new FileInfo[3];
        private DirectoryInfo backupFolder = null;

        public FileInfo BOOT0FileInfo
        {
            get =>_infos[0];
            set
            {
                StatusService.Statuses[BOOT0FileString] = value  == null ? StatusService.Status.Incorrect : StatusService.Status.OK;
                _infos[0] = value;
            }
        }

        public FileInfo TSECFileInfo
        {
            get => _infos[1];
            set
            {
                StatusService.Statuses[TSECFileString] = value == null ? StatusService.Status.Incorrect : StatusService.Status.OK;
                _infos[1] = value;
            }
        }

        public FileInfo FuseFileInfo
        {
            get => _infos[2];
            set
            {
                StatusService.Statuses[FuseFileString] = value == null ? StatusService.Status.Incorrect : StatusService.Status.OK;
                _infos[2] = value;
            }
        }

        private static readonly string
            SDInsertedString = "SD inserted",
            BackupFolderString = "Backup folder",
            FuseFileString = "Fuse dump exists",
            TSECFileString = "TSEC dump exists",
            BOOT0FileString = "BOOT0 exists";


        public PickSDPage()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                SDService.Validator = IsSDCard;
                SDService.OnSDPluggedIn += (drive) =>
                {
                    StatusService.Statuses[SDInsertedString] = StatusService.Status.OK;
                    CopyDump();
                    Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                    {
                        NextButton.IsEnabled = true;
                    }));
                };
                SDService.OnSDRemoved += (drive) =>
                {
                    StatusService.Statuses[SDInsertedString] = StatusService.Status.Incorrect;
                    StatusService.Statuses[BackupFolderString] = StatusService.Status.Incorrect;
                    StatusService.Statuses[FuseFileString] = StatusService.Status.Incorrect;
                    StatusService.Statuses[TSECFileString] = StatusService.Status.Incorrect;
                    StatusService.Statuses[BOOT0FileString] = StatusService.Status.Incorrect;
                    Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                    {
                        NextButton.IsEnabled = false;
                    }));
                    SBK = null;
                    TSECKeys = null;
                    backupFolder = null;
                };

                StatusService.Bar = StatusBar;
                List<string> toBeRemoved = new List<string>();
                foreach (string obj in StatusService.Statuses.Keys)
                    toBeRemoved.Add(obj); // remove default stuff
                foreach (string obj in toBeRemoved)
                    StatusService.Statuses.Remove(obj);

                StatusService.Statuses[SDInsertedString] = StatusService.Status.Incorrect;
                StatusService.Statuses[BackupFolderString] = StatusService.Status.Incorrect;
                StatusService.Statuses[FuseFileString] = StatusService.Status.Incorrect;
                StatusService.Statuses[TSECFileString] = StatusService.Status.Incorrect;
                StatusService.Statuses[BOOT0FileString] = StatusService.Status.Incorrect;

                StatusService.Start();
                RootWindow.Current.Submit(new System.Threading.Tasks.Task(() => SDService.Start()));
            };
        }

        private void NextButtonClick(object sender, RoutedEventArgs e)
        {
            NavigationWindow root = FindRoot();

            // Reset SDService so that it's ready for later
            SDService.ResetHandlers();
            SDService.Stop();

            StatusService.Stop();

            root.Navigate(new DerivingPage((page) => 
            {
                // setup key derivation task and execute it asynchronously on the next page
                CopyToKeyset();

                FileStream boot0 = HACGUIKeyset.TempBOOT0FileInfo.OpenRead();
                Stream pkg1stream;

                if (HACGUIKeyset.TempPkg1FileInfo.Exists)
                    pkg1stream = HACGUIKeyset.TempPkg1FileInfo.OpenRead();
                else
                    pkg1stream = boot0.AsStorage().Slice(0x100000, 0x40000).AsStream();

                Pk11 pkg1 = null;
                try
                {
                    pkg1 = new Package1(HACGUIKeyset.Keyset, pkg1stream.AsStorage()).Pk11;
                }
                catch (Exception)
                {
                    // likely 6.2.0, need to get extra info
                    Array.Copy(TSECKeys[1], HACGUIKeyset.Keyset.TsecRootKeys[0], 0x10); // we really don't know if there will be future tsec keys, and how it will be handled
                    HACGUIKeyset.Keyset.DeriveKeys();
                }

                // Extracting package1 contents
                if (pkg1 != null)
                {
                    HACGUIKeyset.RootTempPkg1FolderInfo.Create();
                    FileStream NXBootloaderStream = HACGUIKeyset.TempNXBootloaderFileInfo.Create();
                    FileStream SecureMonitorStream = HACGUIKeyset.TempSecureMonitorFileInfo.Create();
                    FileStream WarmbootStream = HACGUIKeyset.TempWarmbootFileInfo.Create();
                    pkg1.OpenNxBootloader().CopyTo(NXBootloaderStream.AsStorage());
                    pkg1.OpenSecureMonitor().CopyTo(SecureMonitorStream.AsStorage());
                    pkg1.OpenWarmboot().CopyToStream(WarmbootStream);
                    NXBootloaderStream.Close();
                    SecureMonitorStream.Close();
                    WarmbootStream.Close();
                    pkg1stream.Close();
                }

                HACGUIKeyset.Keyset.DeriveKeys(); // just to make sure

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

        // avert your eyes from this awful code
        private void ManualPickerButtonClick(object sender, RoutedEventArgs e)
        {
            FileInfo info = RequestOpenFileFromUser("*", "BOOT0 dump | *", "Pick your BOOT0 dump...");
            if (info != null) {
                BOOT0FileInfo = info;
                info = RequestOpenFileFromUser(".bin", "Fuse dump(.bin) | *.bin", "Pick your fuse dump...");
                if(info != null)
                {
                    FuseFileInfo = info;
                    info = RequestOpenFileFromUser(".bin", "TSEC dump(.bin) | *.bin", "Pick your TSEC dump...");
                    if(info != null)
                    {
                        TSECFileInfo = info;
                        CopyDump();
                        Dispatcher.BeginInvoke(new Action(() => // Update on the UI thread
                        {
                            NextButton.IsEnabled = true;
                        }));
                        return;
                    }
                }
            }
        }

        public static void CopyToKeyset()
        {
            Array.Copy(SBK, HACGUIKeyset.Keyset.SecureBootKey, 0x10);
            Array.Copy(TSECKeys[0], HACGUIKeyset.Keyset.TsecKey, 0x10);

            FileStream boot0 = HACGUIKeyset.TempBOOT0FileInfo.OpenRead();
            boot0.Seek(0x180000, SeekOrigin.Begin); // Seek to keyblob area
            for (int i = 0; i < 32; i++)
            {
                boot0.Read(HACGUIKeyset.Keyset.EncryptedKeyblobs[i], 0, 0xB0);
                boot0.Seek(0x150, SeekOrigin.Current); // skip empty region
            }
            boot0.Close();
            HACGUIKeyset.Keyset.DeriveKeys(); // derive from keyblobs
        }

        public bool IsValidBackupFolder(DirectoryInfo info)
        {
            FileInfo[] infos = info.FindFiles(new string[] { "BOOT0", "fuses.bin", "tsec_keys.bin" });
            BOOT0FileInfo = infos[0];
            FuseFileInfo = infos[1];
            TSECFileInfo = infos[2];
            foreach (FileInfo i in infos)
                if (i == null)
                    return false;
            backupFolder = info;
            return true;
        }

        private bool IsSDCard(DirectoryInfo info)
        {
            StatusService.Statuses[SDInsertedString] = StatusService.Status.Progress;
            DirectoryInfo rootBackupFolder = info.GetDirectory("backup");
            if (rootBackupFolder.Exists)
            {
                StatusService.Statuses[SDInsertedString] = StatusService.Status.OK;
                StatusService.Statuses[BackupFolderString] = StatusService.Status.OK;
                return IsValidBackupFolder(rootBackupFolder);
            } else
                StatusService.Statuses[SDInsertedString] = StatusService.Status.Incorrect;
            
            return false;
        }


        private void CopyDump()
        {
            byte[] fuses = File.ReadAllBytes(FuseFileInfo.FullName);
            byte[] rawTsec = File.ReadAllBytes(TSECFileInfo.FullName);
            SBK = fuses.Skip(0xA4).Take(0x10).ToArray();

            TSECKeys = new byte[][]
            {
                rawTsec.Take(0x10).ToArray(),
                rawTsec.Skip(0x10).Take(0x10).ToArray(),
                rawTsec.Skip(0x20).Take(0x10).ToArray()
            };

            DirectoryInfo temp = HACGUIKeyset.RootTempFolderInfo;
            temp.Create();
            string exportpath = Path.Combine(temp.FullName, "BOOT0");
            File.Delete(exportpath);
            File.Copy(BOOT0FileInfo.FullName, exportpath);

            if (backupFolder != null)
            {
                FileInfo pkg1 = backupFolder.FindFile("pkg1_decr.bin");
                if (pkg1 != null)
                {
                    File.Delete(HACGUIKeyset.TempPkg1FileInfo.FullName);
                    File.Copy(pkg1.FullName, HACGUIKeyset.TempPkg1FileInfo.FullName);
                }
            }
        }
    }
}
