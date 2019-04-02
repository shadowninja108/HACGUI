using LibHac.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Services
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
            (ParentFileSystem as PseudoFileSystem).FileDict.;
        }

        public IEnumerable<DirectoryEntry> Read()
        {
            throw new NotImplementedException();
        }
    }
}
