using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.Save;
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
                    system.OpenFile(out IFile accountSaveFile, accountSaveFileName.ToU8Span(), OpenMode.Read);
                    SaveDataFileSystem accountSaveFilesystem = new SaveDataFileSystem(HACGUIKeyset.Keyset, accountSaveFile.AsStorage(), IntegrityCheckLevel.ErrorOnInvalid, false);

                    HACGUIKeyset.AccountsFolderInfo.Create(); // make sure folder exists

                    accountSaveFilesystem.OpenDirectory(out IDirectory avatorsDirectory, "/su/avators/".ToU8Span(), OpenDirectoryMode.File);

                    DirectoryEntry[] files = new DirectoryEntry[0x100];

                    avatorsDirectory.Read(out long entriesLength, files.AsSpan());

                    if(accountSaveFilesystem.FileExists("/su/avators/profiles.dat"))
                    {
                        DirectoryEntry profileEntry = files.First(e => StringUtils.Utf8ZToString(e.Name) == "profiles.dat");

                        if (profileEntry.Size == 0x650)
                        {
                            accountSaveFilesystem.OpenFile(out IFile profileFile, "/su/avators/profiles.dat".ToU8Span(), OpenMode.Read);
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

                    foreach (DirectoryEntry entry in files.Where(e => StringUtils.Utf8ZToString(e.Name) != "profiles.dat" && e.Type == DirectoryEntryType.File))
                    {
                        FileInfo localFile = HACGUIKeyset.AccountsFolderInfo.GetFile(StringUtils.Utf8ZToString(entry.Name));
                        accountSaveFilesystem.OpenFile(out IFile saveFile, ("/su/avators/" + StringUtils.Utf8ZToString(entry.Name)).ToU8Span(), OpenMode.Read);
                        using (Stream localStream = localFile.Open(FileMode.Create)) {
                            saveFile.GetSize(out long size);
                            saveFile.AsStorage().CopyToStream(localStream, size);
                        }
                    }
                }
            });
        }
    }
}
