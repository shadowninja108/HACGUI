using LibHac.Fs;
using System;
using System.Collections.Generic;

namespace HACGUI.Utilities
{
    public class PseudoFileSystem : IAttributeFileSystem
    {
        public Dictionary<string, Tuple<string, IAttributeFileSystem>> FileDict;
        public Dictionary<string, IStorage> StorageDict;

        public PseudoFileSystem()
        {
            FileDict = new Dictionary<string, Tuple<string, IAttributeFileSystem>>();
            StorageDict = new Dictionary<string, IStorage>();
        }

        public void Add(string path, string internalPath, IAttributeFileSystem fs)
        {
            FileDict[path] = new Tuple<string, IAttributeFileSystem>(internalPath, fs);
        }

        public void Add(string path, IStorage storage)
        {
            StorageDict[path] = storage;
        }

        public void CleanDirectoryRecursively(string path)
        {
            throw new NotImplementedException();
        }

        public void Commit()
        {
            throw new NotImplementedException();
        }

        public void CreateDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public void CreateFile(string path, long size, CreateFileOptions options)
        {
            throw new NotImplementedException();
        }

        public void DeleteDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteDirectoryRecursively(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path)
        {
            foreach (string p in FileDict.Keys)
            {
                if (p.StartsWith(path) && !FileDict.ContainsKey(path))
                    return true;
            }
            foreach (string p in StorageDict.Keys)
            {
                if (p.StartsWith(path) && !StorageDict.ContainsKey(path))
                    return true;
            }
            return false;
        }

        public bool FileExists(string path)
        {
            if (FileDict.ContainsKey(path))
                return true;
            if (StorageDict.ContainsKey(path))
                return true;
            return false;
        }

        public DirectoryEntryType GetEntryType(string path)
        {
            throw new NotImplementedException();
        }

        public NxFileAttributes GetFileAttributes(string path)
        {
            throw new NotImplementedException();
        }

        public long GetFileSize(string path)
        {
            if (FileDict.ContainsKey(path))
            {
                Tuple<string, IAttributeFileSystem> t = FileDict[path];
                return t.Item2.GetFileSize(t.Item1);
            }
            if (StorageDict.ContainsKey(path))
            {
                return StorageDict[path].GetSize();
            }
            return -1;
        }

        public FileTimeStampRaw GetFileTimeStampRaw(string path)
        {
            throw new NotImplementedException();
        }

        public long GetFreeSpaceSize(string path)
        {
            throw new NotImplementedException();
        }

        public long GetTotalSpaceSize(string path)
        {
            throw new NotImplementedException();
        }

        public IDirectory OpenDirectory(string path, OpenDirectoryMode mode)
        {
            if (path == "/")
                return new PseudoRootDirectory(this);
            else
                throw new NotImplementedException();
        }

        public IFile OpenFile(string path, OpenMode mode)
        {
            if (FileDict.ContainsKey(path))
            {
                Tuple<string, IAttributeFileSystem> t = FileDict[path];
                return t.Item2.OpenFile(t.Item1, mode);
            }
            if (StorageDict.ContainsKey(path))
            {
                return StorageDict[path].AsFile(mode);
            }
            return null;
        }

        public void QueryEntry(Span<byte> outBuffer, ReadOnlySpan<byte> inBuffer, string path, QueryId queryId)
        {
            throw new NotImplementedException();
        }

        public void RenameDirectory(string srcPath, string dstPath)
        {
            throw new NotImplementedException();
        }

        public void RenameFile(string srcPath, string dstPath)
        {
            throw new NotImplementedException();
        }

        public void SetFileAttributes(string path, NxFileAttributes attributes)
        {
            Tuple<string, IAttributeFileSystem> t = FileDict[path];
            t.Item2.SetFileAttributes(t.Item1, attributes);
        }
    }
}
