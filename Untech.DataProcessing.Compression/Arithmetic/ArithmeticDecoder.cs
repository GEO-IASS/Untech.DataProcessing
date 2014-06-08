using System.IO;

namespace Untech.DataProcessing.Compression.Arithmetic
{
    public class ArithmeticDecoder
    {
        private int _value;
        private int _lowBound;
        private int _highBound;
        private byte _buffer;
        private int _bitsToGo;
        private int _totalBits;
        private int _garbageBits;
        private bool _eofFlag;

        public ArithmeticDecoder(Stream stream)
        {
            InputStream = stream;
            Init();
        }

        public Stream InputStream { get; private set; }

        public bool EofFlag { get { return _eofFlag; } }

        private void Init()
        {
            int i;

            _bitsToGo = 0;
            _totalBits = 0;
            _garbageBits = 0;
            _value = 0;

            for (i = 1; i <= ArithmeticCodecConstants.CodeValueBits; i++)
            {
                _value = 2 * _value + (InputBit() ? 1 : 0);
            }

            _lowBound = 0;
            _highBound = ArithmeticCodecConstants.TopValue;
            _eofFlag = false;
        }

        private bool InputBit()
        {
            bool t;

            // If the buffer is empty, read in 8 bits.
            if (_bitsToGo == 0)
            {
                var readed = InputStream.ReadByte();
                _buffer = (byte)readed;

                // If the end of file is met, print error message.
                if (readed == -1)
                {
                    //_eofFlag = true;
                    _garbageBits += 1;

                    if (_garbageBits > ArithmeticCodecConstants.CodeValueBits - 2)
                        _eofFlag = true;
                    //throw new IOException("Arithmetic decoder bad input file");
                }

                _bitsToGo = 8;
            }

            t = (_buffer & 1) == 1;		// Get the LSB
            _buffer >>= 1;
            _bitsToGo -= 1;
            _totalBits += 1;

            return t;
        }

        public void Done()
        {
            InputStream.Close();
        }

        public int TotalBits { get { return _totalBits; } }

        public int DecodeBinary(int dBits)
        {
            int symbol = 0;
            int m;

            for (m = 1 << (dBits); m > 0; m >>= 1)
            {
                if (InputBit())
                    symbol |= m;
            }

            return symbol;
        }

        public int DecodeSymbol(ArithmeticModel model)
        {
            int range;
            int cum;
            int symbol;

            range = (_highBound - _lowBound) + 1;
            cum = (((_value - _lowBound) + 1) * model.CumulativeFrequency[0] - 1) / range;

            for (symbol = 0; model.CumulativeFrequency[symbol + 1] > cum; symbol++)
                /* do nothing */
                ;

            _highBound = _lowBound + (range * model.CumulativeFrequency[symbol]) / model.CumulativeFrequency[0] - 1;
            _lowBound = _lowBound + (range * model.CumulativeFrequency[symbol + 1]) / model.CumulativeFrequency[0];

            for (; ; )
            {
                if (_highBound < ArithmeticCodecConstants.HalfValue)
                {
                    /* do nothing */
                }
                else if (_lowBound >= ArithmeticCodecConstants.HalfValue)
                {
                    _value -= ArithmeticCodecConstants.HalfValue;
                    _lowBound -= ArithmeticCodecConstants.HalfValue;
                    _highBound -= ArithmeticCodecConstants.HalfValue;
                }
                else if (_lowBound >= ArithmeticCodecConstants.FirstQuarterValue &&
                         _highBound < ArithmeticCodecConstants.ThirdQuarterValue)
                {
                    _value -= ArithmeticCodecConstants.FirstQuarterValue;
                    _lowBound -= ArithmeticCodecConstants.FirstQuarterValue;
                    _highBound -= ArithmeticCodecConstants.FirstQuarterValue;
                }
                else
                    break;

                _lowBound = 2 * _lowBound;
                _highBound = 2 * _highBound + 1;
                _value = 2 * _value + (InputBit() ? 1 : 0);
            }

            if (model.Adaptive)
                model.UpdateModel(symbol);

            return symbol;
        }
    }
}