using System;
using System.Collections.Generic;
using System.Text;
using Entity;

namespace Compression
{
    /// <summary>
    /// Minimum priority queue
    /// Use an unordered array to store value
    /// Only for TrieNode which contain character with *ascii* encoding
    /// </summary>
    internal class MinPQ
    {
        private TrieNode[] _nodeArray = new TrieNode[256];
        private short _lastIndex = -1;
        private TrieNode _value4Swap;
        //private TrieNode _tempValue;

        internal void Insert(TrieNode node)
        {
            _lastIndex++;
            _nodeArray[_lastIndex] = node;
        }

        /// <summary>
        /// Preform an inner selection to find the lowest
        /// Swap it with the last one
        /// Return the last one then decrement the _lastIndex;
        /// </summary>
        /// <returns></returns>
        internal TrieNode DelMin()
        {
            ushort minIndex = 0;
            for (ushort i = 1; i <= _lastIndex; i++)
            {
                if (_nodeArray[i].HasLowerFreq(_nodeArray[minIndex]))
                    minIndex = i;
            }
            Swap(minIndex);
            return _nodeArray[_lastIndex--];
        }

        internal int Count
        {
            get => _lastIndex + 1;
        }

        private void Swap(ushort minIndex)
        {
            _value4Swap = _nodeArray[minIndex];
            _nodeArray[minIndex] = _nodeArray[_lastIndex];
            _nodeArray[_lastIndex] = _value4Swap;
        }

    }
}
