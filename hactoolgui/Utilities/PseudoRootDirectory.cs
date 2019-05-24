using LibHac.Fs;
using System;
using System.Collections.Generic;

namespace HACGUI.Utilities
{
    public class PseudoRootDirectory : IDirectory
    {
        public IFileSystem ParentFileSystem { get; set; }

        public string FullPath => "/";

        public OpenDirectoryMode Mode => OpenDirectoryMode.Files;

        public PseudoRootDirectory(PseudoFileSystem fs)
        {
            ParentFileSystem = fs;
        }

        public int GetEntryCount()
        {
            return (ParentFileSystem as PseudoFileSystem).FileDict.Count;
        }

        public IEnumerable<DirectoryEntry> Read()
        {
            foreach(KeyValuePair<string, Tuple<string, IAttributeFileSystem>> kv in (ParentFileSystem as PseudoFileSystem).FileDict)
            {
                IAttributeFileSystem fs = kv.Value.Item2;
                string fullPath = kv.Key;
                int split = fullPath.LastIndexOf('/');
                string path = fullPath.Substring(0, split);
                string name = kv.Key.Substring(split);
                yield return new DirectoryEntry(name, fullPath, DirectoryEntryType.File, fs.GetFileSize(kv.Value.Item1));
            }
        }
    }
}
