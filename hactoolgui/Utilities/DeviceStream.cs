using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace NandReaderGui
{
    public class DeviceStream : Stream
    {
        public const short FileAttributeNormal = 0x80;
        public const short InvalidHandleValue = -1;
        public const uint GenericRead = 0x80000000;
        public const uint GenericWrite = 0x40000000;
        public const uint CreateNew = 1;
        public const uint CreateAlways = 2;
        public const uint OpenExisting = 3;

        // Use interop to call the CreateFile function.
        // For more information about CreateFile,
        // see the unmanaged MSDN reference library.
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateFile(
             [MarshalAs(UnmanagedType.LPWStr)] string filename,
             [MarshalAs(UnmanagedType.U4)] FileAccess access,
             [MarshalAs(UnmanagedType.U4)] FileShare share,
             IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
             [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
             [MarshalAs(UnmanagedType.U4)] FileAttributes flagsAndAttributes,
             IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile,                        // handle to file
            byte[] lpBuffer,                // data buffer
            int nNumberOfBytesToRead,        // number of bytes to read
            ref int lpNumberOfBytesRead,    // number of bytes read
            IntPtr lpOverlapped
        //
        // ref OVERLAPPED lpOverlapped        // overlapped buffer
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool WriteFile(
            SafeFileHandle handle, 
            IntPtr bytes, 
            uint numBytesToWrite, 
            out uint numBytesWritten, 
            IntPtr mustBeZero);

        private SafeFileHandle _handleValue;
        private FileStream _fs;

        public DeviceStream(string device, long length)
        {
            Load(device);
            Length = length;
        }

        private void Load(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            // Try to open the file.
            IntPtr ptr = CreateFile(path, FileAccess.ReadWrite, 0, IntPtr.Zero, FileMode.Open, 0, IntPtr.Zero);

            _handleValue = new SafeFileHandle(ptr, true);

            if (_handleValue.IsInvalid)
                throw new UnauthorizedAccessException("Not enough privilages to get handle of disk.");

            _fs = new FileStream(_handleValue, FileAccess.ReadWrite);

            // If the handle is invalid,
            // get the last Win32 error 
            // and throw a Win32Exception.
            if (_handleValue.IsInvalid)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        public bool ShouldWrite { get; set; } = false;

        public override bool CanRead { get; } = true;

        public override bool CanSeek => true;

        public override bool CanWrite => ShouldWrite;

        public override void Flush() { }

        public override long Length { get; }

        public override long Position
        {
            get => _fs.Position;
            set => _fs.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            var bufBytes = new byte[count];
            if (!ReadFile(_handleValue.DangerousGetHandle(), bufBytes, count, ref bytesRead, IntPtr.Zero))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[offset + i] = bufBytes[i];
            }
            return bytesRead;
        }

        public override int ReadByte()
        {
            int bytesRead = 0;
            var lpBuffer = new byte[1];
            if (!ReadFile(
                _handleValue.DangerousGetHandle(),                        // handle to file
                lpBuffer,                // data buffer
                1,        // number of bytes to read
                ref bytesRead,    // number of bytes read
                IntPtr.Zero
            ))
            { Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error()); }
            return lpBuffer[0];
        }

        public override long Seek(long offset, SeekOrigin origin) => _fs.Seek(offset, origin);

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (CanWrite)
            {
                var bufferPtr = Marshal.AllocHGlobal(count);
                Marshal.Copy(buffer, offset, bufferPtr, count);
                if (!WriteFile(_handleValue, bufferPtr, (uint)count, out _, IntPtr.Zero))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }

                Marshal.FreeHGlobal(bufferPtr);
            }
            else
                throw new NotSupportedException();
        }

        public override void Close()
        {
            _handleValue.Close();
            _handleValue.Dispose();
            _handleValue = null;
            base.Close();
        }

        private bool _disposed;

        protected override void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_handleValue != null)
                    {
                        _fs.Dispose();
                        _handleValue.Close();
                        _handleValue.Dispose();
                        _handleValue = null;
                    }
                }
                // Note disposing has been done.
                _disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
