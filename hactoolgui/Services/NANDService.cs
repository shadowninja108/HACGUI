using LibHac.Nand;
using LibHac.Streams;
using NandReaderGui;
using System;
using System.IO;
using System.Management;
using static HACGUI.DiskStream;

namespace HACGUI.Services
{
    public class NANDService
    {
        public delegate void NANDChangedEventHandler();

        public static Func<Stream, bool> Validator = DefaultValidator;
        public static Func<bool> RequestSwitchSource = () => { return false; };
        public static event NANDChangedEventHandler OnNANDPluggedIn, OnNANDRemoved;
        public static DiskInfo CurrentDisk;

        public static Stream NANDSource;
        public static Nand NAND;

        private static ManagementEventWatcher CreateWatcher, DeleteWatcher;
        private static bool Started = false;

        private static Func<Stream, bool> DefaultValidator = 
            (stream) =>
            { // essentially just check if the Nand constructor passes
                try
                {
                    Nand nand = new Nand(stream, HACGUIKeyset.Keyset);
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
                    IterateDisks();
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
                            if (info.Equals(CurrentDisk))
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

        private static void IterateDisks()
        {
            ManagementObjectCollection disks = GetDisks();
            foreach (ManagementObject disk in disks)
            {
                if (disk["Model"].ToString() == "Linux UMS disk 0 USB Device") // probably a bad way of filtering?
                {
                    try
                    {
                        DiskInfo info = CreateDiskInfo(disk);
                        //DiskStream diskStream = new DiskStream(DiskStream.CreateDiskInfo(disk)); impl is shit i guess
                        Stream diskStream = new RandomAccessSectorStream(new SectorStream(new DeviceStream(info.PhysicalName, info.Length), info.SectorSize * 100));
                        if(InsertNAND(diskStream, true))
                            CurrentDisk = info;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Cannot direct access drive due to lack of permissions.");
                    }
                }
            }
        }

        private static ManagementObjectCollection GetDisks()
        {
            ManagementClass wmi = new ManagementClass("Win32_DiskDrive");
            return wmi.GetInstances();
        }

        public static bool InsertNAND(Stream input, bool raw)
        {
            if (Validator(input))
            {
                if (NAND != null && !raw)
                    if (!RequestSwitchSource())
                        return false;
                NAND = new Nand(input, HACGUIKeyset.Keyset);
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
            IterateDisks();

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
                NANDSource.Close();
                NANDSource.Dispose();
            }

            Started = false;
        }

        public static void ResetHandlers()
        {
            // Clear event handlers
            foreach (Delegate d in OnNANDPluggedIn.GetInvocationList())
                OnNANDPluggedIn -= (NANDChangedEventHandler)d;
            foreach (Delegate d in OnNANDRemoved.GetInvocationList())
                OnNANDRemoved -= (NANDChangedEventHandler)d;

            // Reset validator to default
            Validator = DefaultValidator;
        }

        
    }
}
