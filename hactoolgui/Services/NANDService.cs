using LibHac;
using LibHac.IO;
using LibHac.Nand;
using NandReaderGui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;

namespace HACGUI.Services
{
    public class NANDService
    {
        public delegate void NANDChangedEventHandler();

        public static Func<IStorage, bool> Validator = DefaultValidator;
        public static Func<bool> RequestSwitchSource = () => { return false; };
        public static event NANDChangedEventHandler OnNANDPluggedIn, OnNANDRemoved;
        public static DiskInfo CurrentDisk;

        public static IStorage NANDSource;
        public static Nand NAND;

        private static ManagementEventWatcher CreateWatcher, DeleteWatcher;
        private static bool Started = false;

        private static readonly Func<IStorage, bool> DefaultValidator = 
            (storage) =>
            { // essentially just check if the Nand constructor passes
                try
                {
                    Nand nand = new Nand(storage.AsStream(FileAccess.Read), HACGUIKeyset.Keyset);
                    return true;
                } catch (Exception)
                {
                    return false;
                }
            };

        static NANDService()
        {
            Validator = DefaultValidator;

            // Create event handlers to detect when a device is added or removed
            CreateWatcher = new ManagementEventWatcher();
            WqlEventQuery createQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            CreateWatcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                if (NAND == null) // ignore if we already found the Switch
                    Refresh();
             });
            CreateWatcher.Query = createQuery;

            DeleteWatcher = new ManagementEventWatcher();
            WqlEventQuery deleteQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_USBHub'");
            DeleteWatcher.EventArrived += new EventArrivedEventHandler((s, e) =>
            {
                if (NAND != null) // ignore if we haven't found a Switch yet
                {
                    if (CurrentDisk != null)
                    { // means the NAND is access over USB, so we need to determine if it 
                        ManagementObjectCollection disks = GetDisks();
                        bool found = false;
                        foreach (ManagementObject disk in disks) // search to see if we can match the DiskInfo with an existing device
                        {
                            DiskInfo info = CreateDiskInfo(disk);
                            if (info.PhysicalName.Equals(CurrentDisk.PhysicalName))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                            OnNANDRemoved();
                    }
                }
            });
            DeleteWatcher.Query = deleteQuery;
        }

        private static void Refresh()
        {
            ManagementObjectCollection disks = GetDisks();
            foreach (ManagementObject disk in disks)
            {
                if (disk["Model"].ToString() == "Linux UMS disk 0 USB Device") // probably a bad way of filtering?
                {
                    try
                    {
                        DiskInfo info = CreateDiskInfo(disk);
                        IEnumerable<PartitionInfo> partitions = CreatePartitionInfos(GetPartitions()).Where((p) => info.Index == p.DiskIndex).OrderBy((p) => p.Index);
                        PartitionInfo lastPartition = partitions.Last();
                        long length = (long)(lastPartition.Size + lastPartition.StartingOffset);
                        // thx windows for ignoring the GPT backup AND reporting the size of the disk incorrectly...
                        long missingLength = (0x747BFFE00 - 0x727800000) + 0x200; // (start of GPT backup - end of USER) + length of GPT backup
                        length += missingLength;
                        IStorage diskStorage = new CachedStorage(new DeviceStream(info.PhysicalName, length).AsStorage().AsReadOnly(), info.SectorSize * 100, 4, true);
                        if (InsertNAND(diskStorage, true))
                            CurrentDisk = info;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Cannot direct access drive due to lack of permissions.");
                    }
                }
            }
        }

        private static DiskInfo CreateDiskInfo(ManagementObject disk)
        {
            var info = new DiskInfo
            {
                PhysicalName = (string)disk.GetPropertyValue("Name"),
                Name = (string)disk.GetPropertyValue("Caption"),
                Model = (string)disk.GetPropertyValue("Model"),
                //todo Why is Windows returning small sizes? https://stackoverflow.com/questions/15051660
                Length = (long)((ulong)disk.GetPropertyValue("Size")),
                SectorSize = (int)((uint)disk.GetPropertyValue("BytesPerSector")),
                DisplaySize = Util.GetBytesReadable((long)((ulong)disk.GetPropertyValue("Size"))),
                Partitions = (uint)disk.GetPropertyValue("Partitions"),
                Index = (uint)disk.GetPropertyValue("Index")
            };
            return info;
        }

        private static PartitionInfo CreatePartitionInfo(ManagementObject parition)
        {
            return new PartitionInfo()
            {
                DiskIndex = (uint)parition.GetPropertyValue("DiskIndex"),
                Index = (uint)parition.GetPropertyValue("Index"),
                Size = (ulong)parition.GetPropertyValue("Size"),
                StartingOffset = (ulong)parition.GetPropertyValue("StartingOffset"),
                Name = (string)parition.GetPropertyValue("Name"),
                Description = (string)parition.GetPropertyValue("Description"),
            };
        }

        private static PartitionInfo[] CreatePartitionInfos(ManagementObjectCollection partitions)
        {
            List<PartitionInfo> info = new List<PartitionInfo>();
            foreach (ManagementObject partition in partitions)
            {
                info.Add(CreatePartitionInfo(partition));
            }
            return info.ToArray();
        }

        private static ManagementObjectCollection GetDisks()
        {
            return new ManagementClass("Win32_DiskDrive").GetInstances();
        }

        private static ManagementObjectCollection GetPartitions()
        {
            return new ManagementClass("Win32_DiskPartition").GetInstances();
        }

        public static bool InsertNAND(IStorage input, bool raw)
        {
            if (Validator(input))
            {
                if (NAND != null && !raw)
                    if (!RequestSwitchSource())
                        return false;
                NAND = new Nand(input.AsStream(FileAccess.Read), HACGUIKeyset.Keyset);
                NANDSource = input;
                OnNANDPluggedIn();
                return true;
            }
            else
                return false;
        }

        public static void Start()
        {
            if (Started)
                throw new Exception("NAND service is already started!");

            // Do initial scan of drives
            Refresh();

            CreateWatcher.Start();
            DeleteWatcher.Start();

            Started = true;
        }

        public static void Stop()
        {
            if (!Started)
                throw new Exception("NAND service hasn't started yet!");

            CreateWatcher.Stop();
            DeleteWatcher.Stop();

            if (NANDSource != null)
            {
                NANDSource.AsStream().Close();
                NANDSource.Dispose();
            }

            Started = false;
        }

        public static void ResetHandlers()
        {
            // Clear event handlers
            if(OnNANDPluggedIn != null)
                foreach (Delegate d in OnNANDPluggedIn.GetInvocationList())
                    OnNANDPluggedIn -= (NANDChangedEventHandler)d;
            if (OnNANDRemoved != null)
                foreach (Delegate d in OnNANDRemoved.GetInvocationList())
                    OnNANDRemoved -= (NANDChangedEventHandler)d;

            // Reset validator to default
            Validator = DefaultValidator;
        }

        
    }

    public class DiskInfo
    {
        public string PhysicalName { get; set; }
        public string Name { get; set; }
        public string Model { get; set; }
        public long Length { get; set; }
        public int SectorSize { get; set; }
        public string DisplaySize { get; set; }
        public string Display => $"{Name} ({DisplaySize})";
        public uint Partitions { get; set; }
        public uint Index { get; set; }
    }

    public class PartitionInfo
    {
        public uint DiskIndex { get; set; }
        public uint Index { get; set; }
        public ulong Size { get; set; }
        public ulong StartingOffset { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
