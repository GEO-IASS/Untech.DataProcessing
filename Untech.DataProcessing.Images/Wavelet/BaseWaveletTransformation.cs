using System;
using System.Runtime.InteropServices;
using Untech.DataProcessing.Common.Unmanaged;

namespace Untech.DataProcessing.Images.Wavelet
{
    public abstract class BaseWaveletTransformation : IWaveletTransformation
    {
        public abstract int ElementSize { get; }

        public abstract void Encode1D(IntPtr input, int size, int inputStride, int inputOffset,
                                      IntPtr output, int outputStride = -1, int outputOffset = -1);

        public void Encode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset,
                             IntPtr output, int outputWidthStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            // Encode rows
            for (int row = 0; row < height; row++)
            {
                Encode1D(input, width, 1, inputOffset + row * inputWidthStride,
                         output, 1, outputOffset + row * outputWidthStride);
            }

            IntPtr buffer = Marshal.AllocHGlobal(width*height*ElementSize);
            MemoryAllocator.Copy2D(output, width, height, outputWidthStride, outputOffset, buffer, width, 0, ElementSize);

            // Encode columns
            for (int column = 0; column < width; column++)
            {
                Encode1D(buffer, height, width, column, 
                         output, outputWidthStride, outputOffset + column);
            }

            Marshal.FreeHGlobal(buffer);
        }

        public void Encode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride,
                             int inputOffset, IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputHeightStride == -1)
                outputHeightStride = inputHeightStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            // Encode 2d surface in depth
            for (int surface = 0; surface < depth; surface++)
            {
                Encode2D(input, width, height, inputWidthStride, inputOffset + surface * inputWidthStride * inputHeightStride,
                         output, outputWidthStride, outputOffset + surface * outputWidthStride * outputHeightStride);
            }

            IntPtr buffer = Marshal.AllocHGlobal(width * height * depth * ElementSize);
            MemoryAllocator.Copy3D(output, width, height, depth, outputWidthStride, outputHeightStride, outputOffset, buffer, width, height, 0, ElementSize);

            // Encode depth vectors
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    Encode1D(buffer, depth, width * height, row * width + column,
                             output, outputWidthStride * outputHeightStride, outputOffset + row * outputWidthStride + column);
                }
            }

            Marshal.FreeHGlobal(buffer);
        }

        public abstract void Decode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output,
                                      int outputStride = -1,
                                      int outputOffset = -1);

        public void Decode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset, IntPtr output,
                             int outputWidthStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            // Decode rows
            for (int row = 0; row < height; row++)
            {
                Decode1D(input, width, 1, inputOffset + row * inputWidthStride,
                         output, 1, outputOffset + row * outputWidthStride);
            }
            
            IntPtr buffer = Marshal.AllocHGlobal(width * height * ElementSize);
            MemoryAllocator.Copy2D(output, width, height, outputWidthStride, outputOffset, buffer, width, 0, ElementSize);

            // Decode columns
            for (int column = 0; column < width; column++)
            {
                Decode1D(buffer, height, width, column,
                         output, outputWidthStride, outputOffset + column);
            }

            Marshal.FreeHGlobal(buffer);
        }

        public void Decode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride,
                             int inputOffset, IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputHeightStride == -1)
                outputHeightStride = inputHeightStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            // Decode depth vectors
            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    Decode1D(input, depth, inputWidthStride * inputHeightStride, row * inputWidthStride + column,
                             output, outputWidthStride * outputHeightStride, outputOffset + row * outputWidthStride + column);
                }
            }


            IntPtr buffer = Marshal.AllocHGlobal(width * height * depth * ElementSize);
            MemoryAllocator.Copy3D(output, width, height, depth, outputWidthStride, outputHeightStride, outputOffset, buffer, width, height, 0, ElementSize);


            // Decode 2d surface in depth
            for (int surface = 0; surface < depth; surface++)
            {
                Decode2D(buffer, width, height, width, surface * width * height,
                         output, outputWidthStride, outputOffset + surface * outputWidthStride * outputHeightStride);
            }

            Marshal.FreeHGlobal(buffer);
        }
    }
}