using HACGUI.Extensions;
using LibHac;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HACGUI.Utilities
{
    public class Native
    {
        public static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);

        public static IEnumerable<DiskInfo> CreateDiskInfos(ManagementObjectCollection disks) 
            => disks.OfType<ManagementObject>().Select(i => new DiskInfo(i));
        public static IEnumerable<PartitionInfo> CreatePartitionInfos(ManagementObjectCollection partitions)
            => partitions.OfType<ManagementObject>().Select(i => new PartitionInfo(i));
        public static IEnumerable<UsbDeviceInfo> CreateUsbControllerDeviceInfos(ManagementObjectCollection partitions)
            => partitions.OfType<ManagementObject>().Select(i => new UsbDeviceInfo(i));

        public static ManagementObjectCollection GetDisks()
        {
            return new ManagementClass("Win32_DiskDrive").GetInstances();
        }

        public static ManagementObjectCollection GetPartitions()
        {
            return new ManagementClass("Win32_DiskPartition").GetInstances();
        }

        public static ManagementObjectCollection GetUsbDevices()
        {
            return new ManagementClass("Win32_PnPEntity").GetInstances();
        }

        public static string GetLoggedInUser()
        {
            ManagementScope scope = new ManagementScope($"\\\\{Environment.MachineName}\\root\\cimv2");
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(scope, query);
            IEnumerable<ComputerSystemInfo> infos = searcher.Get().Cast<ManagementObject>()
                .Select(i => new ComputerSystemInfo(i));
            string user = infos.FirstOrDefault(x => x.UserName != null)?.UserName;
            if (user == null) // can occur over a remote desktop connection
                user = Environment.UserName; // will be innaccurate when user is running as admin from an unprivileged user and over RDP
            return user.Substring(user.IndexOf("\\")+1);
        }

        public class ComputerSystemInfo
        {
            public string UserName { get; set; }

            public ComputerSystemInfo(ManagementObject info)
            {
                UserName = (string)info.GetPropertyValue("UserName");
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

            public DiskInfo(ManagementObject disk)
            {
                PhysicalName = (string)disk.GetPropertyValue("Name");
                Name = (string)disk.GetPropertyValue("Caption");
                Model = (string)disk.GetPropertyValue("Model");
                //todo Why is Windows returning small sizes? https://stackoverflow.com/questions/15051660
                Length = (long)disk.GetUlong("Size");
                SectorSize = disk.GetInt("BytesPerSector");
                DisplaySize = Util.GetBytesReadable(Length);
                Partitions = (uint)disk.GetPropertyValue("Partitions");
                Index = (uint)disk.GetPropertyValue("Index");
            }
        }

        public class PartitionInfo
        {
            public uint DiskIndex { get; set; }
            public uint Index { get; set; }
            public ulong Size { get; set; }
            public ulong StartingOffset { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }

            public PartitionInfo(ManagementObject partition)
            {
                DiskIndex = (uint)partition.GetPropertyValue("DiskIndex");
                Index = (uint)partition.GetPropertyValue("Index");
                Size = (ulong)partition.GetPropertyValue("Size");
                StartingOffset = (ulong)partition.GetPropertyValue("StartingOffset");
                Name = (string)partition.GetPropertyValue("Name");
                Description = (string)partition.GetPropertyValue("Description");
            }
        }

        public class UsbDeviceInfo
        {
            public string PNPDeviceID { get; set; }
            public string Description { get; set; }
            public string DeviceID { get; set; }
            public string Name { get; set; }
            public string Manufacturer { get; set; }
            public string CreationClassName { get; set; }
            public string Service { get; set; }
            public string ClassGuid { get; set; }
            public string[] CompatibleID { get; set; }
            public string[] HardwareID { get; set; }

            public UsbDeviceInfo(ManagementObject device)
            {
                PNPDeviceID = (string)device.GetPropertyValue("PNPDeviceID");
                Description = (string)device.GetPropertyValue("Description");
                DeviceID = (string)device.GetPropertyValue("DeviceID");
                Name = (string)device.GetPropertyValue("Name");
                Manufacturer = (string)device.GetPropertyValue("Manufacturer");
                CreationClassName = (string)device.GetPropertyValue("CreationClassName");
                Service = (string)device.GetPropertyValue("Service");
                ClassGuid = (string)device.GetPropertyValue("ClassGuid");
                CompatibleID = (string[])device.GetPropertyValue("CompatibleID");
                HardwareID = (string[])device.GetPropertyValue("HardwareID");
            }

        }
        public static void LaunchProgram(string fileName, Action callback, string args = "", bool asAdmin = false, string workingDirectory = "", bool wait = false)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = fileName;
            proc.StartInfo.UseShellExecute = true;
            if (asAdmin)
                proc.StartInfo.Verb = "runas";
            proc.StartInfo.Arguments = args;
            proc.StartInfo.WorkingDirectory = workingDirectory;
            try
            {
                proc.Start();
                Task task = Task.Run(() =>
                {
                    if(wait)
                        proc.WaitForExit();
                    callback();
                });

                if (wait)
                    task.Wait();
            }
            catch (System.ComponentModel.Win32Exception)
            {
                ;
            }
        }
    }
}
