using System;

namespace Untech.DataProcessing.Images.Wavelet
{
    public interface IWaveletTransformation
    {
        int ElementSize { get; }

        void Encode1D(IntPtr input, int size, int inputStride, int inputOffset,
                      IntPtr output, int outputStride = -1, int outputOffset = -1);

        void Encode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset,
                      IntPtr output, int outputWidthStride = -1, int outputOffset = -1);

        void Encode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride, int inputOffset,
                      IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1, int outputOffset = -1);

        void Decode1D(IntPtr input, int size, int inputStride, int inputOffset,
                      IntPtr output, int outputStride = -1, int outputOffset = -1);

        void Decode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset,
                      IntPtr output, int outputWidthStride = -1, int outputOffset = -1);

        void Decode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride, int inputOffset,
                      IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1, int outputOffset = -1);
    }
}