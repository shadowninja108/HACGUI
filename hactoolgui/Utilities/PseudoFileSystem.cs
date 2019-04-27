using LibHac.IO;
using System;
using System.Collections.Generic;

namespace HACGUI.Utilities
{
    public class PseudoFileSystem : IAttributeFileSystem
    {
        public Dictionary<string, Tuple<string, IAttributeFileSystem>> FileDict;

        public PseudoFileSystem()
        {
            FileDict = new Dictionary<string, Tuple<string, IAttributeFileSystem>>();
        }

        public void Add(string path, string internalPath, IAttributeFileSystem fs)
        {
            FileDict[path] = new Tuple<string, IAttributeFileSystem>(internalPath, fs);
            /*PathParser parser = new PathParser(Encoding.ASCII.GetBytes(path));
            while (!parser.IsFinished())
            {
                if (parser.TryGetNext(out ReadOnlySpan<byte> pb))
                {
                    string p = new string(Encoding.ASCII.GetChars(pb.ToArray()));
                    ;
                }
            }*/
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
            return false;
        }

        public bool FileExists(string path)
        {
            return FileDict.ContainsKey(path);
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
            Tuple<string, IAttributeFileSystem> t = FileDict[path];
            return t.Item2.GetFileSize(t.Item1);
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
            Tuple<string, IAttributeFileSystem> t = FileDict[path];
            return t.Item2.OpenFile(t.Item1, mode);
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
