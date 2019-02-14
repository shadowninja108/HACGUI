using LibHac;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Navigation;

namespace HACGUI.Extensions
{
    public static class Extensions
    {
        public static T FindParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
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
            return new DirectoryInfo($"{obj.FullName}{Path.DirectorySeparatorChar}{foldername}");
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

        public static void CopyToNew(this Stream source, Stream destination, long length = long.MaxValue)
        {
            int b = 0;
            long beginning = source.Position;
            b = source.ReadByte();
            while(b != -1 && (source.Position - beginning) <= length)
            {
                destination.WriteByte((byte) b);
                b = source.ReadByte();
            }
        }

        // as efficient as hashing shit everywhere will get
        public static Dictionary<byte[], byte[]> FindKeyViaHash(this Stream source, List<HashSearchEntry> searches, HashAlgorithm crypto, int dataLength, long length = -1)
        {
            Dictionary<byte[], byte[]> dict = new Dictionary<byte[], byte[]>();
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
                            dict[search.Hash] = new byte[dataLength];
                            Array.Copy(buffer, dict[search.Hash], dataLength);
                        }
                if (searches.Count == dict.Count)
                    break; // found all the keys
                if (source.Position == length)
                    throw new EndOfStreamException("Not all keys were found in the stream!");

                source.Position -= (dataLength - 1);
            }
            return dict;
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

            public HashSearchEntry(byte[] hash, long dataLength)
            {
                Hash = hash;
                DataLength = dataLength;
            }
        }

        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
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
                else
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
                    string TitleID = $"{title.Id:x16}";
                    byte M = Convert.ToByte(TitleID.Substring(12, 1), 16);
                    TitleID = $"{TitleID.ToLower().Substring(0, 12)}{M - M % 2:x}000";
                    return Convert.ToUInt64(TitleID, 16);
            }
            return title.Id;
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
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ext,
                Filter = filter
            };

            if (title != null)
                dlg.Title = title;
            dlg.FileName = fileName ?? "";

            if (dlg.ShowDialog() == DialogResult.OK)
                return new FileInfo(dlg.FileName);
            return null;
        }

        public static FileInfo[] RequestOpenFilesFromUser(string ext, string filter, string title = null, string fileName = null)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                DefaultExt = ext,
                Filter = filter,
                Multiselect = true
            };

            if (title != null)
                dlg.Title = title;
            dlg.FileName = fileName ?? "";

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                FileInfo[] infos = new FileInfo[dlg.FileNames.Length];
                for (int i = 0; i < infos.Length; i++)
                    infos[i] = new FileInfo(dlg.FileNames[i]);
                return infos;
            }
            return null;
        }

        public static FileInfo RequestSaveFileFromUser(string ext, string filter, string title = null)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                DefaultExt = ext,
                Filter = filter
            };

            if (title != null)
                dlg.Title = title;

            if (dlg.ShowDialog() == DialogResult.OK)
                return new FileInfo(dlg.FileName);
            return null;
        }

        public static void CreateAndClose(this FileInfo info)
        {
            File.WriteAllBytes(info.FullName, new byte[] { });
        }

    }
}
