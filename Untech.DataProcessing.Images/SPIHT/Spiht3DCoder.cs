using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Untech.DataProcessing.Compression.Arithmetic;

namespace Untech.DataProcessing.Images.SPIHT
{
    public unsafe class Spiht3DCoder
    {
        private ArithmeticCoder _coder;

        private readonly SubbandSize _imageSize;

        private readonly List<SubbandSize> _subbandSizes;

        private readonly LinkedList<Spiht3DPixel> _lsp;
        private readonly LinkedList<Spiht3DPixel> _lip;
        private readonly LinkedList<Spiht3DSet> _lis;

        private int _threshold;

        private long _budget;

        private readonly short* _coefficients;
        private readonly short* _quantization;

        private readonly ArithmeticModel _thresholdModel;
        private readonly ArithmeticModel _bitModel;

        private long _bitCount;

        public Spiht3DCoder(IntPtr image, int width, int height, int depth)
        {
            _coefficients = (short*)image.ToPointer();

            

            _quantization = (short*)Marshal.AllocHGlobal(width * height * depth * 2);
           
            for (int i = 0; i < width * height * depth; i++)
            {
                *(_quantization + i) = 0;
            }

            _imageSize = new SubbandSize { Width = width, Height = height, Depth = depth };

            _subbandSizes = SpihtHelpers.CalculateSubbands(width, height, depth);

            _threshold = SpihtHelpers.AbsoluteMax((short*)image.ToPointer(), width * height * depth);

            _lsp = new LinkedList<Spiht3DPixel>();
            _lip = new LinkedList<Spiht3DPixel>();
            _lis = new LinkedList<Spiht3DSet>();

            _thresholdModel = new ArithmeticModel(4096);
            _bitModel = new ArithmeticModel(2, true);
        }

        public short* Quantization
        {
            get { return _quantization; }
        }

        public void Encode(ArithmeticCoder coder, long budget)
        {
            _coder = coder;
            _budget = budget;

            Init();

            var thresholdBits = (int)Math.Floor(Math.Log(_threshold) / Math.Log(2) + 0.00001);
            
            _coder.EncodeSymbol(_thresholdModel, thresholdBits);

            _threshold = 1 << thresholdBits;

            for (; thresholdBits >= 0; thresholdBits--)
            {
                if (!SortingPass1())
                    break;
                if (!SortingPass2())
                    break;
                if (!RefinementPass())
                    break;

                _threshold >>= 1;

                GC.Collect();
            }

            EndEncoding();
        }

        private void Init()
        {
            _lip.Clear();
            _lsp.Clear();
            _lis.Clear();

            var lowestSubband = _subbandSizes.Last();

            for (byte band = 0; band < lowestSubband.Depth; band++)
            {
                for (short row = 0; row < lowestSubband.Height; row++)
                {
                    for (short column = 0; column < lowestSubband.Width; column++)
                    {
                        _lip.AddLast(new Spiht3DPixel { X = column, Y = row, Z = band });

                        // Not all of them roots
                        if ((row & 0x1) != 0 || (column & 0x1) != 0 || (band & 0x1) != 0)
                        {
                            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = column, Y = row, Z = band });
                        }
                    }
                }
            }
        }

        private bool SortingPass1()
        {
            var lipCurrent = _lip.First;

            while (lipCurrent != null)
            {
                var offset = (lipCurrent.Value.Z * _imageSize.Height + lipCurrent.Value.Y) * _imageSize.Width +
                             lipCurrent.Value.X;

                var value = *(_coefficients + offset);
                if (Math.Abs(value) >= _threshold)
                {
                    Output(true, _bitModel);
                    _bitCount++;

                    if (_coder.TotalBits >= _budget)
                        return false;

                    Output(value > 0, _bitModel);
                    _bitCount++;
                    *(Quantization + offset) = (short)_threshold;

                    if (_coder.TotalBits >= _budget)
                        return false;

                    _lsp.AddLast(lipCurrent.Value);

                    var lipNext = lipCurrent.Next;
                    _lip.Remove(lipCurrent);
                    lipCurrent = lipNext;
                }
                else
                {
                    Output(false, _bitModel);
                    _bitCount++;

                    if (_coder.TotalBits >= _budget)
                        return false;

                    lipCurrent = lipCurrent.Next;
                }
            }

            if (_coder.TotalBits >= _budget)
                return false;

            return true;
        }

        private bool SortingPass2()
        {
            var lisCurrent = _lis.First;

            while (lisCurrent != null)
            {
                if (lisCurrent.Value.Type == SetType.Descendent)
                {
                    if (ExamineD(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z))
                    {
                        Output(true, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;

                        DecomposeD(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

                        var lisNext = lisCurrent.Next;
                        _lis.Remove(lisCurrent);
                        lisCurrent = lisNext;
                    }
                    else
                    {
                        Output(false, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;

                        lisCurrent = lisCurrent.Next;
                    }
                }
                else
                {
                    if (ExamineG(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z))
                    {
                        Output(true, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;

                        DecomposeG(lisCurrent.Value.X, lisCurrent.Value.Y, lisCurrent.Value.Z);

                        var lisNext = lisCurrent.Next;
                        _lis.Remove(lisCurrent);
                        lisCurrent = lisNext;
                    }
                    else
                    {
                        Output(false, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;

                        lisCurrent = lisCurrent.Next;
                    }
                }
            }

            if (_coder.TotalBits >= _budget)
                return false;

            return true;
        }

        private bool RefinementPass()
        {
            var lspCurrent = _lsp.First;

            while (lspCurrent != null)
            {
                var offset = (lspCurrent.Value.Z * _imageSize.Height + lspCurrent.Value.Y) * _imageSize.Width +
                             lspCurrent.Value.X;

                var value = *(_coefficients + offset);
                value = Math.Abs(value);

                var quant = *(_quantization + offset);

                if (value >= 2* _threshold)
                {

                    if (value >= (quant + _threshold) && value < (quant + 2*_threshold))
                    {
                        Output(true, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;

                        *(_quantization + offset) += (short)_threshold;
                    }
                    else
                    //if (value >= quant && value < (quant + _threshold))
                    {
                        Output(false, _bitModel);
                        _bitCount++;

                        if (_coder.TotalBits >= _budget)
                            return false;
                    }

                    
                }

                lspCurrent = lspCurrent.Next;
            }

            if (_coder.TotalBits >= _budget)
                return false;

            return true;
        }

        private void EndEncoding()
        {
            _coder.Done();

            _lip.Clear();
            _lsp.Clear();
            _lis.Clear();

            //Marshal.FreeHGlobal(new IntPtr(_quantization));
        }


        private bool ExamineD(short x, short y, byte z)
        {
            var lowestSubband = _subbandSizes.Last();

            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
            {
                return ScanD(x, y, z);
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
            {
                return LowestScanD((short) (x + lowestSubband.Width - 1), y, z);
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                return LowestScanD(x, (short) (y + lowestSubband.Height - 1), z);
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                return LowestScanD((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1), z);
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                return LowestScanD(x, y, (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                return LowestScanD((short) (x + lowestSubband.Width - 1), y, (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                return LowestScanD(x, (short) (y + lowestSubband.Height - 1), (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                return LowestScanD((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1),
                                   (byte) (z + lowestSubband.Depth - 1));
            }
            return false;
        }

        private bool LowestScanD(short x, short y, byte z)
        {
            for (int dZ = z; dZ < z + 2; dZ++)
                for (int dY = y; dY < y + 2; dY++)
                    for (int dX = x; dX < x + 2; dX++)
                    {
                        var offset = (dZ * _imageSize.Height + dY) * _imageSize.Width + dX;
                        var value = *(_coefficients + offset);
                        value = Math.Abs(value);

                        if (value >= _threshold)
                            return true;
                    }

            for (byte dZ = z; dZ < z + 2; dZ++)
                for (short dY = y; dY < y + 2; dY++)
                    for (short dX = x; dX < x + 2; dX++)
                    {
                        if (ScanD(dX, dY, dZ))
                            return true;
                    }

            return false;
        }


        private bool ScanD(short x, short y, byte z)
        {
            // note: what if subband sizes are: 
            // L5 % 2 == 0 
            // L4 % 2 == 0 
            // L3 % 2 == 1
            // L2 % 2 == 0 
            // L1 % 2 == 0 ?
            // Wrong childs?
            int column = x;
            int row = y;
            int depth = z;
            int length = 1;

            while ((2 * column < _imageSize.Width) &&
                   (2 * row < _imageSize.Height) &&
                   (2 * depth < _imageSize.Depth))
            {
                row <<= 1;
                column <<= 1;
                depth <<= 1;
                length <<= 1;

                for (int dZ = depth; dZ < depth + length; dZ++)
                    for (int dY = row; dY < row + length; dY++)
                        for (int dX = column; dX < column + length; dX++)
                        {
                            var offset = (dZ * _imageSize.Height + dY) * _imageSize.Width + dX;
                            var value = *(_coefficients + offset);
                            value = Math.Abs(value);

                            if (value >= _threshold)
                                return true;
                        }
            }

            return false;
        }

        private bool ExamineG(short x, short y, byte z)
        {
            var lowestSubband = _subbandSizes.Last();

            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
            {
                return ScanG((short) (x * 2), (short) (y * 2), (byte) (z * 2));
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
            {
                return ScanG((short) (x + lowestSubband.Width - 1), y, z);
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                return ScanG(x, (short) (y + lowestSubband.Height - 1), z);
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                return ScanG((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1), z);
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                return ScanG(x, y, (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                return ScanG((short) (x + lowestSubband.Width - 1), y, (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                return ScanG(x, (short) (y + lowestSubband.Height - 1), (byte) (z + lowestSubband.Depth - 1));
            }
            if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                return ScanG((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1),
                             (byte) (z + lowestSubband.Depth - 1));
            }
            return false;
        }

        private bool ScanG(short x, short y, byte z)
        {
            for (byte dZ = z; dZ < z + 2; dZ++)
                for (short dY = y; dY < y + 2; dY++)
                    for (short dX = x; dX < x + 2; dX++)
                    {
                        if (ScanD(dX, dY, dZ))
                            return true;
                    }

            return false;
        }

        private void DecomposeD(short x, short y, byte z)
        {
            var lowestSubband = _subbandSizes.Last();

            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
            {
                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
                    SubDecomposeD((short) (x  * 2), (short) (y * 2), (byte) (z * 2));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
            {
                SubDecomposeD((short) (x + lowestSubband.Width - 1), y, z);
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                SubDecomposeD(x, (short) (y + lowestSubband.Height - 1), z);
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                SubDecomposeD((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1), z);
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                SubDecomposeD(x, y, (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                SubDecomposeD((short) (x + lowestSubband.Width - 1), y, (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                SubDecomposeD(x, (short) (y + lowestSubband.Height - 1), (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                SubDecomposeD((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1),
                              (byte) (z + lowestSubband.Depth - 1));
            }

            if (2 * x < _imageSize.Width/2 && 2 * y < _imageSize.Height/2 && 2 * z < _imageSize.Depth/2)
            {
                _lis.AddLast(new Spiht3DSet { Type = SetType.Grandchildren, X = x, Y = y, Z = z });
            }
        }

        private void SubDecomposeD(short x, short y, byte z)
        {
            EncodeSignificance(x, y, z);
            EncodeSignificance((short) (x + 1), y, z);
            EncodeSignificance(x, (short) (y + 1), z);
            EncodeSignificance((short) (x + 1), (short) (y + 1), z);
            EncodeSignificance(x, y, (byte) (z + 1));
            EncodeSignificance((short) (x + 1), y, (byte) (z + 1));
            EncodeSignificance(x, (short) (y + 1), (byte) (z + 1));
            EncodeSignificance((short) (x + 1), (short) (y + 1), (byte) (z + 1));
        }

        private void EncodeSignificance(short x, short y, byte z)
        {
            if (_coder.TotalBits >= _budget)
                return;

            var offset = (z * _imageSize.Height + y) * _imageSize.Width + x;

            var value = *(_coefficients + offset);

            if (Math.Abs(value) >=  _threshold)
            {
                Output(true, _bitModel);
                _bitCount++;

                if (_coder.TotalBits >= _budget)
                    return;

                Output(value > 0, _bitModel);
                _bitCount++;
                *(_quantization + offset) = (short)_threshold;

                if (_coder.TotalBits >= _budget)
                    return;

                _lsp.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
            }
            else
            {
                Output(false, _bitModel);
                _bitCount++;

                if (_coder.TotalBits >= _budget)
                    return;

                _lip.AddLast(new Spiht3DPixel { X = x, Y = y, Z = z });
            }
        }

        private void DecomposeG(short x, short y, byte z)
        {
            var lowestSubband = _subbandSizes.Last();

            if (x >= lowestSubband.Width || y >= lowestSubband.Height || z >= lowestSubband.Depth)
            {
                if (2 * x < _imageSize.Width && 2 * y < _imageSize.Height && 2 * z < _imageSize.Depth)
                    SubDecomposeG((short) (x * 2), (short) (y * 2), (byte) (z * 2));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 0)
            {
                SubDecomposeG((short) (x + lowestSubband.Width - 1), y, z);
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                SubDecomposeG(x, (short) (y + lowestSubband.Height - 1), z);
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 0)
            {
                SubDecomposeG((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1), z);
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                SubDecomposeG(x, y, (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 0 && (z & 0x1) == 1)
            {
                SubDecomposeG((short) (x + lowestSubband.Width - 1), y, (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 0 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                SubDecomposeG(x, (short) (y + lowestSubband.Height - 1), (byte) (z + lowestSubband.Depth - 1));
            }
            else if ((x & 0x1) == 1 && (y & 0x1) == 1 && (z & 0x1) == 1)
            {
                SubDecomposeG((short) (x + lowestSubband.Width - 1), (short) (y + lowestSubband.Height - 1),
                              (byte) (z + lowestSubband.Depth - 1));
            }
        }

        private void SubDecomposeG(short x, short y, byte z)
        {
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = y, Z = z });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short) (x + 1), Y = y, Z = z });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = (short) (y + 1), Z = z });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short) (x + 1), Y = (short) (y + 1), Z = z });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = y, Z = (byte) (z + 1) });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short) (x + 1), Y = y, Z = (byte) (z + 1) });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = x, Y = (short) (y + 1), Z = (byte) (z + 1) });
            _lis.AddLast(new Spiht3DSet { Type = SetType.Descendent, X = (short) (x + 1), Y = (short) (y + 1), Z = (byte) (z + 1) });
        }

        private void Output(bool bit, ArithmeticModel model)
        {
            _coder.EncodeSymbol(model, bit ? 1 : 0);
        }
    }
}