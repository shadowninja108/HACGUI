using LibHac;
using LibHac.Common;
using LibHac.Fs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HACGUI.Utilities
{
    public class PseudoRootDirectory : IDirectory
    {
        public IFileSystem ParentFileSystem { get; set; }

        public string FullPath => "/";

        public OpenDirectoryMode Mode => OpenDirectoryMode.File;
        private int CurrentIndex { get; set; }

        public PseudoRootDirectory(PseudoFileSystem fs)
        {
            ParentFileSystem = fs;
        }

        public int GetEntryCount()
        {
            return (ParentFileSystem as PseudoFileSystem).FileDict.Count;
        }

        public Result Read(out long entryCount, Span<DirectoryEntry> entries)
        {
            int i = 0;
            foreach(KeyValuePair<string, Tuple<string, IAttributeFileSystem>> kv in (ParentFileSystem as PseudoFileSystem).FileDict.Skip(CurrentIndex))
            {
                if (entries.Length <= i)
                    break;

                IAttributeFileSystem fs = kv.Value.Item2;
                string fullPath = kv.Key;
                int split = fullPath.LastIndexOf('/');
                string path = fullPath.Substring(0, split);
                fs.GetFileSize(out long size, kv.Value.Item1.ToU8Span());

                StringUtils.Copy(entries[i].Name, Encoding.UTF8.GetBytes(kv.Key.Substring(split)).AsSpan());
                entries[i].Size = size;
                entries[i].Attributes = NxFileAttributes.None;
                entries[i].Type = DirectoryEntryType.File;

                i++;
            }

            foreach(KeyValuePair<string, IStorage> kv in (ParentFileSystem as PseudoFileSystem).StorageDict.Skip(CurrentIndex - (ParentFileSystem as PseudoFileSystem).FileDict.Count))
            {
                if (entries.Length <= i)
                    break;

                string fullPath = kv.Key;
                int split = fullPath.LastIndexOf('/');
                kv.Value.GetSize(out long size);

                StringUtils.Copy(entries[i].Name, Encoding.UTF8.GetBytes(kv.Key.Substring(split)).AsSpan());
                entries[i].Size = size;
                entries[i].Attributes = NxFileAttributes.None;
                entries[i].Type = DirectoryEntryType.File;

                i++;
            }
            entryCount = i;
            CurrentIndex += (int)entryCount;

            return Result.Success;
        }

        public Result GetEntryCount(out long entryCount)
        {
            entryCount = (ParentFileSystem as PseudoFileSystem).FileDict.Count + (ParentFileSystem as PseudoFileSystem).StorageDict.Count;
            return Result.Success;
        }
    }
}
