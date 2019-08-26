using LibHac;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media;
using HACGUI.Utilities;
using System.Windows.Controls;
using LibHac.Fs.NcaUtils;
using LibHac.Fs.Save;
using LibHac.Fs;
using System.Reflection;

namespace HACGUI.Extensions
{
    public static class Extensions
    {
        private static readonly PropertyInfo InheritanceContextProp = typeof(DependencyObject).GetProperty("InheritanceContext", BindingFlags.NonPublic | BindingFlags.Instance);

        public static T FindParent<T>(this DependencyObject dependencyObject) where T : DependencyObject
        {
            DependencyObject parent = LogicalTreeHelper.GetParent(dependencyObject);

            if (parent == null)
            {
                if (dependencyObject is FrameworkElement)
                {
                    parent = VisualTreeHelper.GetParent(dependencyObject);
                }
                if (parent == null && dependencyObject is ContentElement)
                {
                    parent = ContentOperations.GetParent((ContentElement)dependencyObject);
                }
                if (parent == null)
                {
                    parent = InheritanceContextProp.GetValue(dependencyObject, null) as DependencyObject;
                }
            }

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? parent.FindParent<T>();
        }

        public static FileInfo GetFile(this DirectoryInfo obj, string filename)
        {
            return new FileInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{filename}");
        }

        public static bool ContainsFile(this DirectoryInfo obj, string filename)
        {
            return obj.GetFile(filename).Exists;
        }

        public static DirectoryInfo GetDirectory(this DirectoryInfo obj, string foldername)
        {
            return new DirectoryInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{foldername.Replace('/', Path.DirectorySeparatorChar)}");
        }


        public static FileInfo FindFile(this DirectoryInfo root, string filename, SearchOption option = SearchOption.AllDirectories)
        {
            return root.FindFiles(new string[] { filename }, option)[0];
        }

        public static FileInfo[] FindFiles(this DirectoryInfo root, string[] filenames, SearchOption option = SearchOption.AllDirectories)
        {
            FileInfo[] infos = new FileInfo[filenames.Length];
            foreach (FileInfo file in root.EnumerateFiles("*", option))
            {
                foreach (string filename in filenames)
                {
                    void Check()
                    {
                        if (file.Name == filename)
                        {
                            infos[Array.IndexOf(filenames, filename)] = file;
                            return;
                        }
                    };
                    Check(); // awful hack to prevent a break from exiting the entire loop
                }
            }
            return infos;
        }

        public static bool CanOpen(this FileInfo info)
        {
            try
            {
                using (info.OpenRead())
                {
                    return true;
                }
            } catch
            {
                return false;
            }
        }
        public static void CopyToNew(this Stream source, Stream destination, long length = long.MaxValue)
        {
            long beginning = source.Position;
            int b = source.ReadByte();
            while (b != -1 && (source.Position - beginning) <= length)
            {
                destination.WriteByte((byte)b);
                b = source.ReadByte();
            }
        }

        // as efficient as hashing shit everywhere will get
        public static void FindKeysViaHash(this Stream source, List<HashSearchEntry> searches, HashAlgorithm crypto, int dataLength, long length = -1)
        {
            int count = 0;
            long beginning = source.Position;
            byte[] buffer = new byte[dataLength];

            if (length == -1)
                length = source.Length;

            while ((source.Position - beginning) < length)
            {
                source.Read(buffer, 0, dataLength);
                crypto.ComputeHash(buffer);

                foreach (HashSearchEntry search in searches)
                    if (search.DataLength == dataLength)
                        if (crypto.Hash.SequenceEqual(search.Hash))
                        {
                            Array.Copy(buffer, search.Setter(), dataLength);
                            count++;
                        }
                if (searches.Count == count)
                    break; // found all the keys
                if (source.Position == length)
                    throw new EndOfStreamException("Not all keys were found in the stream!");

                source.Position -= (dataLength - 1);
            }
        }

        public static bool VerifyViaHash(this byte[] data, byte[] expectedHash, HashAlgorithm crypto)
        {
            byte[] hash = crypto.ComputeHash(data);
            return hash.SequenceEqual(expectedHash);
        }

        public static void WriteString(this Stream stream, string str, Encoding encoding = null)
        {
            if (encoding == null)
                encoding = Encoding.ASCII;

            byte[] bytes = encoding.GetBytes(str);
            stream.Write(bytes, 0, bytes.Length);
        }

        public struct HashSearchEntry
        {
            public byte[] Hash;
            public long DataLength;
            public Func<byte[]> Setter;

            public HashSearchEntry(byte[] hash, Func<byte[]> setter, long dataLength)
            {
                Hash = hash;
                DataLength = dataLength;
                Setter = setter;
            }
        }

        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection, bool silent = false)
        {
            if (collection == null)
            {
                throw new ArgumentNullException("Collection is null");
            }

            foreach (var item in collection)
            {
                if (!source.ContainsKey(item.Key))
                {
                    source.Add(item.Key, item.Value);
                }
                else if (!silent)
                {
                    throw new Exception($"Duplicate key {item.Key}!");
                }
            }
        }

        public static ulong GetBaseTitleID(this Title title)
        {
            switch (title.Metadata.Type)
            {
                case TitleType.AddOnContent:
                case TitleType.Patch:
                    return GetBaseTitleID(title.Id);
            }
            return title.Id;
        }

        public static ulong GetBaseTitleID(this ulong titleId)
        {
            string TitleID = $"{titleId:x16}";
            byte M = Convert.ToByte(TitleID.Substring(12, 1), 16);
            TitleID = $"{TitleID.ToLower().Substring(0, 12)}{M - M % 2:x}000";
            return Convert.ToUInt64(TitleID, 16);
        }

        public static ulong GetRightsId(this Nca nca)
        {
            byte[] tidArr = nca.Header.RightsId.ToArray();
            Array.Reverse(tidArr);
            ulong tidl = BitConverter.ToUInt64(tidArr, 0);
            ulong tidu = BitConverter.ToUInt64(tidArr, 0x8);
            return tidu | tidl;
        }

        public static PseudoFileSystem MakeFs(this IEnumerable<SwitchFsNca> ncas)
        {
            PseudoFileSystem ncaFs = new PseudoFileSystem();
            foreach (SwitchFsNca nca in ncas)
            {
                string s = nca.Filename;
                while (s.EndsWith(".nca"))
                    s = s.Substring(0, s.IndexOf(".nca"));
                ncaFs.Add($"/{s}.nca", nca.Nca.BaseStorage);
            }
            return ncaFs;
        }

        public static PseudoFileSystem MakeFs(this IEnumerable<FileInfo> files)
        {
            PseudoFileSystem fs = new PseudoFileSystem();
            foreach (FileInfo file in files)
            {
                LocalFileSystem localfs = new LocalFileSystem(file.Directory.FullName);

                // clean up filename so it only ends with .nca, then map to actual name
                string s = file.Name;
                while (s.EndsWith(".nca"))
                    s = s.Substring(0, s.IndexOf(".nca"));
                fs.Add($"/{s}.nca", $"/{file.Name}", localfs);
            }
            return fs;
        }

        public static void MatchupBaseNca(this IEnumerable<SwitchFsNca> ncas)
        {
            Dictionary<ulong, Dictionary<ContentType, SwitchFsNca>> mainNcas = new Dictionary<ulong, Dictionary<ContentType, SwitchFsNca>>();
            PseudoFileSystem ps = ncas.MakeFs();
            SwitchFs fs = SwitchFs.OpenNcaDirectory(HACGUIKeyset.Keyset, ps);
            foreach (KeyValuePair<ulong, LibHac.Application> kv in fs.Applications)
            {
                ulong tid = kv.Key;
                LibHac.Application app = kv.Value;

                if(app.Patch != null && app.Main != null)
                    foreach (SwitchFsNca nca in app.Patch.Ncas)
                    {
                        ContentType type = nca.Nca.Header.ContentType;
                        SwitchFsNca baseNca = app.Main.Ncas.Where(n => n.Nca.Header.ContentType == type).FirstOrDefault();
                        if (baseNca != null)
                        {
                            bool hasPatch = false;
                            for(int i = 0; i < 4; i++)
                            {
                                Nca n = nca.Nca;
                                if (n.CanOpenSection(i))
                                {
                                    NcaFsHeader section = n.Header.GetFsHeader(i);
                                    if(section.IsPatchSection())
                                    {
                                        hasPatch = true;
                                        break;
                                    }
                                }
                            }
                            if(hasPatch)
                                ncas.Where(n => n.Filename == nca.Filename.Replace("/", "")).First().BaseNca = baseNca.Nca; // set original NCA, not new parsed one
                        }
                    }

            }
        }

        public static byte[] ToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] Serialize(this object obj)
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (var ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        public static object Deserialize(this byte[] bytes)
        {
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(bytes, 0, bytes.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                return obj;
            }
        }

        public static FileInfo RequestOpenFileFromUser(string ext, string filter, string title = null, string fileName = null)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ext,
                Filter = filter
            };

            if (title != null)
                dlg.Title = title;
            dlg.FileName = fileName ?? "";

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return new FileInfo(dlg.FileName);
            return null;
        }

        public static FileInfo[] RequestOpenFilesFromUser(string ext, string filter, string title = null, string fileName = null)
        {
            System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog
            {
                DefaultExt = ext,
                Filter = filter,
                Multiselect = true
            };

            if (title != null)
                dlg.Title = title;
            dlg.FileName = fileName ?? "";

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FileInfo[] infos = new FileInfo[dlg.FileNames.Length];
                for (int i = 0; i < infos.Length; i++)
                    infos[i] = new FileInfo(dlg.FileNames[i]);
                return infos;
            }
            return null;
        }

        public static FileInfo RequestSaveFileFromUser(string ext, string filter, string title = null, string fileName = null)
        {
            System.Windows.Forms.SaveFileDialog dlg = new System.Windows.Forms.SaveFileDialog
            {
                DefaultExt = ext,
                Filter = filter,
                FileName = fileName ?? ""
            };

            if (title != null)
                dlg.Title = title;

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                return new FileInfo(dlg.FileName);
            return null;
        }

        public static void CreateAndClose(this FileInfo info)
        {
            File.WriteAllBytes(info.FullName, new byte[] { });
        }

        public static string FilterMultilineString(this string str)
        {
            str = str.Replace("\t", "");
            string retstr = "";
            string sub = str;
            while (true)
            {
                if (string.IsNullOrWhiteSpace(sub.Replace(Environment.NewLine, ""))) break;
                if (string.IsNullOrWhiteSpace(sub.Replace("\n", ""))) break;
                char ce = sub.Where(c => !char.IsWhiteSpace(c)).FirstOrDefault();
                int ie = sub.IndexOf(ce);
                sub = sub.Substring(ie);
                int s = sub.IndexOf(Environment.NewLine);
                if (s == -1) // fuck you too Windows
                    s = sub.IndexOf("\n");
                retstr += sub.Substring(0, s);
                sub = sub.Substring(s);
            }
            return retstr;
        }

        public static List<Ticket> DumpTickets(Keyset keyset, IStorage savefile, string consoleName)
        {
            var tickets = new List<Ticket>();
            var save = new SaveDataFileSystem(keyset, savefile, IntegrityCheckLevel.ErrorOnInvalid, false);
            var ticketList = new BinaryReader(save.OpenFile("/ticket_list.bin", OpenMode.Read).AsStream());
            var ticketFile = new BinaryReader(save.OpenFile("/ticket.bin", OpenMode.Read).AsStream());
            DirectoryInfo ticketFolder = HACGUIKeyset.GetTicketsDirectory(consoleName);
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

        public static ulong GetUlong(this ManagementObject obj, string name)
        {
            object o = obj.GetPropertyValue(name) ?? 0;
            ulong o1 = Convert.ToUInt64(o);
            return o1;
        }

        public static int GetInt(this ManagementObject obj, string name)
        {
            object o = obj.GetPropertyValue(name) ?? 0;
            int o1 = Convert.ToInt32(o);
            return o1;
        }

        public static void DeleteRecursively(this DirectoryInfo obj)
        {
            foreach (FileInfo file in obj.EnumerateFiles())
                file.Delete();

            foreach (DirectoryInfo directory in obj.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                foreach (FileInfo file in directory.EnumerateFiles())
                    file.Delete();
                directory.Delete();
            }

            obj.Delete();
        }

        public static void XOR(this byte[] buffer1, byte[] buffer2, out byte[] output)
        {
            if (buffer1.Length != buffer2.Length)
                throw new InvalidDataException("XOR buffer size must match!");
            output = new byte[buffer1.Length];
            for (int i = 0; i < buffer1.Length; i++)
                output[i] = (byte)(buffer1[i] ^ buffer2[i]);
        }

        public static byte[] ToBytes(this ulong obj, int length)
        {
            byte[] buffer = BitConverter.GetBytes(obj);
            byte[] outp = new byte[length];
            Array.Copy(buffer, outp, length);
            return outp;
        }

        public static byte[] ToBytes(this int obj, int length)
        {
            byte[] buffer = BitConverter.GetBytes(obj);
            byte[] outp = new byte[length];
            Array.Copy(buffer, outp, length);
            return outp;
        }

        public static byte[] ToBytes(this string obj)
        {
            return Encoding.ASCII.GetBytes(obj);
        }

        public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public static ListViewItem GetContainerByItem(this ListView obj, object item)
        {
            int index = obj.Items.IndexOf(item);
            if(index >= 0)
                return obj.ItemContainerGenerator.ContainerFromIndex(index) as ListViewItem;
            return null;
        }

        public static NcaFsHeader GetFsHeader(this NcaHeader obj, NcaFormatType type)
        {
            for(int i = 0; i < 4; i++)
            {
                NcaFsHeader header = obj.GetFsHeader(i);
                if (header.FormatType == type)
                    return header;
            }
            throw new InvalidOperationException($"NCA is missing section type {type}");
        }
    }
}
