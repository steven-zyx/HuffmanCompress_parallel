using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DisplayTrie
{
    public partial class Display : Form
    {
        const int _bufferSize4ReadByte = 128 * 1024;
        const int _bufferSize4ReadChar = 128 * 1024;
        const string sourceFile = @"C:\Users\Administrator\Desktop\1G.txt";
        const string targetFile = @"C:\Users\Administrator\Desktop\conpressed.txt";
        private int[] _freqTalbe = new int[256];

        public Display()
        {
            InitializeComponent();
        }

        void ReadData(string fileName)
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

        void TabulateFreq(byte[] buffer, int readCount)
        {
            for (int i = 0; i < readCount; i++)
            {
                _freqTalbe[buffer[i]]++;
            }
        }

        TrieNode BuildTrie()
        {
            MinPQ minPQ = new MinPQ();
            for (byte i = 0; i <= 255; i++)
            {
                if (_freqTalbe[i] > 0)
                    minPQ.Insert(new TrieNode(i, _freqTalbe[i], null, null));
            }

            while (minPQ.Count > 1)
            {
                TrieNode left = minPQ.DelMin();
                TrieNode right = minPQ.DelMin();
                minPQ.Insert(new TrieNode(0, left.Freq + right.Freq, left, right));
            }
            return minPQ.DelMin();
        }

        void DisplayTrie(TrieNode node)
        {

        }

    }
}
