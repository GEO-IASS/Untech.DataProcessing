using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Untech.DataProcessing.Compression.Huffman
{
    public class StaticHuffmanDecoder
    {
        private readonly Dictionary<int, byte> _dictionary;

        public StaticHuffmanDecoder()
        {
            _dictionary = new Dictionary<int, byte>();
        }

        public IntPtr Decode(Stream stream)
        {
            var buffer = Marshal.AllocHGlobal(1000 * 1000 * 1000);
            
            ReadDictionary(stream);
            unsafe
            {
                Decompress((byte*)buffer.ToPointer(), 1000 * 1000 * 1000, stream);
            }

            return buffer;
        }

        private void ReadDictionary(Stream stream)
        {
            var reader = new BinaryReader(stream);

            byte key = 0;
            byte value;
            byte mask;

            while (key != 0xFE)
            {
                key = reader.ReadByte();
                
                if (key == 0xFE) break;

                if (key == 0xFD)
                {
                    key = reader.ReadByte();
                }
                value = reader.ReadByte();
                if (value == 0xFD)
                {
                    value = reader.ReadByte();
                }
                mask = reader.ReadByte();
                if (mask == 0xFD)
                {
                    mask = reader.ReadByte();
                }

                _dictionary.Add(mask << 8 | value, key);
            }
        }

        private unsafe void Decompress(byte* data, int size, Stream stream)
        {
            var reader = new BinaryReader(stream);

            int decodedBytes = 0;

            int registerA = 0;
            int registerMask = 0;
            int key;
            byte readed;
            byte code;
            bool flag = true;

            while (stream.Position != stream.Length)
            {
                readed = reader.ReadByte();
                if (readed == 0xFD)
                    readed = reader.ReadByte();
                
                registerA |= readed;
                registerMask |= 0xFF;

                for (int i = 0; i < 8; i++)
                {
                    registerA <<= 1;
                    registerMask <<= 1;
                    registerMask |= 1;

                    code = (byte) ((registerA & 0xFF00) >> 8);
                    key = registerMask & 0xFF00;
                    key |= code;

                    if (_dictionary.ContainsKey(key))
                    {
                        *(data + decodedBytes) = _dictionary[key];
                        registerA &= 0xFF;
                        registerMask &= 0xFF;
                       decodedBytes++;
                    }
                } 
            }
        }
    }
}