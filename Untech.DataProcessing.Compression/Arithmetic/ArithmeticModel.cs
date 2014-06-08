using System.IO;

namespace Untech.DataProcessing.Compression.Arithmetic
{
    public class ArithmeticModel
    {
        public ArithmeticModel(int numberOfSymbols, bool adaptive = false)
        {
            Frequency = new short[numberOfSymbols];
            CumulativeFrequency = new short[numberOfSymbols + 1];

            Adaptive = adaptive;

            CumulativeFrequency[numberOfSymbols] = 0;
            for (int i = 0; i < numberOfSymbols; i++)
            {
                Frequency[i] = 1;
                CumulativeFrequency[i] = (short)(numberOfSymbols - i);
            }
        }

        public ArithmeticModel(short[] initialFrequency, bool adaptive = false)
        {
            int numberOfSymbols = initialFrequency.Length;

            Frequency = new short[numberOfSymbols];
            CumulativeFrequency = new short[numberOfSymbols + 1];
            Adaptive = adaptive;

            CumulativeFrequency[numberOfSymbols] = 0;
            for (int i = numberOfSymbols - 1; i >= 0; i--)
            {
                Frequency[i] = initialFrequency[i];
                CumulativeFrequency[i] = (short) (CumulativeFrequency[i + 1] + Frequency[i]);
            }

            if (CumulativeFrequency[0] > ArithmeticCodecConstants.MaxFrequency)
                throw new InvalidDataException("Arithmetic coder model max frequency exceeded");

        }

        public int NumberOfSymbols { get { return Frequency.Length; } }

        public short[] Frequency { get; private set; }

        public short[] CumulativeFrequency { get; private set; }

        public bool Adaptive { get; set; }

        public void UpdateModel(int symbol)
        {
            if (CumulativeFrequency[0] == ArithmeticCodecConstants.MaxFrequency)
            {
                short accumulator = 0;
                CumulativeFrequency[NumberOfSymbols] = 0;
                for (int i = NumberOfSymbols - 1; i >= 0; i--)
                {
                    Frequency[i] = (short)((Frequency[i] + 1) /2);
                    accumulator += Frequency[i];
                    CumulativeFrequency[i] = accumulator;
                }
            }

            Frequency[symbol] += 1;
            for (int i = symbol; i >= 0; i--)
                CumulativeFrequency[i] += 1;
        }
    }
}