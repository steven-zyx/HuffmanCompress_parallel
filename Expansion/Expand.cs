using System;
using System.IO;
using Entity;
using System.Collections;
using System.Text;
using Tool;

namespace Expansion
{
    class Expand
    {
        const string sourceFile = @"C:\Users\Administrator\Desktop\128M_Compressed_Parallel.txt";
        const string targetFile = @"C:\Users\Administrator\Desktop\128M_Expand.txt";
        static byte[] _buffer4ReadTrie = new byte[1];
        static ulong _totalChar;
        static FileStream reader;


        static void Main(string[] args)
        {
            EmptyTarget();
            Console.WriteLine("Now start.");
            using (reader = File.OpenRead(sourceFile))
            {
                TrieNode root = ReadTrie();
                ReadTotalChar();
                ExpandContent(root);
            }

            Console.WriteLine($"Complete. File:{targetFile}");
            Console.WriteLine($"MD5:{Tools.GetDigest(targetFile)}");
            Console.WriteLine("Press any key to exit.");

            Console.ReadKey();
        }

        static void EmptyTarget()
        {
            FileInfo target = new FileInfo(targetFile);
            if (target.Exists)
                target.Delete();
        }

        static TrieNode ReadTrie()
        {
            reader.Read(_buffer4ReadTrie, 0, 1);
            if (_buffer4ReadTrie[0] == 255)
            {
                reader.Read(_buffer4ReadTrie, 0, 1);
                return new TrieNode(_buffer4ReadTrie[0], 0, null, null);
            }
            return new TrieNode(0, 0, ReadTrie(), ReadTrie());
        }

        static void ReadTotalChar()
        {
            using (BinaryReader bReader = new BinaryReader(reader, Encoding.Default, true))
            {
                _totalChar = bReader.ReadUInt64();
            }
        }

        static void ExpandContent(TrieNode root)
        {
            using (FileStream writer = File.OpenWrite(targetFile))
            {
                byte[] buffer4Convert = new byte[1];
                BitArray bits4SingleByte = new BitArray(8);
                TrieNode x = root;
                while (true)
                {
                    buffer4Convert[0] = (byte)reader.ReadByte();
                    bits4SingleByte = new BitArray(buffer4Convert);
                    foreach (bool digit in bits4SingleByte)
                    {
                        if (x.IsLeaf)
                        {
                            writer.WriteByte(x.Ch);
                            x = root;
                            _totalChar--;
                            if (_totalChar == 0)
                                return;
                        }
                        x = digit ? x.Right : x.Left;
                    }
                }
            }
        }
    }
}
