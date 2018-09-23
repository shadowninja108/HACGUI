using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
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
    }
}
