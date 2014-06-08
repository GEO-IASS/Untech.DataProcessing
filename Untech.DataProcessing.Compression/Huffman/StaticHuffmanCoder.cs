using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Untech.DataProcessing.Compression.Huffman
{
    public class StaticHuffmanCoder
    {
        internal class Node
        {
            private readonly byte _symbolCode;
            private readonly int _symbolsCount;

            private Node _left;
            private Node _right;

            public Node(byte symbolCode, int symbolsCount)
            {
                _symbolCode = symbolCode;
                _symbolsCount = symbolsCount;
            }

            public Node(Node left, Node right)
            {
                _symbolsCount = left.SymbolsCount + right.SymbolsCount;

                _left = left;
                _right = right;
            }

            public byte SymbolCode { get { return _symbolCode; } }
            public int SymbolsCount { get { return _symbolsCount; } }

            public Node Left { get { return _left; } }
            public Node Right { get { return _right; } }
        }

        internal class CompressedSymbol
        {
            public byte Dest { get; set; }
            public byte Mask { get; set; }
        }


        private readonly Dictionary<byte, int> _probabilities;
        private readonly List<Node> _nodes;
        private Node _root;
        private readonly Dictionary<byte, CompressedSymbol> _dictionary;

        public StaticHuffmanCoder()
        {
            _probabilities = new Dictionary<byte, int>();
            _nodes = new List<Node>();
            _dictionary = new Dictionary<byte, CompressedSymbol>();
        }

        public void Encode(IntPtr data, int sizeInBytes, Stream stream)
        {
            unsafe
            {
                var dataPointer = (byte*)data.ToPointer();

                CountProbabilites(dataPointer, sizeInBytes);

                CreateNodes();

                BuildTree();

                BuildDictionary(_root, 0, 0);

                WriteDictionary(stream);

                CompressData(dataPointer, sizeInBytes, stream);
            }
        }

        private unsafe void CountProbabilites(byte* data, int size)
        {
            for (int i = 0; i < size; i++)
            {
                var t = *(data + i);
                if (_probabilities.ContainsKey(t))
                {
                    _probabilities[t] = _probabilities[t] + 1;
                }
                else
                {
                    _probabilities.Add(t, 1);
                }
            }
        }

        private void CreateNodes()
        {
            foreach (var probability in _probabilities)
            {
                _nodes.Add(new Node(probability.Key, probability.Value));
            }
        }

        private void BuildTree()
        {
            while (_nodes.Count > 1)
            {
                var ordered = _nodes.OrderBy(n => n.SymbolsCount).ToList();
                var left = ordered.First();
                var right = ordered.Skip(1).First();

                _nodes.Remove(left);
                _nodes.Remove(right);

                _nodes.Add(new Node(left, right));
            }

            _root = _nodes.First();
        }

        private void BuildDictionary(Node node, byte code, byte mask)
        {
            if (node == null)
                return;
            if (node.Left != null && node.Right != null)
            {
                code <<= 1;
                mask <<= 1;
                mask |= 1;

                BuildDictionary(node.Left, code, mask);
                BuildDictionary(node.Right, (byte)(code | 1), mask);
            }
            else if (node.Left != null || node.Right != null)
            {
                throw new InvalidDataException();
            }
            else
            {
                _dictionary.Add(node.SymbolCode, new CompressedSymbol { Dest = code, Mask = mask });
            }
        }

        private void WriteDictionary(Stream stream)
        {
            var writer = new BinaryWriter(stream);
            foreach (var compressedSymbol in _dictionary)
            {
                if (compressedSymbol.Key == 0xFE || compressedSymbol.Key == 0xFD)
                {
                    writer.Write((byte)0xFD);
                }
                writer.Write(compressedSymbol.Key);
                if (compressedSymbol.Value.Dest == 0xFE || compressedSymbol.Value.Dest == 0xFD)
                {
                    writer.Write((byte)0xFD);
                }
                writer.Write(compressedSymbol.Value.Dest);
                if (compressedSymbol.Value.Mask == 0xFE || compressedSymbol.Value.Mask == 0xFD)
                {
                    writer.Write((byte)0xFD);
                }
                writer.Write(compressedSymbol.Value.Mask);
            }
            writer.Write((byte)0xFE);
        }

        private unsafe void CompressData(byte* data, int size, Stream stream)
        {
            var writer = new BinaryWriter(stream);
            int registerA = 0;
            int registerMask = 0;
            int codedSymbol;
            int codedMask;

            for (int i = 0; i < size; i++)
            {
                var source = *(data + i);

                codedSymbol = _dictionary[source].Dest;
                codedMask = _dictionary[source].Mask;

                registerA |= codedSymbol;
                registerMask |= codedMask;

                while ((registerMask & 0xFF) != 0)
                {
                    if ((registerMask & 0xFF00) == 0xFF00)
                    {
                        var output = (byte)(registerA >> 8);
                        if (output == 0xFE || output == 0xFD)
                        {
                            writer.Write((byte)0xFD);
                        }
                        writer.Write(output);

                        registerA &= 0xFF;
                        registerMask &= 0xFF;
                    }

                    registerA <<= 1;
                    registerMask <<= 1;

                    if ((registerMask & 0xFF00) == 0xFF00)
                    {
                        var output = (byte)(registerA >> 8);
                        if (output == 0xFE || output == 0xFD)
                        {
                            writer.Write((byte)0xFD);
                        }
                        writer.Write(output);

                        registerA &= 0xFF;
                        registerMask &= 0xFF;
                    }
                }
            }

            if ((registerMask & 0xFF00) == 0xFF00)
            {
                var output = (byte)(registerA >> 8);
                if (output == 0xFE || output == 0xFD)
                {
                    writer.Write((byte)0xFD);
                }
                writer.Write(output);
            }
        }
    }
}