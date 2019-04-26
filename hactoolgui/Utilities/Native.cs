using HACGUI.Extensions;
using LibHac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace HACGUI.Utilities
{
    public class Native
    {

        public static IEnumerable<DiskInfo> CreateDiskInfos(ManagementObjectCollection disks) 
            => disks.OfType<ManagementObject>().Select(i => new DiskInfo(i));
        public static IEnumerable<PartitionInfo> CreatePartitionInfos(ManagementObjectCollection partitions)
            => partitions.OfType<ManagementObject>().Select(i => new PartitionInfo(i));

        public static ManagementObjectCollection GetDisks()
        {
            return new ManagementClass("Win32_DiskDrive").GetInstances();
        }

        public static ManagementObjectCollection GetPartitions()
        {
            return new ManagementClass("Win32_DiskPartition").GetInstances();
        }

        public static string GetLoggedInUser()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT UserName FROM Win32_ComputerSystem");
            IEnumerable<ComputerSystemInfo> infos = searcher.Get().Cast<ManagementObject>()
                .Select(i => new ComputerSystemInfo(i));
            string user = infos.FirstOrDefault()?.UserName;
            if (user == null)
                return "";
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
    }
}
