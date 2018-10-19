using LibHac;
using LibHac.Streams;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI
{
    public class DiskStream : Stream
    {

        public const short 
            FileAttributeNormal = 0x80,
            InvalidHandleValue = -1;
        public const uint 
            GenericRead = 0x80000000,
            GenericWrite = 0x40000000,
            CreateNew = 1,
            CreateAlways = 2,
            OpenExisting = 3;

        public override bool CanRead { get; } = true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override void Flush() { }

        public override long Length { get => length; }

        public override long Position
        {
            get => stream.Position;
            set => stream.Position = value;
        }

        private long length;
        private SafeFileHandle handle;
        private Stream stream;
        private bool disposed;

        public DiskStream(DiskInfo disk)
        {
            handle = CreateHandle(disk);

            if (handle.IsInvalid)
                throw new UnauthorizedAccessException();

            length = disk.Length;

            FileStream filestream = new FileStream(handle, FileAccess.Read);
            stream = new RandomAccessSectorStream(new SectorStream(filestream, disk.SectorSize * 100));
        }

        public static SafeFileHandle CreateHandle(DiskInfo diskInfo)
        {
            SafeFileHandle driveHandle = CreateFile(diskInfo.PhysicalName,
                        FileAccess.Read,
                        FileShare.None,
                        IntPtr.Zero,
                        FileMode.Open,
                        FileAttributes.Normal,
                        IntPtr.Zero);

            return driveHandle;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            var bufBytes = new byte[count];
            if (!ReadFile(handle.DangerousGetHandle(), bufBytes, count, ref bytesRead, IntPtr.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[offset + i] = bufBytes[i];
            }
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (handle != null)
                    {
                        stream.Dispose();
                        handle.Close();
                        handle.Dispose();
                        handle = null;
                    }
                }
                // Note disposing has been done.
                disposed = true;
                base.Dispose(disposing);
            }
        }

        public static DiskInfo CreateDiskInfo(ManagementBaseObject drive)
        {
            var info = new DiskInfo();
            info.PhysicalName = (string)drive["Name"];
            info.Name = (string)drive["Caption"];
            info.Model = (string)drive["Model"];
            info.Length = (long)((ulong)drive["Size"]);
            info.SectorSize = (int)((uint)drive["BytesPerSector"]);
            info.DisplaySize = Util.GetBytesReadable((long)((ulong)drive["Size"]));
            return info;
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
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern SafeFileHandle CreateFile(
           string lpFileName,
           [MarshalAs(UnmanagedType.U4)] FileAccess dwDesiredAccess,
           [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode,
           IntPtr lpSecurityAttributes,
           [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition,
           [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes,
           IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile,                        // handle to file
            byte[] lpBuffer,                // data buffer
            int nNumberOfBytesToRead,        // number of bytes to read
            ref int lpNumberOfBytesRead,    // number of bytes read
            IntPtr lpOverlapped);

    }
}
