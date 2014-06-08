//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Untech.DataProcessing.Compression.Arithmetic;
//using Untech.DataProcessing.Images.SPIHT;

//namespace Untech.DataProcessing.Images.NLS
//{
//    public unsafe class NLS3DDecoder
//    {
//        private readonly ArithmeticModel _bitModel;
//        private readonly short* _coefficients;
//        private readonly SubbandSize _imageSize;

//        private readonly LinkedList<Spiht3DPixel> _lip;
//        private readonly LinkedList<Spiht3DSet> _lis;
//        private readonly LinkedList<Spiht3DPixel> _lsp;
//        private readonly List<SubbandSize> _subbandSizes;

//        private readonly ArithmeticModel _thresholdModel;

//        private long _bitCount;
//        private long _budget;
//        private ArithmeticDecoder _decoder;
//        private int _threshold;

//        public Spiht3DDecoder(IntPtr image, int width, int height, int depth)
//        {
//            _coefficients = (short*)image.ToPointer();
//            for (int i = 0; i < width * height * depth; i++)
//            {
//                *(_coefficients + i) = 0;
//            }

//            _imageSize = new SubbandSize { Width = width, Height = height, Depth = depth };

//            _subbandSizes = SpihtHelpers.CalculateSubbands(width, height, depth);

//            _threshold = SpihtHelpers.AbsoluteMax((short*)image.ToPointer(), width * height * depth);

//            _lsp = new LinkedList<Spiht3DPixel>();
//            _lip = new LinkedList<Spiht3DPixel>();
//            _lis = new LinkedList<Spiht3DSet>();

//            _thresholdModel = new ArithmeticModel(4096);
//            _bitModel = new ArithmeticModel(2, true);
//        }

//        public void Decode(ArithmeticDecoder decoder, long budget)
//        {
//            _decoder = decoder;
//            _budget = budget;

//            Init();

//            int thresholdBits = _decoder.DecodeSymbol(_thresholdModel);
//            Console.WriteLine(thresholdBits);

//            for (; thresholdBits >= 0; thresholdBits--)
//            {
//                _threshold = 1 << thresholdBits;

//                if (!SortingPass1() || (_decoder.EofFlag))
//                {
//                    break;
//                }
//                if (!SortingPass2() || (_decoder.EofFlag))
//                {
//                    break;
//                }
//                if (!RefinementPass() || (_decoder.EofFlag))
//                {
//                    break;
//                }
//            }

//            EndDecoding();
//        }

//        private void Init()
//        {
//            _lip.Clear();
//            _lsp.Clear();
//            _lis.Clear();

//            SubbandSize lowestSubband = _subbandSizes.Last();

//            for (byte band = 0; band < lowestSubband.Depth; band++)
//            {
//                for (short row = 0; row < lowestSubband.Height; row++)
//                {
//                    for (short column = 0; column < lowestSubband.Width; column++)
//                    {
//                        _lip.AddLast(new Spiht3DPixel { X = column, Y = row, Z = band });

//                        // Not all of them roots
//                        if ((row & 0x1) != 0 || (column & 0x1) != 0 || (band & 0x1) != 0)
//                        {
//                            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = column, Y = row, Z = band });
//                        }
//                    }
//                }
//            }
//        }

//        private bool SortingPass1()
//        {
//            LinkedListNode<Spiht3DPixel> lipCurrent = _lip.First;

//            while (lipCurrent != null)
//            {
//                if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                    return false;

//                int offset = (lipCurrent.Value.Z * _imageSize.Height + lipCurrent.Value.Y) * _imageSize.Width +
//                             lipCurrent.Value.X;

//                int symbol = _decoder.DecodeSymbol(_bitModel);

//                if (symbol == 1)
//                {
//                    if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                        return false;

//                    symbol = _decoder.DecodeSymbol(_bitModel);

//                    if (symbol == 1)
//                    {
//                        *(_coefficients + offset) = (short)_threshold;
//                    }
//                    else
//                    {
//                        *(_coefficients + offset) = (short)-_threshold;
//                    }

//                    if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                        return false;

//                    _lsp.AddLast(lipCurrent.Value);

//                    LinkedListNode<Spiht3DPixel> lipNext = lipCurrent.Next;
//                    _lip.Remove(lipCurrent);
//                    lipCurrent = lipNext;
//                }
//                else
//                {
//                    lipCurrent = lipCurrent.Next;
//                }
//            }

//            if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                return false;

//            return true;
//        }

//        private bool SortingPass2()
//        {
//            LinkedListNode<Spiht3DSet> lisCurrent = _lis.First;

//            while (lisCurrent != null)
//            {
//                if (lisCurrent.Value.Type == SetType.Descendent)
//                {
//                    if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                        return false;

//                    int symbol = _decoder.DecodeSymbol(_bitModel);

//                    if (symbol == 1)
//                    {
//                        ReconstructD(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

//                        LinkedListNode<Spiht3DSet> lisNext = lisCurrent.Next;
//                        _lis.Remove(lisCurrent);
//                        lisCurrent = lisNext;
//                    }
//                    else
//                    {
//                        lisCurrent = lisCurrent.Next;
//                    }
//                }
//                else
//                {
//                    if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                        return false;

//                    int symbol = _decoder.DecodeSymbol(_bitModel);


//                    if (symbol == 1)
//                    {
//                        ReconstructG(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

//                        LinkedListNode<Spiht3DSet> lisNext = lisCurrent.Next;
//                        _lis.Remove(lisCurrent);
//                        lisCurrent = lisNext;
//                    }
//                    else
//                    {
//                        lisCurrent = lisCurrent.Next;
//                    }
//                }
//            }

//            if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                return false;

//            return true;
//        }

//        private bool RefinementPass()
//        {
//            LinkedListNode<Spiht3DPixel> lspCurrent = _lsp.First;

//            while (lspCurrent != null)
//            {
//                if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                    return false;

//                int offset = (lspCurrent.Value.Z * _imageSize.Height + lspCurrent.Value.Y) * _imageSize.Width +
//                             lspCurrent.Value.X;

//                var value = *(_coefficients + offset);
//                value = Math.Abs(value);

//                if (value >= 2*   _threshold)
//                {
//                    int symbol = _decoder.DecodeSymbol(_bitModel);
//                    if (symbol == 1)
//                    {
//                        if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                            return false;

//                        if (*(_coefficients + offset) > 0)
//                            *(_coefficients + offset) += (short) _threshold;
//                        else
//                            *(_coefficients + offset) -= (short)  _threshold;
//                    }
//                }

//                lspCurrent = lspCurrent.Next;
//            }

//            if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                return false;

//            return true;
//        }

//        private void EndDecoding()
//        {
//            _decoder.Done();

//            _lip.Clear();
//            _lsp.Clear();
//            _lis.Clear();
//        }

//        private void ReconstructD(short x, short y, byte z)
//        {
//            SubbandSize lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
//                    SubReconstructD((short)(x * 2), (short)(y * 2), (byte)(z * 2));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                SubReconstructD((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubReconstructD(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubReconstructD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubReconstructD(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubReconstructD((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubReconstructD(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubReconstructD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                                (byte)(z + lowestSubband.Depth - 1));
//            }

//            if (4 * x < _imageSize.Width && 4 * y < _imageSize.Height && 4 * z < _imageSize.Depth)
//            {
//                _lis.AddLast(new Spiht3DSet { Type = SetType.Grandchildren, X = x, Y = y, Z = z });
//            }
//        }

//        private void SubReconstructD(short x, short y, byte z)
//        {
//            DecodeSignificance(x, y, z);
//            DecodeSignificance((short)(x + 1), y, z);
//            DecodeSignificance(x, (short)(y + 1), z);
//            DecodeSignificance((short)(x + 1), (short)(y + 1), z);
//            DecodeSignificance(x, y, (byte)(z + 1));
//            DecodeSignificance((short)(x + 1), y, (byte)(z + 1));
//            DecodeSignificance(x, (short)(y + 1), (byte)(z + 1));
//            DecodeSignificance((short)(x + 1), (short)(y + 1), (byte)(z + 1));
//        }

//        private void DecodeSignificance(short x, short y, byte z)
//        {
//            if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                return;

//            int offset = (z * _imageSize.Height + y) * _imageSize.Width + x;

//            if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                return;

//            int symbol = _decoder.DecodeSymbol(_bitModel);

//            if (symbol == 1)
//            {
//                if (_decoder.TotalBits >= _budget || _decoder.EofFlag)
//                    return;

//                symbol = _decoder.DecodeSymbol(_bitModel);

//                if (symbol == 1)
//                {
//                    *(_coefficients + offset) = (short)_threshold;
//                }
//                else
//                {
//                    *(_coefficients + offset) = (short)-_threshold;
//                }

//                _lsp.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
//            }
//            else
//            {
//                _lip.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
//            }
//        }

//        private void ReconstructG(short x, short y, byte z)
//        {
//            SubbandSize lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
//                    SubReconstructG((short)(x * 2), (short)(y * 2), (byte)(z * 2));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                SubReconstructG((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubReconstructG(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubReconstructG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubReconstructG(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubReconstructG((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubReconstructG(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubReconstructG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                                (byte)(z + lowestSubband.Depth - 1));
//            }
//        }

//        private void SubReconstructG(short x, short y, byte z)
//        {
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = y, Z = z });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short)(x + 1), Y = y, Z = z });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = (short)(y + 1), Z = z });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short)(x + 1), Y = (short)(y + 1), Z = z });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = y, Z = (byte)(z + 1) });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short)(x + 1), Y = y, Z = (byte)(z + 1) });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = (short)(y + 1), Z = (byte)(z + 1) });
//            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short)(x + 1), Y = (short)(y + 1), Z = (byte)(z + 1) });
//        }
//    }
//}