using HACGUI.Services;
using LibHac.IO;
using LibHac.IO.Save;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class CopyAccountDataTask : ProgressTask
    {
        public CopyAccountDataTask() : base("Copy account data...")
        {

        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                FatFileSystemProvider system =  NANDService.NAND.OpenSystemPartition();
                IFile accountSaveFile = system.OpenFile("/save/8000000000000010", OpenMode.Read);
                SaveDataFileSystem accountSaveFilesystem = new SaveDataFileSystem(HACGUIKeyset.Keyset, accountSaveFile.AsStorage(), IntegrityCheckLevel.ErrorOnInvalid, false);

            });
        }
    }
}
