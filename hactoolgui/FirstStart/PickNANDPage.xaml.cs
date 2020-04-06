using CertNX;
using HACGUI.Extensions;
using HACGUI.Main.TaskManager.Tasks;
using HACGUI.Services;
using HACGUI.Utilities;
using LibHac;
using LibHac.Common;
using LibHac.Crypto;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using LibHac.FsSystem.Save;
using LibHac.Nand;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Navigation;
using static CertNX.RSAUtils;
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
                if (Native.IsAdministrator)
                {
                    MemloaderDescriptionLabel.Text = "HACGUI is waiting for a Switch in RCM to be plugged in.";

                    RestartAsAdminButton.Content = "Inject for me";
                    RestartAsAdminButton.IsEnabled = InjectService.LibusbKInstalled;
                    InjectService.DeviceInserted += () => 
                    {
                        if(InjectService.LibusbKInstalled)
                            Dispatcher.Invoke(() => RestartAsAdminButton.IsEnabled = true);
                    };

                    InjectService.DeviceRemoved += () =>
                    {
                        Dispatcher.Invoke(() => RestartAsAdminButton.IsEnabled = false);
                    };
                }

                NANDService.OnNANDPluggedIn += () =>
                {
                    InjectService.Stop();
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
                NavigationWindow root = FindNavigationWindow();
                root.Navigate(new DerivingPage((page) =>
                {
                    OnNandFound();

                    PageExtension next = null;
                    // move to next page (after the task is complete)
                    page.Dispatcher.BeginInvoke(new Action(() => // move to UI thread again...
                    {
                        next = new FinishPage();
                        page.FindNavigationWindow().Navigate(next);
                    })).Wait(); // must wait, otherwise a race condition may occur

                    return next;
                }));
                KeepAlive = false;
            })).Wait();


        }

        private void OnNandFound()
        {
            Nand nand = NANDService.NAND;

            // stream package2 to memory
            IStorage pkg2nand = nand.OpenPackage2(0); // 0 -> BCPKG2-1-Normal-Main
            byte[] pkg2raw = new byte[0x7FC000]; // maximum size of pkg2
            pkg2nand.Read(0x4000, pkg2raw);

            MemoryStorage pkg2memory = new MemoryStorage(pkg2raw);

            HACGUIKeyset.RootTempFolderInfo.Create();

            // copy to file for end user
            using (FileStream pkg2file = HACGUIKeyset.TempPkg2FileInfo.Create())
                pkg2memory.CopyToStream(pkg2file);

            Package2 pkg2 = new Package2(HACGUIKeyset.Keyset, pkg2memory);
            HACGUIKeyset.RootTempPkg2FolderInfo.Create(); // make sure it exists
            using (FileStream kernelstream = HACGUIKeyset.TempKernelFileInfo.Create())
                pkg2.OpenKernel().CopyToStream(kernelstream);
            using (FileStream INI1stream = HACGUIKeyset.TempINI1FileInfo.Create())
                pkg2.OpenIni1().CopyToStream(INI1stream);

            Ini1 INI1 = new Ini1(pkg2.OpenIni1());
            List<HashSearchEntry> hashes = new List<HashSearchEntry>();
            HACGUIKeyset.RootTempINI1FolderInfo.Create();
            foreach (Kip kip in INI1.Kips)
            {
                using (Stream rodatastream = new MemoryStream(kip.DecompressSection(1)))
                    switch (kip.Header.Name)
                    {
                        case "FS":
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.KeyAreaKeyApplicationSourceHash,
                                () => HACGUIKeyset.Keyset.KeyAreaKeyApplicationSource,
                                0x10));
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.KeyAreaKeyOceanSourceHash,
                                () => HACGUIKeyset.Keyset.KeyAreaKeyOceanSource,
                                0x10));
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.KeyAreaKeySystemSourceHash,
                                () => HACGUIKeyset.Keyset.KeyAreaKeySystemSource,
                                0x10));
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.HeaderKekSourceHash,
                                () => HACGUIKeyset.Keyset.HeaderKekSource,
                                0x10));
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.SaveMacKekSourceHash,
                                () => HACGUIKeyset.Keyset.SaveMacKekSource,
                                0x10));
                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.SaveMacKeySourceHash,
                                () => HACGUIKeyset.Keyset.SaveMacKeySource,
                                0x10));

                            rodatastream.FindKeysViaHash(hashes, new SHA256Managed(), 0x10);

                            hashes.Clear();
                            rodatastream.Seek(0, SeekOrigin.Begin);

                            bool sdWarn = false;

                            hashes.Add(new HashSearchEntry(NintendoKeys.SDCardKekSourceHash, () => HACGUIKeyset.Keyset.SdCardKekSource,
                                0x10));
                            try
                            {
                                rodatastream.FindKeysViaHash(hashes, new SHA256Managed(), 0x10);
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
                                hashes.Add(new HashSearchEntry(
                                    NintendoKeys.SDCardSaveKeySourceHash,
                                    () => HACGUIKeyset.Keyset.SdCardKeySources[0],
                                    0x20));
                                hashes.Add(new HashSearchEntry(
                                    NintendoKeys.SDCardNcaKeySourceHash,
                                    () => HACGUIKeyset.Keyset.SdCardKeySources[1],
                                    0x20));
                                rodatastream.FindKeysViaHash(hashes, new SHA256Managed(), 0x20);
                            }

                            hashes.Clear();

                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.HeaderKeySourceHash, 
                                () => HACGUIKeyset.Keyset.HeaderKeySource,
                                0x20));

                            using (Stream datastream = new MemoryStream(kip.DecompressSection(2)))
                                datastream.FindKeysViaHash(hashes, new SHA256Managed(), 0x20);

                            hashes.Clear();

                            break;
                        case "spl":
                            hashes.Clear();

                            hashes.Add(new HashSearchEntry(
                                NintendoKeys.AesKeyGenerationSourceHash, 
                                () => HACGUIKeyset.Keyset.AesKeyGenerationSource,
                                0x10));

                            rodatastream.FindKeysViaHash(hashes, new SHA256Managed(), 0x10);
                            break;
                    }

                using (FileStream kipstream = HACGUIKeyset.RootTempINI1FolderInfo.GetFile(kip.Header.Name + ".kip").Create())
                    kip.OpenRawFile().CopyToStream(kipstream);
            }

            HACGUIKeyset.Keyset.DeriveKeys();

            SwitchFs fs = SwitchFs.OpenNandPartition(HACGUIKeyset.Keyset, nand.OpenSystemPartition());

            NintendoKeys.KekSeeds[1].XOR(NintendoKeys.KekMasks[0], out byte[] RsaPrivateKekGenerationSource);

            NintendoKeys.KekSeeds[3].XOR(NintendoKeys.KekMasks[0], out byte[] RsaOaepKekGenerationSource);

            foreach (Nca nca in fs.Ncas.Values.Select(n => n.Nca))
            {
                ulong titleId = nca.Header.TitleId;

                if (!new ulong[] { // check if title ID is one that needs to be processed before opening it
                    0x0100000000000033, // es
                    0x0100000000000024, // ssl
                }.Contains(titleId))
                    continue;

                // mainly to check if the NCA can be decrypted
                if (!nca.CanOpenSection(0))
                    continue;

                if (nca.Header.ContentType != NcaContentType.Program)
                    continue;

                IFileSystem pfs = nca.OpenFileSystem(NcaSectionType.Code, IntegrityCheckLevel.ErrorOnInvalid);
                pfs.OpenFile(out IFile nsoFile, "main".ToU8Span(), OpenMode.Read);
                Nso nso = new Nso(new FileStorage(nsoFile));
                NsoSection section = nso.Sections[1];
                Stream data = new MemoryStream(section.DecompressSection());
                byte[] key1;
                byte[] key2;
                switch (titleId)
                {
                    case 0x0100000000000033: // es
                        hashes.Clear();

                        byte[] EticketRsaKekSource = new byte[0x10];
                        byte[] EticketRsaKekekSource = new byte[0x10];
                        hashes.Add(new HashSearchEntry(
                            NintendoKeys.EticketRsaKekSourceHash,
                            () => EticketRsaKekSource,
                            0x10));
                        hashes.Add(new HashSearchEntry(
                            NintendoKeys.EticketRsaKekekSourceHash,
                            () => EticketRsaKekekSource,
                            0x10));
                        data.FindKeysViaHash(hashes, new SHA256Managed(), 0x10, data.Length);

                        key1 = new byte[0x10];
                        new AesEcbDecryptor(HACGUIKeyset.Keyset.MasterKeys[0]).Transform(RsaOaepKekGenerationSource, key1);
                        key2 = new byte[0x10];
                        new AesEcbDecryptor(key1).Transform(EticketRsaKekekSource, key2);
                        new AesEcbDecryptor(key2).Transform(EticketRsaKekSource, HACGUIKeyset.Keyset.EticketRsaKek);
                        break;
                    case 0x0100000000000024: // ssl
                        hashes.Clear();

                        byte[] SslAesKeyX = new byte[0x10];
                        byte[] SslRsaKeyY = new byte[0x10];
                        hashes.Add(new HashSearchEntry(
                            NintendoKeys.SslAesKeyXHash,
                            () => SslAesKeyX,
                            0x10));
                        hashes.Add(new HashSearchEntry(
                            NintendoKeys.SslRsaKeyYHash,
                            () => SslRsaKeyY,
                            0x10));
                        data.FindKeysViaHash(hashes, new SHA256Managed(), 0x10, data.Length);

                        key1 = new byte[0x10];
                        new AesEcbDecryptor(HACGUIKeyset.Keyset.MasterKeys[0]).Transform(RsaPrivateKekGenerationSource, key1);
                        key2 = new byte[0x10];
                        new AesEcbDecryptor(key1).Transform(SslAesKeyX, key2);
                        new AesEcbDecryptor(key2).Transform(SslRsaKeyY, HACGUIKeyset.Keyset.SslRsaKek);
                        break;
                }
            }

            // save PRODINFO to file, then derive eticket_ext_key_rsa
            if(!TryDumpCert(nand: nand))
            {
                MessageBox.Show($"Failed to parse decrypted certificate. If you are using Incognito, select your PRODINFO backup now.");
                Dispatcher.Invoke(() => // dispatcher is required, otherwise a deadlock occurs. probably some threading issue
                {
                    while (true)
                    {
                        FileInfo info = RequestOpenFileFromUser(".bin", "PRODINFO backup (.bin)|*.bin", "Select a valid PRODINFO backup...", "PRODINFO.bin");
                        if (info != null)
                        {
                            if (TryDumpCert(info))
                                break;
                        }
                        else
                        {
                            MessageBox.Show("Failed to parse provided PRODINFO. You must have a valid PRODINFO backup.");
                        }
                    }
                });
            }

            // get tickets
            new DecryptTicketsTask(PickConsolePage.ConsoleName).CreateTask().RunSynchronously();

            FatFileSystemProvider system = NANDService.NAND.OpenSystemPartition();
            system.OpenFile(out IFile nsAppmanFile, "save\\8000000000000043".ToU8Span(), OpenMode.Read);
            IStorage nsAppmanStorage = nsAppmanFile.AsStorage();
            SaveDataFileSystem nsAppmanSave = new SaveDataFileSystem(HACGUIKeyset.Keyset, nsAppmanStorage, IntegrityCheckLevel.ErrorOnInvalid, false);
            nsAppmanSave.OpenFile(out IFile privateFile, "/private".ToU8Span(), OpenMode.Read);

            byte[] sdIdenitifer = new byte[0x10];
            byte[] sdSeed = new byte[0x10];

            using (nsAppmanFile)
            using (nsAppmanSave)
            using (privateFile)
            {
                IStorage privateStorage = privateFile.AsStorage();
                privateStorage.Read(0, sdIdenitifer); // stored on SD and NAND, used to uniquely idenitfy the SD/NAND
                privateStorage.Read(0x10, sdSeed);
            }
            HACGUIKeyset.Keyset.SetSdSeed(sdSeed);
            Preferences.Current.SdIdentifiers[sdIdenitifer.ToHexString()] = sdSeed.ToHexString();

            NANDService.Stop();

            DirectoryInfo oldKeysDirectory = HACGUIKeyset.RootFolderInfo.GetDirectory("keys");
            if (oldKeysDirectory.Exists)
                oldKeysDirectory.Delete(true); // fix old versions after restructure of directory
            

            // write all keys to file
            new SaveKeysetTask(PickConsolePage.ConsoleName).CreateTask().RunSynchronously();

            Preferences.Current.DefaultConsoleName = PickConsolePage.ConsoleName;
            Preferences.Current.Write();
        }

        private static bool TryDumpCert(FileInfo prodinfo = null, Nand nand = null)
        {
            try
            {
                Calibration cal0;
                byte[] certBytes;
                using (Stream prodinfoFile = HACGUIKeyset.TempPRODINFOFileInfo.Create())
                {
                    // copy PRODINFO from local file
                    Stream prodinfoStream = null;

                    if (prodinfo != null)
                        prodinfoStream = prodinfo.OpenRead();
                    else
                        prodinfoStream = nand.OpenProdInfo();
                    
                    prodinfoStream.CopyTo(prodinfoFile);
                    prodinfoStream.Close();

                    prodinfoFile.Seek(0, SeekOrigin.Begin);
                    cal0 = new Calibration(prodinfoFile);

                    prodinfoFile.Seek(0x0AD0, SeekOrigin.Begin);  // seek to certificate length
                    byte[] buffer = new byte[0x4];
                    prodinfoFile.Read(buffer, 0, buffer.Length); // read cert length
                    uint certLength = BitConverter.ToUInt32(buffer, 0);

                    certBytes = new byte[certLength];
                    prodinfoFile.Seek(0x0AE0, SeekOrigin.Begin); // seek to cert (should be redundant?)
                    prodinfoFile.Read(certBytes, 0, (int)certLength); // read actual cert
                }

                byte[] counter = cal0.SslExtKey.Take(0x10).ToArray();
                byte[] privModulus = cal0.SslExtKey.Skip(0x10).ToArray(); // bit strange structure but it works

                new Aes128CtrTransform(HACGUIKeyset.Keyset.SslRsaKek, counter).TransformBlock(privModulus); // decrypt private modulus

                X509Certificate certificate = new X509CertificateParser().ReadCertificate(certBytes);
                AsymmetricKeyParameter privKey = certificate.RecoverPrivateParameter(privModulus);

                var store = new Pkcs12Store();
                X509CertificateEntry certEntry = new X509CertificateEntry(certificate);
                store.SetCertificateEntry(certificate.SubjectDN.ToString(), certEntry);

                AsymmetricKeyEntry privKeyEntry = new AsymmetricKeyEntry(privKey);
                store.SetKeyEntry(certificate.SubjectDN.ToString() + "_key", privKeyEntry, new X509CertificateEntry[] { certEntry });

                using (Stream pfxStream = HACGUIKeyset.GetClientCertificateByName(PickConsolePage.ConsoleName).Create())
                    store.Save(pfxStream, "switch".ToCharArray(), new SecureRandom());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void OnBack()
        {
            NANDService.Stop();
        }

        private void RestartAsAdminButtonClick(object sender, RoutedEventArgs e)
        {
            if (!Native.IsAdministrator)
            {
                new SaveKeysetTask(PickConsolePage.ConsoleName).CreateTask().RunSynchronously();

                Native.LaunchProgram(
                    AppDomain.CurrentDomain.FriendlyName,
                    () => Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown()),
                    $"continue \"{PickConsolePage.ConsoleName}\"",
                    true);
            } else
            {
                InjectService.SendPayload(HACGUIKeyset.MemloaderPayloadFileInfo);
                InjectService.SendIni(HACGUIKeyset.MemloaderSampleFolderInfo.GetFile("ums_emmc.ini"));
            }
        }
    }
}
