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
                    Mounted.Remove(fs);
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
            Mounted[fs].Item1.Join();
            Dokan.RemoveMountPoint(mountPoint);
        }

        public static void UnmountAll()
        {
            foreach (MountableFileSystem fs in new List<MountableFileSystem>(Mounted.Keys))
                Unmount(fs);
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
        public readonly string Name;
        private readonly IFileSystem Fs;
        private readonly Dictionary<IFile, IStorage> OpenedFiles;

        public MountableFileSystem(IFileSystem fs, string name)
        {
            Fs = fs;
            Name = name;
            OpenedFiles = new Dictionary<IFile, IStorage>();
        }

        public void Cleanup(string fileName, DokanFileInfo info)
        {
        }

        public void CloseFile(string fileName, DokanFileInfo info)
        {
            IFile file = GetFile(fileName);
            if (OpenedFiles.ContainsKey(file))
            {
                OpenedFiles[file].Dispose();
                OpenedFiles.Remove(file);
            }
        }

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info)
        {
            return NtStatus.Success;
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            return FindFilesWithPattern(fileName, "*", out files, info);
        }

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info)
        {
            IDirectory directory = GetDirectory(fileName);
            files = null;
            if (directory.Exists)
            {
                files = new List<FileInformation>();
                if (searchPattern.EndsWith("\"*")) // thanks windows
                    searchPattern = searchPattern.Replace("\"*", "*");
                foreach (IFileSytemEntry entry in directory.GetFileSystemEntries(searchPattern))
                    files.Add(CreateInfo(entry));
                return NtStatus.Success;
            }
            return NtStatus.NotADirectory;
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = null;
            return NtStatus.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
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
            if (GetDirectory(fileName).Exists)
                fileInfo = CreateInfo(GetDirectory(fileName));
            else if (GetFile(fileName).Exists)
                fileInfo = CreateInfo(GetFile(fileName));
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
            fileSystemName = Name;
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
            return NtStatus.NotImplemented;
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            IFile file = GetFile(fileName);
            if (file.Exists)
            {
                IStorage storage = OpenFile(file);
                if (storage != null)
                {
                    long distanceToEof = storage.Length - buffer.Length;

                    storage.Read(buffer, Math.Min(offset, distanceToEof), (int)Math.Min(buffer.Length, file.Length), 0);
                    bytesRead = buffer.Length; // TODO accuracy
                    return NtStatus.Success;
                } else
                {
                    bytesRead = 0;
                    return NtStatus.Unsuccessful;
                }
            }
            bytesRead = 0;
            return NtStatus.NoSuchFile;
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.NotImplemented;
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
            foreach (IStorage storage in OpenedFiles.Values)
                storage.Dispose();
            OpenedFiles.Clear();
            return NtStatus.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            bytesWritten = 0;
            return NtStatus.NotImplemented;
        }

        private static FileInformation CreateInfo(IFile file)
        {
            if (file.Exists)
                return new FileInformation
                {
                    FileName = file.FileName,
                    Length = file.Length,
                    Attributes = FileAttributes.ReadOnly
                };
            else
                return new FileInformation();
        }

        private static FileInformation CreateInfo(IFileSytemEntry entry)
        {
            if ((entry as IFile) != null)
                return CreateInfo((IFile)entry);

            FileInformation info = new FileInformation
            {
                FileName = entry.Name,
                Attributes = (entry as IDirectory) == null ? FileAttributes.Normal : FileAttributes.Directory,
            };
            return info;
        }

        public IFile GetFile(string name)
        {
            name = name.Replace($"{Path.DirectorySeparatorChar}", Fs.PathSeperator);
            if (name.StartsWith(Fs.PathSeperator))
                name = name.Substring(Fs.PathSeperator.Length);
            return Fs.GetFile(name);
        }

        public IDirectory GetDirectory(string name)
        {
            name = name.Replace($"{Path.DirectorySeparatorChar}", Fs.PathSeperator);
            if (name.StartsWith(Fs.PathSeperator))
                name = name.Substring(Fs.PathSeperator.Length);
            return Fs.GetDirectory(name);
        }

        public static string GetPath(IFileSytemEntry file)
        {
            string str = file.Path;
            if(str.Length > 0)
                if (str[0] == Path.DirectorySeparatorChar)
                    str = str.Substring(1);
            return str;
        }

        public IStorage OpenFile(IFile file)
        {
            IFile key = OpenedFiles.Keys.FirstOrDefault(f => f.Equals(file));
            if (key != null)
                return OpenedFiles[key];

            try
            {
                IStorage storage = file.Open(FileMode.Open);
                OpenedFiles[file] = storage;
                return storage;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
