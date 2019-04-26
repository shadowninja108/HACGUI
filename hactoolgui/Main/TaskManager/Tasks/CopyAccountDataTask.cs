using HACGUI.Services;
using HACGUI.Utilities;
using LibHac.IO;
using LibHac.IO.Save;
using LibHac.Nand;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static HACGUI.Extensions.Extensions;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class CopyAccountDataTask : ProgressTask
    {
        public CopyAccountDataTask() : base("Copying account data...")
        {

        }

        public override Task CreateTask()
        {
            return new Task(() => 
            {
                FatFileSystemProvider system =  NANDService.NAND.OpenSystemPartition();
                string accountSaveFileName = "/save/8000000000000010";
                if (system.FileExists(accountSaveFileName))
                {
                    IFile accountSaveFile = system.OpenFile(accountSaveFileName, OpenMode.Read);
                    SaveDataFileSystem accountSaveFilesystem = new SaveDataFileSystem(HACGUIKeyset.Keyset, accountSaveFile.AsStorage(), IntegrityCheckLevel.ErrorOnInvalid, false);
                    string profilesDatPath = "/su/avators/profiles.dat"; // yes Nintendo spelled it wrong
                    IFile profilesDatFile = accountSaveFilesystem.OpenFile(profilesDatPath, OpenMode.Read);

                    HACGUIKeyset.AccountsFolderInfo.Create(); // make sure folder exists

                    //TODO: parse account database here

                    IDirectory avatorsDirectory = accountSaveFilesystem.OpenDirectory("/su/avators/", OpenDirectoryMode.Files);
                    foreach (DirectoryEntry entry in avatorsDirectory.Read().Where(e => e.Name != "profiles.dat"))
                    {
                        FileInfo localFile = HACGUIKeyset.AccountsFolderInfo.GetFile(entry.Name);
                        IFile saveFile = accountSaveFilesystem.OpenFile(entry.FullPath, OpenMode.Read);
                        using (Stream localStream = localFile.Open(FileMode.Create))
                            saveFile.AsStorage().CopyToStream(localStream, saveFile.GetSize());
                    }

                    ;
                }
            });
        }
    }
}
