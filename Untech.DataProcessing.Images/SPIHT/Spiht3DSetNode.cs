namespace Untech.DataProcessing.Images.SPIHT
{
    public enum SetType : byte
    {
        Descendent = 1,
        Grandchildren
    }

    public struct Spiht3DSet
    {
        public short X { get; set; }
        public short Y { get; set; }
        public byte Z { get; set; }
        public SetType Type { get; set; }
    }
}