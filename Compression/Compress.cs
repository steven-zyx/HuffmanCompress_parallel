using Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Tool;

namespace Compression
{
    public class Compress
    {
        const int _bufferSize4ReadByte = 128 * 1024;
        const int _bufferSize4WriteByte = 128 * 1024;
        const string sourceFile = @"C:\Users\10788\Desktop\Data\1G_Origin.txt";
        const string targetFile = @"C:\Users\10788\Desktop\Data\1G_Compressed.txt";
        private static int[] _freqTable = new int[256];
        private static string[] _codeTable = new string[256];
        private static ulong _totalChar;

        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            EmptyTarget();
            Console.WriteLine($"File:{sourceFile}");
            Console.WriteLine($"MD5:{Tools.GetDigest(sourceFile)}");
            Console.WriteLine("Now start.");
            watch.Start();

            ReadData(sourceFile);
            TrieNode root = BuildTrie();
            BuildCode(root, "");
            WriteTrie(root);
            WriteTotalChar();
            WriteContent();

            Console.WriteLine($"Elapsed: {watch.Elapsed}");
            Console.WriteLine("Complete. Press Any Key to exit.");
            Console.ReadKey();
        }

        static void EmptyTarget()
        {
            FileInfo target = new FileInfo(targetFile);
            if (target.Exists)
                target.Delete();
        }

        static void ReadData(string fileName)
        {
            using (FileStream stream = File.OpenRead(fileName))
            {
                byte[] buffer = new byte[_bufferSize4ReadByte];
                int readCount = 0;
                do
                {
                    readCount = stream.Read(buffer, 0, _bufferSize4ReadByte);
                    TabulateFreq(buffer, readCount);
                } while (readCount == _bufferSize4ReadByte);
            }
        }

        static void TabulateFreq(byte[] buffer, int readCount)
        {
            _totalChar += (ulong)readCount;
            for (int i = 0; i < readCount; i++)
            {
                _freqTable[buffer[i]]++;
            }
        }

        static TrieNode BuildTrie()
        {
            MinPQ minPQ = new MinPQ();
            for (ushort i = 0; i <= 255; i++)
            {
                if (_freqTable[i] > 0)
                    minPQ.Insert(new TrieNode((Byte)i, _freqTable[i], null, null));
            }

            while (minPQ.Count > 1)
            {
                TrieNode left = minPQ.DelMin();
                TrieNode right = minPQ.DelMin();
                minPQ.Insert(new TrieNode(0, left.Freq + right.Freq, left, right));
            }
            return minPQ.DelMin();
        }

        static void BuildCode(TrieNode node, string s)
        {
            if (node.IsLeaf)
            {
                _codeTable[node.Ch] = s;
                return;
            }
            BuildCode(node.Left, s + '0');
            BuildCode(node.Right, s + '1');
        }

        public static void WriteTrie(TrieNode node)
        {
            using (Stream stream = File.OpenWrite(targetFile))
            {
                WriteNode(node, stream);
            }
        }

        static void WriteNode(TrieNode node, Stream stream)
        {
            if (node.IsLeaf)
            {
                stream.WriteByte(255);
                stream.WriteByte(node.Ch);
                return;
            }
            stream.WriteByte(0);
            WriteNode(node.Left, stream);
            WriteNode(node.Right, stream);
        }

        public static void WriteTotalChar()
        {
            FileStream stream = new FileStream(targetFile, FileMode.Append);
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(_totalChar);
            }
        }

        static void WriteContent()
        {
            using (FileStream reader = File.OpenRead(sourceFile))
            using (FileStream writer = new FileStream(targetFile, FileMode.Append))
            {
                byte[] buffer4Read = new byte[_bufferSize4ReadByte];
                BitArray buffer4WriteBit = new BitArray(_bufferSize4ReadByte * 8 * 8);
                int endIndexOfBitArray = 0;
                byte[] buffer4Write = new byte[_bufferSize4ReadByte * 8];
                int readCount = 0;
                Queue<bool> reminder = new Queue<bool>(8);
                while (true)
                {
                    endIndexOfBitArray = 0;
                    readCount = reader.Read(buffer4Read, 0, _bufferSize4ReadByte);

                    //place the reminder back to the next bit buffer.
                    while (reminder.Count > 0)
                    {
                        buffer4WriteBit[endIndexOfBitArray] = reminder.Dequeue();
                        endIndexOfBitArray++;
                    }

                    //convert the buffer read to a bit buffer which is now not aligned.
                    for (int i = 0; i < readCount; i++)
                    {
                        char[] chars = _codeTable[buffer4Read[i]].ToCharArray();
                        foreach (char value in chars)
                        {
                            buffer4WriteBit[endIndexOfBitArray] = value == '1' ? true : false;
                            endIndexOfBitArray++;
                        }
                    }

                    //convert the bit buffer to byte buffer then write.
                    buffer4WriteBit.CopyTo(buffer4Write, 0);
                    if (readCount == _bufferSize4ReadByte)
                    {
                        //hold the reminder of bits, in order to perform a floor-align.
                        for (int i = (endIndexOfBitArray / 8) * 8; i < endIndexOfBitArray; i++)  //i begin with a flooring of endIndexOfBitArray with a facter of 8
                        {
                            reminder.Enqueue(buffer4WriteBit[i]);
                        }
                        writer.Write(buffer4Write, 0, endIndexOfBitArray / 8);
                    }
                    else
                    {
                        //last time to write, do a ceilling-align
                        writer.Write(buffer4Write, 0, (endIndexOfBitArray / 8) + 1);
                        break;
                    }

                }
            }
        }

    }
}
