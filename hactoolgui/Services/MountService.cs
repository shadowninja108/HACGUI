using DokanNet;
using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;

namespace HACGUI.Services
{
    public static class MountService
    {
        private static readonly char[] DriveLetters = "CDEFGHIJKLMNOPQRSTUVWXYZ".ToArray();

        public static readonly string PathSeperator = "\\";
        
        private static readonly Dictionary<MountableFileSystem, Tuple<Thread, char>> Mounted = new Dictionary<MountableFileSystem, Tuple<Thread, char>>();

        static MountService()
        {
            RootWindow.Current.Closed += (_, __) => UnmountAll();
        }

        public static void Mount(MountableFileSystem fs)
        {
            if (CanMount())
            {
                char drive = GetAvailableDriveLetter();
                Thread thread = new Thread(new ThreadStart(() => 
                {
                    DokanOptions options = DokanOptions.RemovableDrive;
                    if (fs.Mode != OpenMode.ReadWrite)
                        options |= DokanOptions.WriteProtection;
                    Dokan.Mount(fs, $"{drive}:", options);
                }
                ));
                if (Mounted.ContainsKey(fs))
                    Unmount(fs);
                thread.Start();
                Mounted[fs] = new Tuple<Thread, char>(thread, drive);
            }
            else
            {
                MessageBox.Show("Dokan driver seems to be missing. Install the driver and try again.");
            }
        }

        public static char GetAvailableDriveLetter()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();
            char[] currentDrives = new char[drives.Length];
            for (int i = 0; i < drives.Length; i++)
                currentDrives[i] = drives[i].Name[0];
            return DriveLetters.Except(currentDrives).First();  
        }

        public static void Unmount(MountableFileSystem fs)
        {
            Dokan.Unmount(Mounted[fs].Item2);
            string mountPoint = $"{Mounted[fs].Item2}:";
            Mounted[fs].Item1.Join(); // wait for thread to actually stop
            Dokan.RemoveMountPoint(mountPoint);
            Mounted.Remove(fs);
        }

        public static void UnmountAll()
        {
            foreach (Tuple<Thread, char> fs in Mounted.Values)
                Dokan.Unmount(fs.Item2); // tell every drive to unmount
            foreach (Tuple<Thread, char> fs in new List<Tuple<Thread, char>>(Mounted.Values))
                fs.Item1.Join(); // wait for thread to stop (wait for drives to be unmounted)
            Mounted.Clear();
        }

        public static bool CanMount()
        {
            try
            {
                Dokan.DriverVersion.ToString();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    public class MountableFileSystem : IDokanOperations
    {
        public readonly string Name, FileSystemType;

        private readonly IFileSystem Fs;
        private readonly Dictionary<string, FileStorage> OpenedFiles;
        private readonly object IOLock = new object();
        public readonly OpenMode Mode;

        public MountableFileSystem(IFileSystem fs, string name, string fileSystemType, OpenMode mode)
        {
            Fs = fs;
            Name = name;
            FileSystemType = fileSystemType;
            OpenedFiles = new Dictionary<string, FileStorage>();
            Mode = mode;
        }

        public void Cleanup(string fileName, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);
            if(!info.IsDirectory)
                CloseFile(fileName, info);
            if (info.DeleteOnClose)
            {
                if (info.IsDirectory)
                    Fs.DeleteDirectory(fileName.ToU8Span());
                else
                    Fs.DeleteFile(fileName.ToU8Span());
            }
        }

        public void CloseFile(string fileName, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);
            CloseFile(fileName);
        }

        public void CloseFile(string fileName)
        {
            fileName = FilterPath(fileName);
            lock (IOLock)
            {
                if (OpenedFiles.ContainsKey(fileName))
                {
                    OpenedFiles[fileName].Dispose();
                    OpenedFiles.Remove(fileName);
                }
            }
        }

        public void CloseAllFiles()
        {
            foreach (string fileName in new List<string>(OpenedFiles.Keys))
                CloseFile(fileName);
            if(Mode == OpenMode.ReadWrite)
                lock(IOLock)
                    Fs.Commit();
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, IDokanFileInfo info)
        {
            if (mode == FileMode.OpenOrCreate || mode == FileMode.CreateNew || mode == FileMode.Create && Mode == OpenMode.Read)
                return DokanResult.AccessDenied;

            try
            {
                fileName = FilterPath(fileName);
                bool isDirectory;
                try
                {
                    Fs.GetEntryType(out DirectoryEntryType type, fileName.ToU8Span());
                    isDirectory = type == DirectoryEntryType.Directory;
                }
                catch (DirectoryNotFoundException)
                {
                    return DokanResult.FileNotFound;
                }

                if (info.IsDirectory || isDirectory)
                {
                    info.IsDirectory = true;
                    if (Fs.FileExists(fileName))
                        return DokanResult.NotADirectory;
                    else if (Fs.DirectoryExists(fileName))
                        return DokanResult.Success;
                    if (mode == FileMode.OpenOrCreate || mode == FileMode.CreateNew || mode == FileMode.Create)
                        lock (IOLock)
                            Fs.CreateDirectory(fileName.ToU8Span());
                    else if (mode == FileMode.Open)
                        return DokanResult.FileNotFound;
                }
                else
                {
                    bool exists = Fs.FileExists(fileName);

                    attributes = CreateInfo(fileName).Attributes;

                    if (mode == FileMode.Open && exists)
                        return DokanResult.Success; 

                    switch (mode)
                    {
                        case FileMode.Create:
                        case FileMode.OpenOrCreate:
                        case FileMode.CreateNew:
                            if (exists)
                                return DokanResult.AlreadyExists;
                            lock(IOLock)
                                Fs.CreateFile(fileName.ToU8Span(), 0, CreateFileOptions.None);
                            if (Fs.FileExists(fileName))
                                return NtStatus.Success;
                            break;
                    }


                    if (!exists)
                        return DokanResult.FileNotFound;
                }
                return NtStatus.Success;
            }
            catch (NotImplementedException)
            {
                return NtStatus.NotImplemented;
            }
        }

        public NtStatus DeleteDirectory(string fileName, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            if (Fs.DirectoryExists(fileName))
                return NtStatus.Success;
            else
            {
                int index = fileName.LastIndexOf('\\');
                if (Fs.DirectoryExists(fileName.Substring(0, index)))
                    return NtStatus.ObjectNameNotFound;
                else
                    return NtStatus.ObjectPathNotFound;
            }
        }

        public NtStatus DeleteFile(string fileName, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            if (Fs.FileExists(fileName))
                return NtStatus.Success;
            else
                return NtStatus.ObjectNameNotFound;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, IDokanFileInfo info)
        {
            return FindFilesWithPattern(fileName, "*", out files, info);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, IDokanFileInfo info)
        {
            IDirectory directory = GetDirectory(fileName, OpenDirectoryMode.All);
            searchPattern = WildcardToRegex(searchPattern);

            files = new List<FileInformation>();
            //if (searchPattern.EndsWith("\"*")) // thanks windows
           //     searchPattern = searchPattern.Replace("\"*", "*");
            try
            {
                directory.GetEntryCount(out long entryCount);
                DirectoryEntry[] entries = new DirectoryEntry[entryCount];

                directory.Read(out entryCount, entries);
                for (int i = 0; i < entryCount; i++)
                {
                    string name = StringUtils.Utf8ZToString(entries[i].Name);
                    if (Regex.IsMatch(name, searchPattern)) {
                        DirectoryEntryEx entry = new DirectoryEntryEx(name, FilterPath(fileName) + "/" + name, entries[i].Type, entries[i].Size);
                        files.Add(CreateInfo(entry, FilterPath(entry.FullPath)));
                    }
                }
            } catch(Exception e)
            {
                Console.WriteLine("Exception raised when iterating through directory:\n" + e.Message + "\n" + e.StackTrace);
            }
            return NtStatus.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, IDokanFileInfo info)
        {
            streams = null;
            return NtStatus.NotImplemented; // not needed
        }

        public NtStatus FlushFileBuffers(string fileName, IDokanFileInfo info)
        {
            CloseAllFiles();
            return NtStatus.Success;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, IDokanFileInfo info)
        {
            Fs.GetFreeSpaceSize(out freeBytesAvailable, "/".ToU8Span());

            Fs.GetTotalSpaceSize(out totalNumberOfBytes, "/".ToU8Span());

            totalNumberOfFreeBytes = freeBytesAvailable;
            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            IDirectory dir = GetDirectory(fileName, OpenDirectoryMode.All);
            if (dir != null)
                fileInfo = CreateInfo(GetDirectory(fileName, OpenDirectoryMode.All), fileName);
            else if (GetFile(fileName) != null)
                fileInfo = CreateInfo(fileName);
            else
            {
                fileInfo = new FileInformation();
                return NtStatus.NoSuchFile;
            }
            return NtStatus.Success;
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, IDokanFileInfo info)
        {
            volumeLabel = Name;
            features = 0;
            fileSystemName = FileSystemType;
            maximumComponentLength = int.MaxValue; // idk lol
            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Mounted(IDokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, IDokanFileInfo info)
        {
            try
            {
                if (replace && Fs.FileExists(newName))
                    Fs.DeleteFile(newName.ToU8Span());
                Fs.RenameFile(oldName.ToU8Span(), newName.ToU8Span());
                return NtStatus.Success;
            }
            catch (NotImplementedException)
            {
                return NtStatus.NotImplemented;
            }
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            lock (IOLock)
            {
                FileStorage storage = GetFile(fileName);
                storage.GetSize(out long size);
                long distance = size - offset;
                if (distance < 0)
                {
                    bytesRead = 0;
                    return NtStatus.Unsuccessful;
                }
                distance = Math.Min(distance, buffer.Length);

                storage.Read(Math.Min(offset, size - distance), buffer, (int)distance, 0);
                bytesRead = (int)distance; // TODO accuracy
                return NtStatus.Success;
            }
        }

        public NtStatus SetAllocationSize(string fileName, long length, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            lock (IOLock)
            {
                try
                {
                    IStorage storage = GetFile(fileName);
                    storage.SetSize(length);
                    CloseFile(fileName);
                    return NtStatus.Success;
                }
                catch (NotImplementedException)
                {
                    return NtStatus.NotImplemented;
                }
            }
        }
        public NtStatus SetEndOfFile(string fileName, long length, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);

            lock (IOLock)
                GetFile(fileName).SetSize(length);
            CloseFile(fileName);

            return NtStatus.Success;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, IDokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Unmounted(IDokanFileInfo info)
        {
            CloseAllFiles();
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, IDokanFileInfo info)
        {
            fileName = FilterPath(fileName);
            lock (IOLock)
            {
                FileStorage storage = GetFile(fileName);
                storage.GetSize(out long size);

                long distance = size - offset; // distance to EOF
                if (distance < 0) // can't write out side the file dummy
                {
                    bytesWritten = 0;
                    return NtStatus.Unsuccessful;
                }
                distance = Math.Min(distance, buffer.Length); // prevent buffer from writing past EOF

                storage.Write(Math.Min(offset, size - distance), buffer, (int)distance, 0);
                bytesWritten = (int)distance; // TODO accuracy
                return NtStatus.Success;
            }
        }

        private FileInformation CreateInfo(DirectoryEntryEx entry, string path)
        {
            switch (entry.Type)
            {
                case DirectoryEntryType.File:
                    return CreateInfo(path);
                case DirectoryEntryType.Directory:
                    return CreateInfo(GetDirectory(path, OpenDirectoryMode.All), path);
            }
            return new FileInformation();
        }

        private static string FilterPath(string path)
        {
            path =  path.Replace("\\", "/");
            path = PathTools.Normalize(path);
            if (string.IsNullOrWhiteSpace(path))
                path = "/";
            return path;
        }

        private static string ToWindows(string path)
        {
            path = path.Replace("/", "\\");
            if (!path.StartsWith("\\"))
                path = "\\" + path;
            return path;
        }

        private static string GetFileName(string path)
        {
            return Path.GetFileName(FilterPath(path));
        }

        private FileInformation CreateInfo(string path)
        {
            path = FilterPath(path);

            FileStorage storage = GetFile(path);
            if (storage != null) {
                FileInformation f =  new FileInformation
                {
                    FileName = GetFileName(path),
                    Attributes = Mode == OpenMode.Read ? FileAttributes.ReadOnly : FileAttributes.Normal
                };
                storage.GetSize(out long length);
                f.Length = length;

                CloseFile(path);
                return f;
            }
            else
                return new FileInformation();
        }

        private static FileInformation CreateInfo(IDirectory directory, string path)
        {
            if (directory != null)
                return new FileInformation
                {
                    FileName = GetFileName(path),
                    Attributes = FileAttributes.Directory
                };
            else
                return new FileInformation();
        }

        public IFile OpenFile(string name)
        {
            IFile file = null;
            name = FilterPath(name);
            if(Fs.FileExists(name))
                Fs.OpenFile(out file, name.ToU8Span(), Mode);
            return file;
        }

        public IDirectory GetDirectory(string name, OpenDirectoryMode mode)
        {
            IDirectory dir = null;
            name = FilterPath(name);
            if (Fs.DirectoryExists(name))
                Fs.OpenDirectory(out dir, name.ToU8Span(), mode);
            return dir;
        }

        public FileStorage GetFile(string path)
        {
            lock (IOLock)
            {
                if (OpenedFiles.ContainsKey(path))
                    return OpenedFiles[path];

                IFile key = OpenFile(path);

                if (key == null)
                    return null;

                try
                {
                    FileStorage storage = new FileStorage(key);
                    OpenedFiles[path] = storage;
                    return storage;
                }
                catch (UnauthorizedAccessException)
                {
                    return null;
                }
            }
        }
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
            Replace("\\*", ".*").
            Replace("\\?", ".") + "$";
        }
    }
}
