namespace Untech.DataProcessing.Compression.Arithmetic
{
    public static class ArithmeticCodecConstants
    {
        public static readonly int CodeValueBits = 16;

        public static readonly int TopValue = (1 << CodeValueBits) - 1;

        public static readonly int FirstQuarterValue = (TopValue / 4 + 1);

        public static readonly int HalfValue = (2 * FirstQuarterValue);

        public static readonly int ThirdQuarterValue = (3 * FirstQuarterValue);

        public static readonly int MaxFrequency = (1 << (CodeValueBits - 2)) - 1; // 16383;
    }
}