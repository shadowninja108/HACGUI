using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using System;
using System.Collections.Generic;

namespace HACGUI.Utilities
{
    public class PseudoFileSystem : FileSystemBase, IAttributeFileSystem
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
            path = PathTools.Normalize(path);
            FileDict[path] = new Tuple<string, IAttributeFileSystem>(internalPath, fs);
        }

        public void Add(string path, IStorage storage)
        {
            path = PathTools.Normalize(path);
            StorageDict[path] = storage;
        }

        protected override Result CleanDirectoryRecursivelyImpl(U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result CommitImpl()
        {
            throw new NotImplementedException();
        }
        public Result CreateDirectory(U8Span path, NxFileAttributes attributes)
        {
            throw new NotImplementedException();
        }

        protected override Result CreateDirectoryImpl(U8Span path)
        {
            throw new NotImplementedException();
        }
     

        protected override Result CreateFileImpl(U8Span path, long size, CreateFileOptions options)
        {
            throw new NotImplementedException();
        }

        protected override Result DeleteDirectoryImpl(U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result DeleteDirectoryRecursivelyImpl(U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result DeleteFileImpl(U8Span path)
        {
            throw new NotImplementedException();
        }

        public bool DirectoryExists(string path)
        {
            path = PathTools.Normalize(path);
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
            path = PathTools.Normalize(path);
            if (FileDict.ContainsKey(path))
                return true;
            if (StorageDict.ContainsKey(path))
                return true;
            return false;
        }

        protected override Result GetEntryTypeImpl(out DirectoryEntryType type, U8Span path)
        {
            throw new NotImplementedException();
        }

        public Result GetFileAttributes(out NxFileAttributes attributes, U8Span path)
        {
            throw new NotImplementedException();
        }

        public Result GetFileSize(out long size, U8Span spanpath)
        {
            string path = PathTools.Normalize(spanpath.ToString());
            if (FileDict.ContainsKey(path))
            {
                Tuple<string, IAttributeFileSystem> t = FileDict[path];
                return t.Item2.GetFileSize(out size, t.Item1.ToU8Span());
            }
            if (StorageDict.ContainsKey(path))
            {
                return StorageDict[path].GetSize(out size);
            } 
            size = 0;
            return ResultFs.PathNotFound.Value;
        }

        protected override Result GetFileTimeStampRawImpl(out FileTimeStampRaw timestamp, U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result GetFreeSpaceSizeImpl(out long size, U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result GetTotalSpaceSizeImpl(out long size, U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result OpenDirectoryImpl(out IDirectory directory, U8Span spanpath, OpenDirectoryMode mode)
        {
            string path = PathTools.Normalize(spanpath.ToString());
            if (path == "/")
                directory = new PseudoRootDirectory(this);
            else
                throw new NotImplementedException();

            return Result.Success;
        }

        protected override Result OpenFileImpl(out IFile file, U8Span spanpath, OpenMode mode)
        {
            string path = PathTools.Normalize(spanpath.ToString());
            if (FileDict.ContainsKey(path))
            {
                Tuple<string, IAttributeFileSystem> t = FileDict[path];
                return t.Item2.OpenFile(out file, t.Item1.ToU8Span(), mode);
            }
            if (StorageDict.ContainsKey(path))
            {
                file = StorageDict[path].AsFile(mode);
                return Result.Success;
            }

            file = null;
            return ResultFs.PathNotFound.Value;
        }

        protected override Result QueryEntryImpl(Span<byte> outBuffer, ReadOnlySpan<byte> inBuffer, QueryId queryId, U8Span path)
        {
            throw new NotImplementedException();
        }

        protected override Result RenameDirectoryImpl(U8Span srcPath, U8Span dstPath)
        {
            throw new NotImplementedException();
        }

        protected override Result RenameFileImpl(U8Span srcPath, U8Span dstPath)
        {
            throw new NotImplementedException();
        }

        public Result SetFileAttributes(U8Span spanpath, NxFileAttributes attributes)
        {
            string path = PathTools.Normalize(spanpath.ToString());
            Tuple<string, IAttributeFileSystem> t = FileDict[path];
            t.Item2.SetFileAttributes(t.Item1.ToU8Span(), attributes);

            return Result.Success;
        }
    }
}
