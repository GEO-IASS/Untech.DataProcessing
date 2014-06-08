//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.InteropServices;
//using Untech.DataProcessing.Compression.Arithmetic;
//using Untech.DataProcessing.Images.SPIHT;

//namespace Untech.DataProcessing.Images.NLS
//{
//    public unsafe class NLS3DCoder
//    {
//        private ArithmeticCoder _coder;

//        private readonly short* _image;
//        private readonly short* _descedants;
//        private readonly short* _grandchildrens;
//        private readonly byte* _mark;

//        private readonly SubbandSize _imageSize;
//        private readonly SubbandSize _descSize;
//        private readonly SubbandSize _grandSize;

//        private readonly List<SubbandSize> _subbandSizes;

//        private int _threshold;

//        private long _budget;

//        private readonly ArithmeticModel _thresholdModel;
//        private readonly ArithmeticModel _bitModel;

//        private long _bitCount;

//        public NLS3DCoder(SubbandSize imageSize)
//        {
//            _imageSize = imageSize;
//            _descSize = new SubbandSize
//                {
//                    Width = imageSize.Width / 2,
//                    Height = imageSize.Height / 2,
//                    Depth = imageSize.Depth / 2
//                };

//            _grandSize = new SubbandSize
//            {
//                Width = imageSize.Width / 4,
//                Height = imageSize.Height / 4,
//                Depth = imageSize.Depth / 4
//            };

//            var size = imageSize.Width * imageSize.Height * imageSize.Depth;
//            var descedantsSize = size * sizeof(short) / 8;
//            var grandchildrensSize = size * sizeof(short) / 64;
//            size = size / 2;

//            _descedants = (short*)Marshal.AllocHGlobal(descedantsSize);
//            _grandchildrens = (short*)Marshal.AllocHGlobal(grandchildrensSize);
//            _mark = (byte*)Marshal.AllocHGlobal(size);

//            Reset();

//            _subbandSizes = SpihtHelpers.CalculateSubbands(imageSize.Width, imageSize.Height, imageSize.Depth);

//            _thresholdModel = new ArithmeticModel(4096);
//            _bitModel = new ArithmeticModel(2, true);
//        }

//        public void Reset()
//        {
//            var size = _imageSize.Width * _imageSize.Height * _imageSize.Depth;

//            var descedantsSize = size * sizeof(short) / 8;
//            var grandchildrensSize = size * sizeof(short) / 64;
//            size = size / 2;

//            for (int i = 0; i < size; i++)
//                *(_mark + i) = 0;

//            for (int i = 0; i < descedantsSize; i++)
//                *(_descedants + i) = 0;

//            for (int i = 0; i < grandchildrensSize; i++)
//                *(_grandchildrens + i) = 0;
//        }

//        private void ComputeMaximums()
//        {
//            var lowestSubband = _subbandSizes.Last();

//            var width = _imageSize.Width;
//            var height = _imageSize.Height;
//            var depth = _imageSize.Depth;

//            width /= 4;
//            height /= 4;
//            depth /= 4;

//            var computeG = false;

//            while (width > lowestSubband.Width || height > lowestSubband.Height || depth > lowestSubband.Depth)
//            {
//                ComputeSubbandMaximums(width, 0, 0, width, height, depth, computeG);
//                ComputeSubbandMaximums(0, height, 0, width, height, depth, computeG);
//                ComputeSubbandMaximums(0, 0, depth, width, height, depth, computeG);

//                ComputeSubbandMaximums(width, height, 0, width, height, depth, computeG);
//                ComputeSubbandMaximums(0, height, depth, width, height, depth, computeG);
//                ComputeSubbandMaximums(width, 0, depth, width, height, depth, computeG);

//                ComputeSubbandMaximums(width, height, depth, width, height, depth, computeG);

//                computeG = true;

//                width /= 2;
//                height /= 2;
//                depth /= 2;
//            }

//            ComputeLowestSubbandMaximums();
//        }

//        private short ComputeMax(short* array, SubbandSize size, int x, int y, int z)
//        {
//            short temp;

//            temp = Math.Abs(*(array + z * size.Width * size.Height + y * size.Width + x));
//            temp |= Math.Abs(*(array + z * size.Width * size.Height + y * size.Width + x + 1));
//            temp |= Math.Abs(*(array + z * size.Width * size.Height + (y + 1) * size.Width + x));
//            temp |= Math.Abs(*(array + z * size.Width * size.Height + (y + 1) * size.Width + x + 1));
//            temp |= Math.Abs(*(array + (z + 1) * size.Width * size.Height + y * size.Width + x));
//            temp |= Math.Abs(*(array + (z + 1) * size.Width * size.Height + y * size.Width + x + 1));
//            temp |= Math.Abs(*(array + (z + 1) * size.Width * size.Height + (y + 1) * size.Width + x));
//            temp |= Math.Abs(*(array + (z + 1) * size.Width * size.Height + (y + 1) * size.Width + x + 1));

//            return temp;
//        }

//        private void ComputeSubbandMaximums(int x, int y, int z, int width, int height, int depth, bool computeG)
//        {
//            var grandArea = _grandSize.Width * _grandSize.Height;
//            var descArea = _descSize.Width * _descSize.Height;

//            if (computeG)
//            {
//                for (int dz = z; dz < depth + z; dz++)
//                {
//                    for (int dy = y; dy < height + y; dy++)
//                    {
//                        for (int dx = x; dx < width + x; dx++)
//                        {
//                            short max = ComputeMax(_descedants, _descSize, dx * 2, dy * 2, dz * 2);
//                            int offset = dz * grandArea + dy * _grandSize.Width + dx;
//                            *(_grandchildrens + offset) = max;
//                        }
//                    }
//                }
//            }


//            for (int dz = z; dz < depth + z; dz++)
//            {
//                for (int dy = y; dy < height + y; dy++)
//                {
//                    for (int dx = x; dx < width + x; dx++)
//                    {
//                        int offset;
//                        short max = ComputeMax(_image, _imageSize, dx * 2, dy * 2, dz * 2);

//                        if (computeG)
//                        {
//                            offset = dz * grandArea + dy * _grandSize.Width + dx;
//                            max |= *(_grandchildrens + offset);
//                        }

//                        offset = dz * descArea + dy * _descSize.Width + dx;

//                        *(_descedants + offset) = max;
//                    }
//                }
//            }
//        }

//        private short ComputeLowestMax(int x, int y, int z)
//        {
//            var lowestSubband = _subbandSizes.Last();

//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                return ComputeMax(_image, _imageSize, x + lowestSubband.Width - 1, y, z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return ComputeMax(_image, _imageSize, x, (y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return ComputeMax(_image, _imageSize, (x + lowestSubband.Width - 1), (y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return ComputeMax(_image, _imageSize, x, y, (z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return ComputeMax(_image, _imageSize, (x + lowestSubband.Width - 1), y, (z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                return ComputeMax(_image, _imageSize, x, (y + lowestSubband.Height - 1), (z + lowestSubband.Depth - 1));
//            }

//            return 0;
//        }

//        private void ComputeLowestSubbandMaximums()
//        {
//            var lowestSubband = _subbandSizes.Last();
//            var grandArea = _grandSize.Width * _grandSize.Height;
//            var descArea = _descSize.Width * _descSize.Height;

//            for (byte dz = 0; dz < lowestSubband.Depth; dz++)
//            {
//                for (short dy = 0; dy < lowestSubband.Height; dy++)
//                {
//                    for (short dx = 0; dx < lowestSubband.Width; dx++)
//                    {
//                        if ((dy & 0x1) != 0 || (dx & 0x1) != 0 || (dz & 0x1) != 0)
//                        {
//                            int offset;

//                            short max = ComputeLowestMax(dx, dy, dz);

//                            offset = dz * grandArea + dy * _grandSize.Width + dx;
//                            max |= *(_grandchildrens + offset);

//                            offset = dz * descArea + dy * _descSize.Width + dx;

//                            *(_descedants + offset) = max;
//                        }
//                    }
//                }
//            }
//        }

//        private short ComputeMaxThreshold()
//        {
//            var lowestSubband = _subbandSizes.Last();
//            var imageArea = _imageSize.Width * _imageSize.Height;
//            var descArea = _descSize.Width * _descSize.Height;

//            short max = 0;

//            for (byte dz = 0; dz < lowestSubband.Depth; dz++)
//            {
//                for (short dy = 0; dy < lowestSubband.Height; dy++)
//                {
//                    for (short dx = 0; dx < lowestSubband.Width; dx++)
//                    {
//                        int offset;

//                        offset = dz * imageArea + dy * _imageSize.Width + dx;
//                        max |= *(_image + offset);

//                        offset = dz * descArea + dy * _descSize.Width + dx;
//                        max |= *(_descedants + offset);

//                    }
//                }
//            }

//            return max;
//        }


//        public void Encode(ArithmeticCoder coder, long budget)
//        {
//            _coder = coder;
//            _budget = budget;

//            Init();

//            var thresholdBits = (int)Math.Floor(Math.Log(_threshold) / Math.Log(2) + 0.00001);
//            Console.WriteLine(thresholdBits);

//            _coder.EncodeSymbol(_thresholdModel, thresholdBits);

//            _threshold = 1 << thresholdBits;

//            for (; thresholdBits >= 0; thresholdBits--)
//            {
//                if (!SortingPass1())
//                    break;
//                if (!SortingPass2())
//                    break;
//                if (!RefinementPass())
//                    break;

//                _threshold >>= 1;
//            }

//            EndEncoding();
//        }

//        private void Init()
//        {
//            ComputeMaximums();

//            _threshold = ComputeMaxThreshold();

//            var lowestSubband = _subbandSizes.Last();

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
//            var lipCurrent = _lip.First;

//            while (lipCurrent != null)
//            {
//                var offset = (lipCurrent.Value.Z * _imageSize.Height + lipCurrent.Value.Y) * _imageSize.Width +
//                             lipCurrent.Value.X;

//                var value = *(_coefficients + offset);
//                if (Math.Abs(value) >= _threshold)
//                {
//                    Output(true, _bitModel);
//                    _bitCount++;

//                    if (_coder.TotalBits >= _budget)
//                        return false;

//                    Output(value > 0, _bitModel);
//                    _bitCount++;
//                    *(Quantization + offset) = (short)_threshold;

//                    if (_coder.TotalBits >= _budget)
//                        return false;

//                    _lsp.AddLast(lipCurrent.Value);

//                    var lipNext = lipCurrent.Next;
//                    _lip.Remove(lipCurrent);
//                    lipCurrent = lipNext;
//                }
//                else
//                {
//                    Output(false, _bitModel);
//                    _bitCount++;

//                    if (_coder.TotalBits >= _budget)
//                        return false;

//                    lipCurrent = lipCurrent.Next;
//                }
//            }

//            if (_coder.TotalBits >= _budget)
//                return false;

//            return true;
//        }

//        private bool SortingPass2()
//        {
//            var lisCurrent = _lis.First;

//            while (lisCurrent != null)
//            {
//                if (lisCurrent.Value.Type == SetType.Descendent)
//                {
//                    if (ExamineD(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z))
//                    {
//                        Output(true, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;

//                        DecomposeD(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

//                        var lisNext = lisCurrent.Next;
//                        _lis.Remove(lisCurrent);
//                        lisCurrent = lisNext;
//                    }
//                    else
//                    {
//                        Output(false, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;

//                        lisCurrent = lisCurrent.Next;
//                    }
//                }
//                else
//                {
//                    if (ExamineG(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z))
//                    {
//                        Output(true, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;

//                        DecomposeG(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

//                        var lisNext = lisCurrent.Next;
//                        _lis.Remove(lisCurrent);
//                        lisCurrent = lisNext;
//                    }
//                    else
//                    {
//                        Output(false, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;

//                        lisCurrent = lisCurrent.Next;
//                    }
//                }
//            }

//            if (_coder.TotalBits >= _budget)
//                return false;

//            return true;
//        }

//        private bool RefinementPass()
//        {
//            var lspCurrent = _lsp.First;

//            while (lspCurrent != null)
//            {
//                var offset = (lspCurrent.Value.Z * _imageSize.Height + lspCurrent.Value.Y) * _imageSize.Width +
//                             lspCurrent.Value.X;

//                var value = *(_coefficients + offset);
//                value = Math.Abs(value);

//                var quant = *(_quantization + offset);

//                if (value >= 2 * _threshold)
//                {

//                    if (value >= (quant + _threshold) && value < (quant + 2 * _threshold))
//                    {
//                        Output(true, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;

//                        *(_quantization + offset) += (short)_threshold;
//                    }
//                    else
//                    //if (value >= quant && value < (quant + _threshold))
//                    {
//                        Output(false, _bitModel);
//                        _bitCount++;

//                        if (_coder.TotalBits >= _budget)
//                            return false;
//                    }


//                }

//                lspCurrent = lspCurrent.Next;
//            }

//            if (_coder.TotalBits >= _budget)
//                return false;

//            return true;
//        }

//        private void EndEncoding()
//        {
//            _coder.Done();

//            _lip.Clear();
//            _lsp.Clear();
//            _lis.Clear();

//            //Marshal.FreeHGlobal(new IntPtr(_quantization));
//        }


//        private bool ExamineD(short x, short y, byte z)
//        {
//            var lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                return ScanD(x, y, z);
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                return LowestScanD((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return LowestScanD(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return LowestScanD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return LowestScanD(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return LowestScanD((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                return LowestScanD(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                return LowestScanD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                                   (byte)(z + lowestSubband.Depth - 1));
//            }
//            return false;
//        }

//        private bool LowestScanD(short x, short y, byte z)
//        {
//            for (int dZ = z; dZ < z + 2; dZ++)
//                for (int dY = y; dY < y + 2; dY++)
//                    for (int dX = x; dX < x + 2; dX++)
//                    {
//                        var offset = (dZ * _imageSize.Height + dY) * _imageSize.Width + dX;
//                        var value = *(_coefficients + offset);
//                        value = Math.Abs(value);

//                        if (value >= _threshold)
//                            return true;
//                    }

//            for (byte dZ = z; dZ < z + 2; dZ++)
//                for (short dY = y; dY < y + 2; dY++)
//                    for (short dX = x; dX < x + 2; dX++)
//                    {
//                        if (ScanD(dX, dY, dZ))
//                            return true;
//                    }

//            return false;
//        }


//        private bool ScanD(short x, short y, byte z)
//        {
//            // note: what if subband sizes are: 
//            // L5 % 2 == 0 
//            // L4 % 2 == 0 
//            // L3 % 2 == 1
//            // L2 % 2 == 0 
//            // L1 % 2 == 0 ?
//            // Wrong childs?
//            int column = x;
//            int row = y;
//            int depth = z;
//            int length = 1;

//            while ((2 * column < _imageSize.Width) &&
//                   (2 * row < _imageSize.Height) &&
//                   (2 * depth < _imageSize.Depth))
//            {
//                row <<= 1;
//                column <<= 1;
//                depth <<= 1;
//                length <<= 1;

//                for (int dZ = depth; dZ < depth + length; dZ++)
//                    for (int dY = row; dY < row + length; dY++)
//                        for (int dX = column; dX < column + length; dX++)
//                        {
//                            var offset = (dZ * _imageSize.Height + dY) * _imageSize.Width + dX;
//                            var value = *(_coefficients + offset);
//                            value = Math.Abs(value);

//                            if (value >= _threshold)
//                                return true;
//                        }
//            }

//            return false;
//        }

//        private bool ExamineG(short x, short y, byte z)
//        {
//            var lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                return ScanG((short)(x * 2), (short)(y * 2), (byte)(z * 2));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                return ScanG((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return ScanG(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                return ScanG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return ScanG(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                return ScanG((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                return ScanG(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                return ScanG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                             (byte)(z + lowestSubband.Depth - 1));
//            }
//            return false;
//        }

//        private bool ScanG(short x, short y, byte z)
//        {
//            for (byte dZ = z; dZ < z + 2; dZ++)
//                for (short dY = y; dY < y + 2; dY++)
//                    for (short dX = x; dX < x + 2; dX++)
//                    {
//                        if (ScanD(dX, dY, dZ))
//                            return true;
//                    }

//            return false;
//        }

//        private void DecomposeD(short x, short y, byte z)
//        {
//            var lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
//                    SubDecomposeD((short)(x * 2), (short)(y * 2), (byte)(z * 2));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                SubDecomposeD((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubDecomposeD(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubDecomposeD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubDecomposeD(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubDecomposeD((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubDecomposeD(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubDecomposeD((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                              (byte)(z + lowestSubband.Depth - 1));
//            }

//            if (2 * x < _imageSize.Width / 2 && 2 * y < _imageSize.Height / 2 && 2 * z < _imageSize.Depth / 2)
//            {
//                _lis.AddLast(new Spiht3DSet { Type = SetType.Grandchildren, X = x, Y = y, Z = z });
//            }
//        }

//        private void SubDecomposeD(short x, short y, byte z)
//        {
//            EncodeSignificance(x, y, z);
//            EncodeSignificance((short)(x + 1), y, z);
//            EncodeSignificance(x, (short)(y + 1), z);
//            EncodeSignificance((short)(x + 1), (short)(y + 1), z);
//            EncodeSignificance(x, y, (byte)(z + 1));
//            EncodeSignificance((short)(x + 1), y, (byte)(z + 1));
//            EncodeSignificance(x, (short)(y + 1), (byte)(z + 1));
//            EncodeSignificance((short)(x + 1), (short)(y + 1), (byte)(z + 1));
//        }

//        private void EncodeSignificance(short x, short y, byte z)
//        {
//            if (_coder.TotalBits >= _budget)
//                return;

//            var offset = (z * _imageSize.Height + y) * _imageSize.Width + x;

//            var value = *(_coefficients + offset);

//            if (Math.Abs(value) >= _threshold)
//            {
//                Output(true, _bitModel);
//                _bitCount++;

//                if (_coder.TotalBits >= _budget)
//                    return;

//                Output(value > 0, _bitModel);
//                _bitCount++;
//                *(_quantization + offset) = (short)_threshold;

//                if (_coder.TotalBits >= _budget)
//                    return;

//                _lsp.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
//            }
//            else
//            {
//                Output(false, _bitModel);
//                _bitCount++;

//                if (_coder.TotalBits >= _budget)
//                    return;

//                _lip.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
//            }
//        }

//        private void DecomposeG(short x, short y, byte z)
//        {
//            var lowestSubband = _subbandSizes.Last();

//            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
//            {
//                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
//                    SubDecomposeG((short)(x * 2), (short)(y * 2), (byte)(z * 2));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
//            {
//                SubDecomposeG((short)(x + lowestSubband.Width - 1), y, z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubDecomposeG(x, (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
//            {
//                SubDecomposeG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1), z);
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubDecomposeG(x, y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
//            {
//                SubDecomposeG((short)(x + lowestSubband.Width - 1), y, (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubDecomposeG(x, (short)(y + lowestSubband.Height - 1), (byte)(z + lowestSubband.Depth - 1));
//            }
//            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
//            {
//                SubDecomposeG((short)(x + lowestSubband.Width - 1), (short)(y + lowestSubband.Height - 1),
//                              (byte)(z + lowestSubband.Depth - 1));
//            }
//        }

//        private void SubDecomposeG(short x, short y, byte z)
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

//        private void Output(bool bit, ArithmeticModel model)
//        {
//            _coder.EncodeSymbol(model, bit ? 1 : 0);
//        }
//    }
//}