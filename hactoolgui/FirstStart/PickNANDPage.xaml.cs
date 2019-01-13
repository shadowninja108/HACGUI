using HACGUI.Extensions;
using HACGUI.Services;
using LibHac;
using LibHac.Nand;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Navigation;
using static HACGUI.Extensions.Extensions;
using static LibHac.Nso;
using static CertNX.RSAUtils;
using CertNX;
using HACGUI.Utilities;
using LibHac.IO;
using LibHac.IO.Save;
using System.Security.Principal;
using System.Diagnostics;

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
                if (IsAdministrator)
                {
                    MemloaderDescriptionLabel.Content = "HACGUI is waiting for a Switch with\nmemloader setup to be plugged in.";
                    MemloaderDescriptionLabel.VerticalAlignment = VerticalAlignment.Center;
                    MemloaderDescriptionLabel.Margin = new Thickness();
                    RestartAsAdminButton.Visibility = Visibility.Hidden;
                }

                NANDService.OnNANDPluggedIn += () =>
                {
                    StartDeriving();
                };

                NANDService.Start();
            };
        }

        private void PickNANDButtonClick(object sender, RoutedEventArgs e)
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
                    IList<IStorage> streams = new List<IStorage>();
                    foreach (string file in files)
                        streams.Add(new FileInfo(file).OpenRead().AsStorage()); // Change to Open when write support is added
                    IStorage NANDSource = new ConcatenationStorage(streams, true);

                    if (!NANDService.InsertNAND(NANDSource, false))
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
                        next = new FinishPage();
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

            FileStream pkg2file = HACGUIKeyset.TempPkg2FileInfo.Create();
            IStorage pkg2nand = nand.OpenPackage2(0);
            byte[] pkg2raw = new byte[0x7FC000];
            pkg2nand.Read(pkg2raw, 0x4000);
            MemoryStorage pkg2memory = new MemoryStorage(pkg2raw);
            pkg2memory.CopyToStream(pkg2file);

            Package2 pkg2 = new Package2(HACGUIKeyset.Keyset, pkg2memory);
            HACGUIKeyset.RootTempPkg2FolderInfo.Create();
            FileStream kernelstream = HACGUIKeyset.TempKernelFileInfo.Create();
            FileStream INI1stream = HACGUIKeyset.TempINI1FileInfo.Create();
            pkg2.OpenKernel().CopyToStream(kernelstream);
            pkg2.OpenIni1().CopyToStream(INI1stream);
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
                kip.OpenRawFile().CopyToStream(kipstream);
                kipstream.Close();
            }

            pkg2file.Close();
            INI1stream.Close();

            HACGUIKeyset.Keyset.DeriveKeys();

            SwitchFs fs = new SwitchFs(HACGUIKeyset.Keyset, NANDService.NAND.OpenSystemPartition());

            foreach (KeyValuePair<string, Nca> kv in fs.Ncas)
            {
                Nca nca = kv.Value;

                if(nca.CanOpenSection(0)) // mainly a check if the NCA can be decrypted
                    switch (nca.Header.TitleId)
                    {
                        case 0x0100000000000033: // es
                            switch (nca.Header.ContentType)
                            {
                                case ContentType.Program:
                                    NcaSection exefsSection = nca.Sections.FirstOrDefault(x => x?.Type == SectionType.Pfs0);
                                    IStorage pfsStorage = nca.OpenSection(exefsSection.SectionNum, false, IntegrityCheckLevel.ErrorOnInvalid, false);
                                    Pfs pfs = new Pfs(pfsStorage);
                                    Nso nso = new Nso(pfs.OpenFile("main"));
                                    NsoSection section = nso.Sections[1];
                                    Stream data = new MemoryStream(section.DecompressSection());
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
                                    Crypto.DecryptEcb(key2, EticketRsaKekSource, HACGUIKeyset.Keyset.EticketRsaKek, 0x10);
                                    break;
                            }
                            break;
                        case 0x0100000000000024: // ssl
                            switch (nca.Header.ContentType)
                            {
                                case ContentType.Program:
                                    NcaSection exefsSection = nca.Sections.FirstOrDefault(x => x?.Type == SectionType.Pfs0);
                                    IStorage pfsStorage = nca.OpenSection(exefsSection.SectionNum, false, IntegrityCheckLevel.ErrorOnInvalid, false);
                                    Pfs pfs = new Pfs(pfsStorage);
                                    Nso nso = new Nso(pfs.OpenFile("main"));
                                    NsoSection section = nso.Sections[1];
                                    Stream data = new MemoryStream(section.DecompressSection());
                                    hashes.Clear();

                                    hashes.Add(new HashSearchEntry(NintendoKeys.SslAesKeyXHash, 0x10));
                                    hashes.Add(new HashSearchEntry(NintendoKeys.SslRsaKeyYHash, 0x10));
                                    keys = data.FindKeyViaHash(hashes, new SHA256Managed(), 0x10, data.Length);
                                    byte[] SslAesKeyX = new byte[0x10];
                                    byte[] SslRsaKeyY = new byte[0x10];
                                    Array.Copy(keys[NintendoKeys.SslAesKeyXHash], SslAesKeyX, 0x10);
                                    Array.Copy(keys[NintendoKeys.SslRsaKeyYHash], SslRsaKeyY, 0x10);

                                    byte[] RsaPrivateKekGenerationSource;
                                    XOR(NintendoKeys.KekMasks[0], NintendoKeys.KekSeeds[1], out RsaPrivateKekGenerationSource);

                                    byte[] key1 = new byte[0x10];
                                    Crypto.DecryptEcb(HACGUIKeyset.Keyset.MasterKeys[0], RsaPrivateKekGenerationSource, key1, 0x10);
                                    byte[] key2 = new byte[0x10];
                                    Crypto.DecryptEcb(key1, SslAesKeyX, key2, 0x10);
                                    byte[] key3 = new byte[0x10];
                                    Crypto.DecryptEcb(key2, SslRsaKeyY, HACGUIKeyset.Keyset.SslRsaKek, 0x10);
                                    break;
                            }
                            break;
                    }
            }

            // save PRODINFO to file, then derive eticket_ext_key_rsa
            Stream prodinfo = nand.OpenProdInfo();
            Stream prodinfoFile = HACGUIKeyset.TempPRODINFOFileInfo.Create();
            prodinfo.CopyTo(prodinfoFile);
            prodinfo.Close();
            prodinfoFile.Seek(0, SeekOrigin.Begin);
            Calibration cal0 = new Calibration(prodinfoFile);
            HACGUIKeyset.Keyset.EticketExtKeyRsa = Crypto.DecryptRsaKey(cal0.EticketExtKeyRsa, HACGUIKeyset.Keyset.EticketRsaKek);

            // get client certificate
            prodinfo.Seek(0x0AD0, SeekOrigin.Begin);
            byte[] buffer;
            buffer = new byte[0x4];
            prodinfo.Read(buffer, 0, buffer.Length); // read cert length
            uint certLength = BitConverter.ToUInt32(buffer, 0);
            buffer = new byte[certLength];
            prodinfo.Seek(0x0AE0, SeekOrigin.Begin); // should be redundant?
            prodinfo.Read(buffer, 0, buffer.Length); // read actual cert

            byte[] counter = cal0.SslExtKey.Take(0x10).ToArray();
            byte[] key = cal0.SslExtKey.Skip(0x10).ToArray(); // bit strange structure but it works

            new Aes128CtrTransform(HACGUIKeyset.Keyset.SslRsaKek, counter).TransformBlock(key); // decrypt private key

            X509Certificate2 certificate = new X509Certificate2();
            certificate.Import(buffer);
            certificate.ImportPrivateKey(key);

            byte[] pfx = certificate.Export(X509ContentType.Pkcs12, "switch");
            Stream pfxStream = HACGUIKeyset.GetClientCertificateByName(PickConsolePage.ConsoleName).Create();
            pfxStream.Write(pfx, 0, pfx.Length);
            pfxStream.Close();
            prodinfoFile.Close();

            // get tickets
            List<Ticket> tickets = new List<Ticket>();
            NandPartition system = nand.OpenSystemPartition();

            IFile e1File = system.GetFile("save\\80000000000000E1");
            if (e1File.Exists)
            {
                IStorage e1Storage = e1File.Open(FileMode.Open, FileAccess.Read);
                tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e1Storage));
            }

            IFile e2File = system.GetFile("save\\80000000000000E2");
            if (e2File.Exists) {
                IStorage e2Storage = e2File.Open(FileMode.Open, FileAccess.Read);
                tickets.AddRange(DumpTickets(HACGUIKeyset.Keyset, e2Storage));
            }

            IStorage nsAppmanStorage = system.GetFile("save\\8000000000000043").Open(FileMode.Open, FileAccess.Read);
            Savefile save = new Savefile(HACGUIKeyset.Keyset, nsAppmanStorage, IntegrityCheckLevel.ErrorOnInvalid, false);
            IStorage privateStorage = save.OpenFile("/private");
            byte[] sdSeed = new byte[0x10];
            privateStorage.Read(sdSeed, 0x10);
            HACGUIKeyset.Keyset.SetSdSeed(sdSeed);

            foreach (Ticket ticket in tickets)
            {
                HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId] = new byte[0x10];
                Array.Copy(ticket.GetTitleKey(HACGUIKeyset.Keyset), HACGUIKeyset.Keyset.TitleKeys[ticket.RightsId], 0x10);
            }

            NANDService.Stop();

            DirectoryInfo oldKeysDirectory = HACGUIKeyset.RootFolderInfo.GetDirectory("keys");
            if (oldKeysDirectory.Exists)
            {
                oldKeysDirectory.Delete(true); // fix old versions after restructure of directory
            }

            // write all keys to file
            Stream prodKeys = HACGUIKeyset.ProductionKeysFileInfo.Create();
            prodKeys.WriteString(HACGUIKeyset.PrintCommonKeys(HACGUIKeyset.Keyset, true));
            Stream extraKeys = HACGUIKeyset.ExtraKeysFileInfo.Create();
            extraKeys.WriteString(HACGUIKeyset.PrintCommonWithoutFriendlyKeys(HACGUIKeyset.Keyset));
            Stream consoleKeys = HACGUIKeyset.ConsoleKeysFileInfo.Create();
            consoleKeys.WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
            Stream specificConsoleKeys = HACGUIKeyset.GetConsoleKeysFileInfoByName(PickConsolePage.ConsoleName).Create();
            specificConsoleKeys.WriteString(ExternalKeys.PrintUniqueKeys(HACGUIKeyset.Keyset));
            Stream titleKeys = HACGUIKeyset.TitleKeysFileInfo.Create();
            titleKeys.WriteString(ExternalKeys.PrintTitleKeys(HACGUIKeyset.Keyset));
            prodKeys.Close();
            extraKeys.Close();
            consoleKeys.Close();
            specificConsoleKeys.Close();
            titleKeys.Close();

            Preferences.Current.DefaultConsoleName = PickConsolePage.ConsoleName;
            Preferences.Current.Write();
        }

        public override void OnBack()
        {
            NANDService.Stop();
        }

        private static List<Ticket> DumpTickets(Keyset keyset, IStorage savefile)
        {
            var tickets = new List<Ticket>();
            var save = new Savefile(keyset, savefile, IntegrityCheckLevel.ErrorOnInvalid, false);
            var ticketList = new BinaryReader(save.OpenFile("/ticket_list.bin").AsStream());
            var ticketFile = new BinaryReader(save.OpenFile("/ticket.bin").AsStream());
            DirectoryInfo ticketFolder = HACGUIKeyset.GetTicketsDirectory(PickConsolePage.ConsoleName);
            ticketFolder.Create();

            var titleId = ticketList.ReadUInt64();
            while (titleId != ulong.MaxValue)
            {
                ticketList.BaseStream.Position += 0x18;
                var start = ticketFile.BaseStream.Position;
                Ticket ticket = new Ticket(ticketFile);
                Stream ticketFileStream = ticketFolder.GetFile(BitConverter.ToString(ticket.RightsId).Replace("-", "").ToLower() + ".tik").Create();
                byte[] data = ticket.GetBytes();
                ticketFileStream.Write(data, 0, data.Length);
                ticketFileStream.Close();
                tickets.Add(ticket);
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

        private void RestartAsAdminButtonClick(object sender, RoutedEventArgs e)
        {
            Stream continueStream = HACGUIKeyset.TempContinueFileInfo.Create();
            StreamWriter writer = new StreamWriter(continueStream);
            writer.WriteLine(HACGUIKeyset.Keyset.SecureBootKey.ToHexString());
            writer.WriteLine(HACGUIKeyset.Keyset.TsecKey.ToHexString());
            writer.WriteLine(HACGUIKeyset.Keyset.TsecRootKey.Serialize().ToHexString()); // Serialize
            writer.WriteLine(HACGUIKeyset.Keyset.EncryptedKeyblobs.Serialize().ToHexString());
            writer.WriteLine(PickConsolePage.ConsoleName);
            writer.Close();

            Process proc = new Process();
            proc.StartInfo.FileName = AppDomain.CurrentDomain.FriendlyName;
            proc.StartInfo.UseShellExecute = true;
            proc.StartInfo.Verb = "runas";
            proc.StartInfo.Arguments = $"continue";
            try
            {
                proc.Start();
                System.Windows.Application.Current.Shutdown();
            } catch(System.ComponentModel.Win32Exception)
            {
                HACGUIKeyset.TempContinueFileInfo.Delete(); // prob cancelled by user, delete unused continue file
            }
        }

        public static bool IsAdministrator =>
            new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
    }
}
