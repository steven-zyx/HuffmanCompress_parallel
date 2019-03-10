using Entity;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Tool;

namespace Compression_Parallel
{
    public class Compress_parallel
    {
        const int _bufferSize4Scan = 128 * 1024;
        const int _bufferSize4ReadByte = 128 * 1024;
        const int _bufferSize4WriteByte = 128 * 1024;
        static BlockingCollection<byte[]> _inputBytes = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), 1024);    //128M memory cache
        static BlockingCollection<byte[]> _outputBytes = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>(), 1024);   //128M memory cache
        static ManualResetEventSlim _mres = new ManualResetEventSlim(false);
        const string sourceFile = @"C:\Users\10788\Desktop\Data\1G_Origin.txt";
        const string targetFile = @"C:\Users\10788\Desktop\Data\1G_Compressed_Parallel.txt";
        static int[] _freqTalbe = new int[256];
        static string[] _codeTable = new string[256];
        static ulong _totalChar = 0;

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

            Task tWriteTrie = new Task(() => WriteTrie(root));
            tWriteTrie.ContinueWith(x => WriteTotalChar()).ContinueWith(x => _mres.Set());
            tWriteTrie.Start();
            BuildCode(root, "");

            Parallel.Invoke(
                ProduceInput,
                Convert,
                ConsumeAndOutput);

            Console.WriteLine($"Complete. Elapsed: {watch.Elapsed}");
            Console.WriteLine($"Compressed file MD5:{Tools.GetDigest(targetFile)}");
            Console.WriteLine("Press Any Key to exit.");
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
                byte[] buffer = new byte[_bufferSize4Scan];
                int readCount = 0;
                do
                {
                    readCount = stream.Read(buffer, 0, _bufferSize4Scan);
                    TabulateFreq(buffer, readCount);
                } while (readCount == _bufferSize4Scan);
            }
        }

        static void TabulateFreq(byte[] buffer, int readCount)
        {
            _totalChar += (ulong)readCount;
            for (int i = 0; i < readCount; i++)
            {
                _freqTalbe[buffer[i]]++;
            }
        }

        static TrieNode BuildTrie()
        {
            MinPQ minPQ = new MinPQ();
            for (ushort i = 0; i <= 255; i++)
            {
                if (_freqTalbe[i] > 0)
                    minPQ.Insert(new TrieNode((Byte)i, _freqTalbe[i], null, null));
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

        static void ProduceInput()
        {
            using (FileStream stream = File.OpenRead(sourceFile))
            {
                int readCount = 0;
                while (true)
                {
                    byte[] buffer = new byte[_bufferSize4ReadByte];
                    readCount = stream.Read(buffer, 0, _bufferSize4ReadByte);
                    if (readCount == _bufferSize4ReadByte)
                        _inputBytes.Add(buffer);
                    else
                    {
                        //last time to read
                        //give the comsumer exactly what is read, avoid empty character
                        buffer = buffer.Take(readCount).ToArray();
                        _inputBytes.Add(buffer);
                        _inputBytes.CompleteAdding();
                        return;
                    }
                }
            }
        }

        static void Convert()
        {
            BitArray bits = new BitArray(_bufferSize4WriteByte * 8);
            byte[] buffer4Write = new byte[_bufferSize4WriteByte];
            int endIndex = 0;
            foreach (byte[] block in _inputBytes.GetConsumingEnumerable())
            {
                foreach (byte ch in block)
                {
                    char[] characters = _codeTable[ch].ToCharArray();
                    if (_bufferSize4WriteByte * 8 == endIndex)
                    {
                        //The BitArray is just full, no available space left.
                        //convert it to Byte[], Clone one then add to the output collection.
                        bits.CopyTo(buffer4Write, 0);
                        _outputBytes.Add((byte[])buffer4Write.Clone());
                        endIndex = 0;
                    }
                    else if (characters.Length > _bufferSize4WriteByte * 8 - endIndex)
                    {
                        //The BitArray is not full, but no space for this entire word.
                        //place the left part of the word to the BitArray,
                        //convert to Byte[] then add to the output collection.
                        //reset the pointer of the BitArray,
                        //place the right part of the word to the BitArray,
                        //then continue to the next word.
                        int bits4CurrentBlock = _bufferSize4WriteByte * 8 - endIndex;
                        for (int i = 0; i < bits4CurrentBlock; i++)
                        {
                            bits[endIndex] = characters[i] == '1' ? true : false;
                            endIndex++;
                        }
                        bits.CopyTo(buffer4Write, 0);
                        _outputBytes.Add((byte[])buffer4Write.Clone());
                        endIndex = 0;
                        for (int i = bits4CurrentBlock; i < characters.Length; i++)
                        {
                            bits[endIndex] = characters[i] == '1' ? true : false;
                            endIndex++;
                        }
                        continue;
                    }
                    //convert each digit in the character to '1' or '0',
                    //then add them to the BitArray.
                    foreach (char digit in characters)
                    {
                        bits[endIndex] = digit == '1' ? true : false;
                        endIndex++;
                    }
                }
            }
            //no word else to process, convert the BitArray to byte[],
            //then add to the output collection.
            //There's redandunt space written to the file, won't exceed 128K.
            bits.CopyTo(buffer4Write, 0);
            _outputBytes.Add(buffer4Write);
            _outputBytes.CompleteAdding();
        }

        static void ConsumeAndOutput()
        {
            _mres.Wait();
            using (FileStream stream = new FileStream(targetFile, FileMode.Append))
            {
                foreach (byte[] block in _outputBytes.GetConsumingEnumerable())
                {
                    stream.Write(block, 0, block.Length);
                }
            }
        }

    }
}
