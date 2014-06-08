namespace Untech.DataProcessing.Images
{
    public enum BandType
    {
        /// <summary>
        /// Image Dimensions: bands-M-N
        /// </summary>
        BandInterleavedByPixel,
        /// <summary>
        /// Image Dimensions: M-bands-N
        /// </summary>
        BandInterleavedByLine,
        /// <summary>
        /// Image Dimensions: M-N-bands
        /// </summary>
        BandSequential,
    }
}