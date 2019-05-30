using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using LibHac.Fs;
using LibHac.Fs.Save;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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

                    HACGUIKeyset.AccountsFolderInfo.Create(); // make sure folder exists

                    IDirectory avatorsDirectory = accountSaveFilesystem.OpenDirectory("/su/avators/", OpenDirectoryMode.Files);

                    IEnumerable<DirectoryEntry> files = avatorsDirectory.Read();

                    DirectoryEntry profileEntry = files.FirstOrDefault(e => e.Name == "profiles.dat");
                    if(profileEntry != null)
                    {
                        if(profileEntry.Size == 0x650)
                        {
                            IFile profileFile = accountSaveFilesystem.OpenFile(profileEntry.FullPath, OpenMode.Read);
                            Stream profileData = profileFile.AsStream();
                            profileData.Position += 0x10; // skip header
                            for(int i = 0; i < 8; i++)
                            {
                                byte[] data = new byte[0xC8];
                                profileData.Read(data, 0, data.Length);

                                byte[] uidBytes = new byte[0x10];
                                byte[] nameBytes = new byte[32];

                                Array.Copy(data, uidBytes, uidBytes.Length);
                                Array.Copy(data, 0x28, nameBytes, 0, nameBytes.Length);

                                char[] nameChars = Encoding.UTF8.GetChars(nameBytes);
                                int length = Array.IndexOf(nameChars, '\0');
                                string name = new string(nameChars.Take(length).ToArray());

                                Guid uid = Guid.Parse(uidBytes.ToHexString()); // ignores endianness, which is what i want
                                if(!string.IsNullOrEmpty(name))
                                    Preferences.Current.UserIds[uid.ToString()] = name;
                            }
                            Preferences.Current.Write();
                        }
                        else
                        {
                            MessageBox.Show("Invalid profiles.dat size! Something seems to be corrupt...");
                        }
                    }

                    foreach (DirectoryEntry entry in files.Where(e => e.Name != "profiles.dat"))
                    {
                        FileInfo localFile = HACGUIKeyset.AccountsFolderInfo.GetFile(entry.Name);
                        IFile saveFile = accountSaveFilesystem.OpenFile(entry.FullPath, OpenMode.Read);
                        using (Stream localStream = localFile.Open(FileMode.Create))
                            saveFile.AsStorage().CopyToStream(localStream, saveFile.GetSize());
                    }
                }
            });
        }
    }
}
