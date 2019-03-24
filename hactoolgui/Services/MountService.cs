using DokanNet;
using LibHac;
using LibHac.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HACGUI.Services
{
    public class MountService
    {
        private static char[] DriveLetters = "CDEFGHIJKLMNOPQRSTUVWXYZ".ToArray();

        public static readonly string PathSeperator = "\\";
        
        private static Dictionary<MountableFileSystem, Tuple<Thread, char>> Mounted = new Dictionary<MountableFileSystem, Tuple<Thread, char>>();

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
                    Dokan.Mount(fs, $"{drive}:", DokanOptions.RemovableDrive | DokanOptions.WriteProtection);
                }
                ));
                if (Mounted.ContainsKey(fs))
                    Unmount(fs);
                thread.Start();
                Mounted[fs] = new Tuple<Thread, char>(thread, drive);
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
                Dokan.Unmount(fs.Item2);
            foreach (Tuple<Thread, char> fs in new List<Tuple<Thread, char>>(Mounted.Values))
                fs.Item1.Join(); // wait for thread to stop
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
        private readonly Dictionary<IFile, FileStorage> OpenedFiles;
        private readonly object OpenedFileLock = new object();
        private readonly OpenMode Mode;

        public MountableFileSystem(IFileSystem fs, string name, string fileSystemType, OpenMode mode)
        {
            Fs = fs;
            Name = name;
            FileSystemType = fileSystemType;
            OpenedFiles = new Dictionary<IFile, FileStorage>();
            Mode = mode;
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
            if(!info.IsDirectory)
                CloseFile(fileName, info);
            if (info.DeleteOnClose)
            {
                if (info.IsDirectory)
                    Fs.DeleteDirectory(fileName);
                else
                    Fs.DeleteFile(fileName);
            }
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            IFile file = GetFile(fileName);
            if (file != null)
                CloseFile(file);
        }

        public void CloseFile(IFile file)
        {
            lock (OpenedFileLock)
            {
                if (OpenedFiles.ContainsKey(file))
                {
                    OpenedFiles[file].Dispose();
                    OpenedFiles.Remove(file);
                }
            }
        }

        public void CloseAllFiles()
        {
            foreach (IFile file in new List<IFile>(OpenedFiles.Keys))
                CloseFile(file);
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            try
            {
                fileName = FilterPath(fileName);
                bool isDirectory = Fs.DirectoryExists(fileName);

                if (info.IsDirectory || isDirectory)
                {
                    info.IsDirectory = true;
                    if (Fs.FileExists(fileName))
                        return DokanResult.NotADirectory;
                    else if (Fs.DirectoryExists(fileName))
                        return DokanResult.Success;
                    Fs.CreateDirectory(fileName);
                }
                else
                {
                    bool exists = Fs.FileExists(fileName);

                    if (!exists)
                        return DokanResult.FileNotFound;

                    if (mode == FileMode.Open && exists)
                        return DokanResult.Success; 

                    switch (mode)
                    {
                        case FileMode.Create:
                        case FileMode.OpenOrCreate:
                            if (exists)
                                return DokanResult.AlreadyExists;
                            Fs.CreateFile(fileName, 0, CreateFileOptions.None);
                            break;
                    }
                    
                }
                return NtStatus.Success;
            }
            catch (NotImplementedException)
            {
                return NtStatus.NotImplemented;
            }
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
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

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            if (Fs.FileExists(fileName))
                return NtStatus.Success;
            else
                return NtStatus.ObjectNameNotFound;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            return FindFilesWithPattern(fileName, "*", out files, info);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            IDirectory directory = GetDirectory(fileName, OpenDirectoryMode.All);

            files = new List<FileInformation>();
            if (searchPattern.EndsWith("\"*")) // thanks windows
                searchPattern = searchPattern.Replace("\"*", "*");
            try
            {
                foreach (DirectoryEntry entry in directory.EnumerateEntries(searchPattern, SearchOptions.Default))
                    files.Add(CreateInfo(entry, ToWindows(entry.FullPath)));
            } catch(Exception e)
            {
                Console.WriteLine("Exception raised when iterating through directory:\n" + e.Message + "\n" + e.StackTrace);
            }
                return NtStatus.Success;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = null;
            return NtStatus.NotImplemented; // not needed
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            CloseAllFiles();
            Fs.Commit();
            return NtStatus.Success;
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info)
        {
            freeBytesAvailable = 0;
            totalNumberOfBytes = 0;
            totalNumberOfFreeBytes = 0;
            return NtStatus.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info)
        {
            if (GetDirectory(fileName, OpenDirectoryMode.All) != null)
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

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, out uint maximumComponentLength, DokanFileInfo info)
        {
            volumeLabel = Name;
            features = FileSystemFeatures.ReadOnlyVolume; // TODO sort out write support
            fileSystemName = FileSystemType;
            maximumComponentLength = int.MaxValue; // idk lol
            return NtStatus.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Mounted(DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            try
            {
                if (replace && Fs.FileExists(newName))
                    Fs.DeleteFile(newName);
                Fs.RenameFile(oldName, newName);
                return NtStatus.Success;
            }
            catch (NotImplementedException)
            {
                return NtStatus.NotImplemented;
            }
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            FileStorage storage = OpenFile(fileName);
            long size = storage.GetSize() - offset;
            if (size < 0)
            {
                bytesRead = 0;
                return NtStatus.Unsuccessful;
            }
            size = Math.Min(size, buffer.Length);

            storage.Read(buffer, Math.Min(offset, storage.GetSize() - size), (int) size, 0);
            bytesRead = (int)size; // TODO accuracy
            return NtStatus.Success;
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            Fs.OpenFile(FilterPath(fileName), Mode).SetSize(length);
            return NtStatus.Success;
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus Unmounted(DokanFileInfo info)
        {
            lock (OpenedFileLock)
            {
                foreach (IStorage storage in OpenedFiles.Values)
                    storage.Dispose();
            }
            OpenedFiles.Clear();
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            FileStorage storage = OpenFile(fileName);
            long size = storage.GetSize() - offset;
            if (size < 0)
            {
                bytesWritten = 0;
                return NtStatus.Unsuccessful;
            }
            size = Math.Min(size, buffer.Length);

            storage.Write(buffer, Math.Min(offset, storage.GetSize() - size), (int)size, 0);
            bytesWritten = (int)size; // TODO accuracy
            return NtStatus.Success;
        }

        private FileInformation CreateInfo(DirectoryEntry entry, string path)
        {
            switch (entry.Type)
            {
                case DirectoryEntryType.File:
                    return CreateInfo(path);
                case DirectoryEntryType.Directory:
                    return CreateInfo(Fs.OpenDirectory(entry.FullPath, OpenDirectoryMode.All), path);
            }
            return new FileInformation();
        }

        private static string FilterPath(string path)
        {
            path =  path.Replace("\\", "/");
            path = PathTools.Normalize(path);
            if (path.StartsWith("/"))
                path = path.Substring(1);
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
            FileStorage storage = OpenFile(path);
            if (storage != null) {
                return new FileInformation
                {
                    FileName = GetFileName(path),
                    Length = storage.GetSize(),
                    Attributes = FileAttributes.ReadOnly
                };
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

        public IFile GetFile(string name)
        {
            name = FilterPath(name);
            if(Fs.FileExists(name))
                return Fs.OpenFile(name, Mode);
            return null;
        }

        public IDirectory GetDirectory(string name, OpenDirectoryMode mode)
        {
            name = FilterPath(name);
            if (Fs.DirectoryExists(name))
                return Fs.OpenDirectory(name, mode);
            return null;
        }

        public FileStorage OpenFile(string path)
        {
            IFile key = GetFile(path);

            if (key == null)
                return null;

            if (OpenedFiles.ContainsKey(key))
                return OpenedFiles[key];

            try
            {
                FileStorage storage = new FileStorage(key);
                OpenedFiles[key] = storage;
                return storage;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
