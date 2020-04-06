using HACGUI.Extensions;
using IniParser;
using IniParser.Model;
using LibHac.Fs;
using LibHac.FsSystem;
using NandReaderGui;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using static HACGUI.Utilities.Native;

namespace HACGUI.Services
{
    public static class SDService
    {
        public delegate void SDChangedEventHandler(DirectoryInfo drive);

        public static Func<DirectoryInfo, bool> Validator = DefaultValidator;
        public static event SDChangedEventHandler OnSDPluggedIn, OnSDRemoved;
        public static DriveInfo CurrentDrive;
        public static DirectoryInfo SDEffectiveRoot;
        public static DirectoryInfo SDRoot;

        private static readonly ManagementEventWatcher Watcher;
        private static bool Started = false;

        private static readonly Func<DirectoryInfo, bool> DefaultValidator =
            (info) =>
            {
                return info.GetDirectory("Nintendo").GetDirectory("Contents").Exists;
            };

        static SDService()
        {
            Validator = DefaultValidator;

            // Create an event handler to detect when a drive is added or removed
            Watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM __InstanceOperationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_LogicalDisk' AND TargetInstance.Description = \"Removable disk\"");

            Watcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                string driveName = ((ManagementBaseObject)e.NewEvent["TargetInstance"]).Properties["DeviceID"].Value.ToString();
                DriveInfo actedDrive = new DriveInfo(driveName);
                DirectoryInfo actedDriveRoot = FindRootForSd(actedDrive);

                if (actedDrive.IsReady)
                {
                    if (CurrentDrive != null && CurrentDrive.Name == actedDrive.Name)
                        OnSDRemoved?.Invoke(SDEffectiveRoot);

                    if (Validator(actedDriveRoot))
                    {
                        CurrentDrive = actedDrive; // set current drive so the event handler *could* access it directly, but why tho
                        SDRoot = CurrentDrive.RootDirectory;
                        SDEffectiveRoot = actedDriveRoot;
                        OnSDPluggedIn?.Invoke(SDEffectiveRoot);
                    }
                }
                else if (CurrentDrive != null) // if a drive hasn't been found to begin with, no need to check what was removed
                {
                    DirectoryInfo currentDrive = new DirectoryInfo(CurrentDrive.Name);
                    if (currentDrive.Name == actedDrive.Name) // was the removed drive the one we found?
                    {
                        OnSDRemoved?.Invoke(SDEffectiveRoot); // Allow the handler read the current drive before we clear it
                        CurrentDrive = null;
                        SDRoot = null;
                        SDEffectiveRoot = null;
                    }
                }
            });
            Watcher.Query = query;
        }

        public static void Start()
        {
            if (Started)
                throw new Exception("SD service is already started!");

            // Do initial scan of drives
            Refresh();

            Watcher.Start();

            Started = true;
        }

        private static bool FindEmuMMC(DriveInfo drive)
        {
            DirectoryInfo root = drive.RootDirectory;
            FileInfo emummcIni = root.GetFile("emuMMC/emummc.ini");
            if (emummcIni.Exists)
            {
                // find the DiskDrive associated with this drive letter
                VolumeInfo volume = AllVolumes
                    .Where(x => x.Caption == drive.Name).FirstOrDefault();

                LogicalDiskInfo logicalDisk = AllLogicalDisks
                    .Where(x => x.DeviceID == volume.DriveLetter).FirstOrDefault();

                IEnumerable<PartitionInfo> partitionsFromLogicalDisk = ToDiskPartitions(logicalDisk);
                if (!partitionsFromLogicalDisk.Any())
                    return false;

                DiskInfo disk = AllDisks.Where(x => x.Index == partitionsFromLogicalDisk.First().DiskIndex).FirstOrDefault();

                IEnumerable<PartitionInfo> partitions = AllPartitions.Where(x => x.DiskIndex == disk.Index);

                // parse ini
                FileIniDataParser parser = new FileIniDataParser();

                IniData ini = parser.ReadFile(emummcIni.FullName);
                ini.SectionKeySeparator = '/';

                if (!ini.TryGetKey("emummc/sector", out string sectorStr))
                {
                    return false;
                }

                ulong sector = ulong.Parse(sectorStr.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);

                PartitionInfo partition = partitions.Where(x =>
                    (sector * x.BlockSize)
                    - 0x1000000 /* hekate's 16MB padding to protect the emuMMC */

                    == x.StartingOffset).FirstOrDefault();

                bool usingEmummc = partition != null;
                if (usingEmummc)
                {
                    MessageBoxResult r = MessageBox.Show("emuMMC was detected on this SD card. Do you want to open that instead of sysMMC content?", "emuMMC", MessageBoxButton.YesNo);
                    if (r == MessageBoxResult.No)
                        usingEmummc = false;
                }

                if (usingEmummc)
                {
                    DeviceStream stream = new DeviceStream(disk.PhysicalName, disk.Length);
                    IStorage diskStorage = new CachedStorage(stream.AsStorage().AsReadOnly(), disk.SectorSize * 100, 4, true);
                    long offset = (long)partition.StartingOffset;
                    offset += 0x1000000; // account for hekate's padding
                    offset += 0x400000; // BOOT0
                    offset += 0x400000; // BOOT1


                    NANDService.InsertNAND(diskStorage.Slice(offset, (long)partition.Size), false);
                }

                return usingEmummc;
            }

            return false;
        }

        public static DirectoryInfo FindRootForSd(DriveInfo drive)
        {
            DirectoryInfo root = drive.RootDirectory;

            if (FindEmuMMC(drive))
                root = root.GetDirectory("emummc/RAW1");

            return root;
        }

        public static void Refresh()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            foreach (DriveInfo drive in drives)
                if (drive.IsReady)
                {
                    DirectoryInfo root = FindRootForSd(drive);

                    if (Validator(root))
                    {
                        CurrentDrive = drive;
                        SDRoot = drive.RootDirectory;
                        SDEffectiveRoot = root;
                        OnSDPluggedIn(SDEffectiveRoot);
                        break;
                    }
                }
        }

        public static void Stop()
        {
            if (!Started)
                throw new Exception("SD service hasn't started yet!");

            Watcher.Stop();

            Started = false;
        }

        public static void ResetHandlers()
        {
            // Clear event handlers
            if (OnSDPluggedIn != null)
                foreach (Delegate d in OnSDPluggedIn.GetInvocationList())
                    OnSDPluggedIn -= (SDChangedEventHandler)d;
            if (OnSDRemoved != null)
                foreach (Delegate d in OnSDRemoved.GetInvocationList())
                    OnSDRemoved -= (SDChangedEventHandler)d;

            // Reset validator to default
            Validator = DefaultValidator;
        }
    }
}
