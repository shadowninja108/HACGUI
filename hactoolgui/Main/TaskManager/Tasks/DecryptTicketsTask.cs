using HACGUI.Services;
using LibHac;
using LibHac.IO;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HACGUI.Extensions;
using static HACGUI.Extensions.Extensions;
using HACGUI.Utilities;

namespace HACGUI.Main.TaskManager.Tasks
{
    public class DecryptTicketsTask : ProgressTask
    {
        public DecryptTicketsTask() : base("Decrypting new tickets...")
        {
            Indeterminate = true;
            Blocking = true;
        }

        public override Task CreateTask()
        {
            return new Task(() =>
            {
                TaskManagerPage.Current.Queue.Submit(new SaveKeysetTask(Preferences.Current.DefaultConsoleName)); // TODO

                Calibration cal0 = new Calibration(NANDService.NAND.OpenProdInfo());
                HACGUIKeyset.Keyset.EticketExtKeyRsa = Crypto.DecryptRsaKey(cal0.EticketExtKeyRsa, HACGUIKeyset.Keyset.EticketRsaKek);

                List<Ticket> tickets = new List<Ticket>();
                FatFileSystemProvider system = NANDService.NAND.OpenSystemPartition();
                const string e1FileName = "save\\80000000000000E1";
                const string e2FileName = "save\\80000000000000E2";

                if (system.FileExists(e1FileName))
                {
                    IFile e1File = system.OpenFile(e1FileName, OpenMode.Read);
                    IStorage e1Storage = new FileStorage(e1File);
                    tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e1Storage, Preferences.Current.DefaultConsoleName));
                }

                if (system.FileExists(e2FileName))
                {
                    IFile e2File = system.OpenFile(e2FileName, OpenMode.Read);
                    IStorage e2Storage = new FileStorage(e2File);
                    tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e2Storage, Preferences.Current.DefaultConsoleName));
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
