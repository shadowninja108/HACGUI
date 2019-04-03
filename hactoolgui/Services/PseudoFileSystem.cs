using LibHac.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Services
{
    public class PseudoFileSystem : IAttributeFileSystem
    {
        public Dictionary<string, IAttributeFileSystem> FileDict;

        public PseudoFileSystem()
        {
            FileDict = new Dictionary<string, IAttributeFileSystem>();
        }

        public void Add(string path, IAttributeFileSystem fs)
        {
            FileDict[path] = fs;
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
            return FileDict[path].GetFileSize(path);
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
            return FileDict[path].OpenFile(path, mode);
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
            FileDict[path].SetFileAttributes(path, attributes);
        }
    }
}
