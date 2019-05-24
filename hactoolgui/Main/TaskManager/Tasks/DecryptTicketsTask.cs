using HACGUI.Services;
using LibHac;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using HACGUI.Utilities;
using static HACGUI.Extensions.Extensions;
using LibHac.Fs;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class DecryptTicketsTask : ProgressTask
    {
        private string ConsoleName;

        public DecryptTicketsTask(string consoleName) : base("Decrypting new tickets...")
        {
            Indeterminate = true;
            Blocking = true;
            ConsoleName = consoleName;
        }

        public override Task CreateTask()
        {
            return new Task(() =>
            {
                using (Stream stream = NANDService.NAND.OpenProdInfo())
                {
                    Calibration cal0 = new Calibration(stream);
                    HACGUIKeyset.Keyset.EticketExtKeyRsa = Crypto.DecryptRsaKey(cal0.EticketExtKeyRsa, HACGUIKeyset.Keyset.EticketRsaKek);
                }

                List<Ticket> tickets = new List<Ticket>();
                FatFileSystemProvider system = NANDService.NAND.OpenSystemPartition();
                const string e1FileName = "save\\80000000000000E1";
                const string e2FileName = "save\\80000000000000E2";

                if (system.FileExists(e1FileName))
                {
                    IFile e1File = system.OpenFile(e1FileName, OpenMode.Read);
                    IStorage e1Storage = new FileStorage(e1File);
                    tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e1Storage, ConsoleName));
                }

                if (system.FileExists(e2FileName))
                {
                    IFile e2File = system.OpenFile(e2FileName, OpenMode.Read);
                    IStorage e2Storage = new FileStorage(e2File);
                    tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e2Storage, ConsoleName));
                }

                foreach (Ticket ticket in tickets)
                {
                    HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId] = new byte[0x10];
                    Array.Copy(ticket.GetTitleKey(HACGUIKeyset.Keyset), HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId], 0x10);
                }
            });
        }
    }
}
