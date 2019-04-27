using LibHac.IO;
using LibHac.Nand;
using NandReaderGui;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using HACGUI.Utilities;
using static HACGUI.Utilities.Native;

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

        private static readonly ManagementEventWatcher CreateWatcher, DeleteWatcher;
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
                        bool found = false;
                        foreach (DiskInfo info in CreateDiskInfos(GetDisks())) // search to see if we can match the DiskInfo with an existing device
                        {
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
            foreach (DiskInfo info in CreateDiskInfos(GetDisks()))
            {
                if (info.Model == "Linux UMS disk 0 USB Device") // probably a bad way of filtering?
                {
                    try
                    {
                        IEnumerable<PartitionInfo> partitions = CreatePartitionInfos(GetPartitions()).Where((p) => info.Index == p.DiskIndex).OrderBy((p) => p.Index);
                        if (!partitions.Any())
                            continue; // obv the NAND should have *some* partitions
                        PartitionInfo lastPartition = partitions.Last();
                        long length = (long)(lastPartition.Size + lastPartition.StartingOffset);
                        // thx windows for ignoring the GPT backup AND reporting the size of the disk incorrectly...
                        long missingLength = (0x747BFFE00 - 0x727800000) + 0x200; // (start of GPT backup - end of USER) + length of GPT backup
                        length += missingLength;
                        IStorage diskStorage = new CachedStorage(new DeviceStream(info.PhysicalName, length).AsStorage().AsReadOnly(), info.SectorSize * 100, 4, true);
                        if (InsertNAND(diskStorage, true))
                        {
                            CurrentDisk = info;
                            break;
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Cannot direct access drive due to lack of permissions.");
                    }
                }
            }
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


}
