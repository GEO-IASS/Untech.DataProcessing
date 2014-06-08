using System.IO;

namespace Untech.DataProcessing.Compression.Arithmetic
{
    public class ArithmeticCoder
    {
        private int _lowBound;
        private int _highBound;
        private int _followBits;
        private byte _buffer;
        private int _bitsToGo;
        private int _totalBits;

        public ArithmeticCoder(Stream stream)
        {
            OutputStream = stream;
            Init();
        }

        public Stream OutputStream { get; private set; }

        private void Init()
        {
            _bitsToGo = 8;
            _lowBound = 0;
            _highBound = ArithmeticCodecConstants.TopValue;
            _followBits = 0;
            _buffer = 0;
            _totalBits = 0;
        }

        private void OutputBit(bool bit)
        {
            _buffer >>= 1;

            if (bit) _buffer |= 0x80;

            _bitsToGo -= 1;
            _totalBits += 1;

            if (_bitsToGo != 0) return;

            OutputStream.WriteByte(_buffer);

            _bitsToGo = 8;
        }

        private void BitPlusFollow(bool bit)
        {
            OutputBit(bit);

            while (_followBits > 0)
            {
                OutputBit(!bit);
                _followBits -= 1;
            }
        }

        public void Done()
        {
            _followBits += 1;
            BitPlusFollow(_lowBound >= ArithmeticCodecConstants.FirstQuarterValue);

            OutputStream.WriteByte((byte)(_buffer >> _bitsToGo));
            OutputStream.Close();
        }

        public int TotalBits { get { return _totalBits; } }

        public void Encode(int d, int dBits)
        {
            for (int m = 1 << (dBits - 1); m > 0; m >>= 1)
            {
                OutputBit((d & m) != 0);
            }
        }

        public void EncodeSymbol(ArithmeticModel model, int symbol)
        {
            int range = (_highBound - _lowBound) + 1;
            _highBound = _lowBound + (range * model.CumulativeFrequency[symbol]) / model.CumulativeFrequency[0] - 1;
            _lowBound = _lowBound + (range * model.CumulativeFrequency[symbol + 1]) / model.CumulativeFrequency[0];

            for (; ; )
            {
                if (_highBound < ArithmeticCodecConstants.HalfValue)
                {
                    BitPlusFollow(false);
                }
                else if (_lowBound >= ArithmeticCodecConstants.HalfValue)
                {
                    BitPlusFollow(true);
                    _lowBound -= ArithmeticCodecConstants.HalfValue;
                    _highBound -= ArithmeticCodecConstants.HalfValue;
                }
                else if (_lowBound >= ArithmeticCodecConstants.FirstQuarterValue && _highBound < ArithmeticCodecConstants.ThirdQuarterValue)
                {
                    _followBits += 1;
                    _lowBound -= ArithmeticCodecConstants.FirstQuarterValue;
                    _highBound -= ArithmeticCodecConstants.FirstQuarterValue;
                }
                else
                    break;

                _lowBound = 2 * _lowBound;
                _highBound = 2 * _highBound + 1;
            }

            if (model.Adaptive)
                model.UpdateModel(symbol);
        }

    }
}