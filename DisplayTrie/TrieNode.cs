using System;

namespace DisplayTrie
{
    public class TrieNode
    {
        public byte Ch { get; }
        public int Freq { get; }
        public TrieNode Left { get; }
        public TrieNode Right { get; }

        public TrieNode(byte ch, int freq, TrieNode left, TrieNode right)
        {
            Ch = ch;
            Freq = freq;
            Left = left;
            Right = right;
        }

        public bool IsLeaf()
        {
            return Left is null && Right is null;
        }

        public bool HasLowerFreq(TrieNode other)
        {
            return Freq < other.Freq;
        }
    }
}
