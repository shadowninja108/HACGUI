using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.Nand;
using LibHac.Savefile;
using LibHac.Streams;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Navigation;
using static HACGUI.Extensions.Extensions;
using static LibHac.Nso;

namespace HACGUI.FirstStart
{
    /// <summary>
    /// Interaction logic for PickNANDPage.xaml
    /// </summary>
    public partial class PickNANDPage : PageExtension
    {
        public PickNANDPage() : base()
        {
            InitializeComponent();

            Loaded += (_, __) =>
            {
                NANDService.OnNANDPluggedIn += () =>
                {
                    StartDeriving();
                };

                NANDService.Start();
            };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog
            {
                FileName = "rawnand.bin",
                DefaultExt = ".bin",
                Filter = "Raw NAND dump (.bin or .bin.*)|*.bin*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            { // it's nullable so i HAVE to compare it to true
                string[] files = dlg.FileNames;
                if (files != null && files.Length > 0)
                {
                    IList<Stream> streams = new List<Stream>();
                    foreach (string file in files)
                        streams.Add(new FileInfo(file).OpenRead()); // Change to Open when write support is added
                    Stream NANDSource = new CombinationStream(streams);

                    if(!NANDService.InsertNAND(NANDSource, false))
                    {
                        MessageBox.Show("Invalid NAND dump!");
                    }

                }
            }
        }

        private void StartDeriving()
        {
            Dispatcher.BeginInvoke(new Action(() => // move to UI thread
            {
                NavigationWindow root = FindRoot();
                root.Navigate(new DerivingPage((page) =>
                {
                    OnNandFound();

                    PageExtension next = null;
                    // move to next page (after the task is complete)
                    page.Dispatcher.BeginInvoke(new Action(() => // move to UI thread again...
                    {
                        next = new Finish();
                        page.FindRoot().Navigate(next);
                    })).Wait(); // must wait, otherwise a race condition may occur

                    return next;
                }));
                KeepAlive = false;
            })).Wait();
            
            
        }

        private void OnNandFound()
        {
            Nand nand = NANDService.NAND;
            Stream NANDSource = NANDService.NANDSource;

            NANDSource.Seek(0x804000, SeekOrigin.Begin); // BCPKG2-1-Normal-Main offset + length of BootConfig
            FileStream pkg2stream = HACGUIKeyset.TempPkg2FileInfo.Create();
            NANDSource.CopyToNew(pkg2stream, 0x7FC000); // rest of BCPPKG2-Normal-Main partition
            pkg2stream.Seek(0, SeekOrigin.Begin);
            byte[] pkg2raw = new byte[pkg2stream.Length];
            pkg2stream.Read(pkg2raw, 0, pkg2raw.Length);

            Package2 pkg2 = new Package2(HACGUIKeyset.Keyset, new MemoryStream(pkg2raw));
            HACGUIKeyset.RootTempPkg2FolderInfo.Create();
            FileStream kernelstream = HACGUIKeyset.TempKernelFileInfo.Create();
            FileStream INI1stream = HACGUIKeyset.TempINI1FileInfo.Create();
            pkg2.OpenKernel().CopyTo(kernelstream);
            pkg2.OpenIni1().CopyTo(INI1stream);
            kernelstream.Close();
            INI1stream.Close();

            Ini1 INI1 = new Ini1(pkg2.OpenIni1());
            List<HashSearchEntry> hashes = new List<HashSearchEntry>();
            Dictionary<byte[], byte[]> keys = new Dictionary<byte[], byte[]>();
            HACGUIKeyset.RootTempINI1Folder.Create();
            foreach (Kip kip in INI1.Kips)
            {
                Stream rodatastream, datastream;
                switch (kip.Header.Name)
                {
                    case "FS":
                        hashes.Add(new HashSearchEntry(NintendoKeys.KeyAreaKeyApplicationSourceHash, 0x10));
                        hashes.Add(new HashSearchEntry(NintendoKeys.KeyAreaKeyOceanSourceHash, 0x10));
                        hashes.Add(new HashSearchEntry(NintendoKeys.KeyAreaKeySystemSourceHash, 0x10));
                        hashes.Add(new HashSearchEntry(NintendoKeys.HeaderKekSourceHash, 0x10));
                        hashes.Add(new HashSearchEntry(NintendoKeys.SaveMacKekSourceHash, 0x10));
                        hashes.Add(new HashSearchEntry(NintendoKeys.SaveMacKeySourceHash, 0x10));

                        rodatastream = new MemoryStream(kip.DecompressSection(1));
                        keys = rodatastream.FindKeyViaHash(hashes, new SHA256Managed(), 0x10);
                        Array.Copy(keys[NintendoKeys.KeyAreaKeyApplicationSourceHash], HACGUIKeyset.Keyset.KeyAreaKeyApplicationSource, 0x10);
                        Array.Copy(keys[NintendoKeys.KeyAreaKeyOceanSourceHash], HACGUIKeyset.Keyset.KeyAreaKeyOceanSource, 0x10);
                        Array.Copy(keys[NintendoKeys.KeyAreaKeySystemSourceHash], HACGUIKeyset.Keyset.KeyAreaKeySystemSource, 0x10);
                        Array.Copy(keys[NintendoKeys.HeaderKekSourceHash], HACGUIKeyset.Keyset.HeaderKekSource, 0x10);
                        Array.Copy(keys[NintendoKeys.SaveMacKekSourceHash], HACGUIKeyset.Keyset.SaveMacKekSource, 0x10);
                        Array.Copy(keys[NintendoKeys.SaveMacKeySourceHash], HACGUIKeyset.Keyset.SaveMacKeySource, 0x10);

                        hashes.Clear();
                        rodatastream.Seek(0, SeekOrigin.Begin);

                        bool sdWarn = false;

                        hashes.Add(new HashSearchEntry(NintendoKeys.SDCardKekSourceHash, 0x10));
                        try
                        {
                            keys = rodatastream.FindKeyViaHash(hashes, new SHA256Managed(), 0x10);
                            Array.Copy(keys[NintendoKeys.SDCardKekSourceHash], HACGUIKeyset.Keyset.SdCardKekSource, 0x10);
                        }
                        catch (EndOfStreamException)
                        {
                            MessageBox.Show("Failed to find SD card kek source! The NAND is probably from 1.0.0.");
                            sdWarn = true;
                        }

                        if (!sdWarn) // don't try to find the rest of the keys if the other one couldn't be found
                        {
                            hashes.Clear();
                            rodatastream.Seek(0, SeekOrigin.Begin);
                            hashes.Add(new HashSearchEntry(NintendoKeys.SDCardSaveKeySourceHash, 0x20));
                            hashes.Add(new HashSearchEntry(NintendoKeys.SDCardNcaKeySourceHash, 0x20));
                            keys = rodatastream.FindKeyViaHash(hashes, new SHA256Managed(), 0x20);
                            Array.Copy(keys[NintendoKeys.SDCardSaveKeySourceHash], HACGUIKeyset.Keyset.SdCardKeySources[0], 0x20);
                            Array.Copy(keys[NintendoKeys.SDCardNcaKeySourceHash], HACGUIKeyset.Keyset.SdCardKeySources[1], 0x20);
                        }

                        hashes.Clear();
                        rodatastream.Close();

                        hashes.Add(new HashSearchEntry(NintendoKeys.HeaderKeySourceHash, 0x20));
                        datastream = new MemoryStream(kip.DecompressSection(2));
                        keys = datastream.FindKeyViaHash(hashes, new SHA256Managed(), 0x20);
                        Array.Copy(keys[NintendoKeys.HeaderKeySourceHash], HACGUIKeyset.Keyset.HeaderKeySource, 0x20);

                        datastream.Close();
                        hashes.Clear();

                        break;
                    case "spl":
                        hashes.Add(new HashSearchEntry(NintendoKeys.AesKeyGenerationSourceHash, 0x10));

                        rodatastream = new MemoryStream(kip.DecompressSection(1));
                        keys = rodatastream.FindKeyViaHash(hashes, new SHA256Managed(), 0x10);
                        Array.Copy(keys[NintendoKeys.AesKeyGenerationSourceHash], HACGUIKeyset.Keyset.AesKeyGenerationSource, 0x10);

                        rodatastream.Close();
                        hashes.Clear();
                        break;
                }

                FileStream kipstream = HACGUIKeyset.RootTempINI1Folder.GetFile(kip.Header.Name + ".kip").Create();
                kip.OpenRawFile().CopyTo(kipstream);
                kipstream.Close();
            }

            pkg2stream.Close();
            INI1stream.Close();

            HACGUIKeyset.Keyset.DeriveKeys();

            SwitchFs fs = new SwitchFs(HACGUIKeyset.Keyset, NANDService.NAND.OpenSystemPartition());

            foreach (KeyValuePair<string, Nca> kv in fs.Ncas)
            {
                Nca nca = kv.Value;

                switch (nca.Header.TitleId)
                {
                    case 0x0100000000000033: // es
                        switch (nca.Header.ContentType)
                        {
                            case ContentType.Program:
                                NcaSection exefsSection = nca.Sections.FirstOrDefault(x => x?.Type == SectionType.Pfs0);
                                Stream pfsStream = nca.OpenSection(exefsSection.SectionNum, false, false);
                                Pfs pfs = new Pfs(pfsStream);
                                Nso nso = new Nso(pfs.OpenFile("main"));
                                NsoSection section = nso.Sections[1];
                                Stream data = section.OpenDecompressedStream();
                                hashes.Clear();

                                hashes.Add(new HashSearchEntry(NintendoKeys.EticketRsaKekSourceHash, 0x10));
                                hashes.Add(new HashSearchEntry(NintendoKeys.EticketRsaKekekSourceHash, 0x10));
                                keys = data.FindKeyViaHash(hashes, new SHA256Managed(), 0x10, data.Length);
                                byte[] EticketRsaKekSource = new byte[0x10];
                                byte[] EticketRsaKekekSource = new byte[0x10];
                                Array.Copy(keys[NintendoKeys.EticketRsaKekSourceHash], EticketRsaKekSource, 0x10);
                                Array.Copy(keys[NintendoKeys.EticketRsaKekekSourceHash], EticketRsaKekekSource, 0x10);

                                byte[] RsaOaepKekGenerationSource;
                                XOR(NintendoKeys.KekMasks[0], NintendoKeys.KekSeeds[3], out RsaOaepKekGenerationSource);

                                byte[] key1 = new byte[0x10];
                                Crypto.DecryptEcb(HACGUIKeyset.Keyset.MasterKeys[0], RsaOaepKekGenerationSource, key1, 0x10);
                                byte[] key2 = new byte[0x10];
                                Crypto.DecryptEcb(key1, EticketRsaKekekSource, key2, 0x10);
                                byte[] key3 = new byte[0x10];
                                Crypto.DecryptEcb(key2, EticketRsaKekSource, HACGUIKeyset.Keyset.EticketRsaKek, 0x10);
                                break;
                        }
                        break;
                }
            }

            Stream prodinfo = nand.OpenProdInfo();
            Stream prodinfoFile = HACGUIKeyset.TempPRODINFOFileInfo.Create();
            prodinfo.CopyTo(prodinfoFile);
            prodinfo.Close();
            prodinfoFile.Seek(0, SeekOrigin.Begin);
            Calibration cal0 = new Calibration(prodinfoFile);
            HACGUIKeyset.Keyset.EticketExtKeyRsa = Crypto.DecryptRsaKey(cal0.EticketExtKeyRsa, HACGUIKeyset.Keyset.EticketRsaKek);
            prodinfoFile.Close();

            List<Ticket> tickets = new List<Ticket>();
            NandPartition system = nand.OpenSystemPartition();

            Stream e1Stream = system.OpenFile("save\\80000000000000E1", FileMode.Open, FileAccess.Read);
            tickets.AddRange(ReadTickets(HACGUIKeyset.Keyset, e1Stream));

            Stream e2Stream = system.OpenFile("save\\80000000000000E2", FileMode.Open, FileAccess.Read);
            tickets.AddRange(ReadTickets(HACGUIKeyset.Keyset, e2Stream));

            Stream nsAppmanStream = system.OpenFile("save\\8000000000000043", FileMode.Open, FileAccess.Read);
            Savefile save = new Savefile(HACGUIKeyset.Keyset, nsAppmanStream, false);
            Stream privateStream = save.OpenFile("/private");
            byte[] sdSeed = new byte[0x10];
            privateStream.Read(sdSeed, 0, 0x10); // Seek doesn't work so i just read twice
            privateStream.Read(sdSeed, 0, 0x10);
            HACGUIKeyset.Keyset.SetSdSeed(sdSeed);

            foreach (Ticket ticket in tickets)
            {
                HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId] = new byte[0x10];
                Array.Copy(ticket.TitleKeyBlock, HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId], 0x10);
            }
            NANDService.Stop();

            HACGUIKeyset.ProductionKeysFileInfo.Create().WriteString(HACGUIKeyset.PrintCommonKeys(HACGUIKeyset.Keyset, true));
            HACGUIKeyset.ExtraKeysFileInfo.Create().WriteString(HACGUIKeyset.PrintCommonWithoutFriendlyKeys(HACGUIKeyset.Keyset));
            HACGUIKeyset.ConsoleKeysFileInfo.Create().WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
            HACGUIKeyset.GetConsoleKeysFileInfoByName(PickConsole.ConsoleName).Create().WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
            HACGUIKeyset.TitleKeysFileInfo.Create().WriteString(ExternalKeys.PrintTitleKeys(HACGUIKeyset.Keyset));
        }

        public override void OnBack()
        {
            NANDService.Stop();
        }

        private static List<Ticket> ReadTickets(Keyset keyset, Stream savefile)
        {
            var tickets = new List<Ticket>();
            var save = new Savefile(keyset, savefile, false);
            var ticketList = new BinaryReader(save.OpenFile("/ticket_list.bin"));
            var ticketFile = new BinaryReader(save.OpenFile("/ticket.bin"));

            var titleId = ticketList.ReadUInt64();
            while (titleId != ulong.MaxValue)
            {
                ticketList.BaseStream.Position += 0x18;
                var start = ticketFile.BaseStream.Position;
                tickets.Add(new Ticket(ticketFile));
                ticketFile.BaseStream.Position = start + 0x400;
                titleId = ticketList.ReadUInt64();
            }

            return tickets;
        }

        public void XOR(byte[] buffer1, byte[] buffer2, out byte[] output)
        {
            if (buffer1.Length != buffer2.Length)
                throw new InvalidDataException("XOR buffer size must match!");
            output = new byte[buffer1.Length];
            for (int i = 0; i < buffer1.Length; i++)
                output[i] = (byte)(buffer1[i] ^ buffer2[i]);
        }
    }
}
