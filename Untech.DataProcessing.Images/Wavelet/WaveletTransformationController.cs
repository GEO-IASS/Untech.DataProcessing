using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Untech.DataProcessing.Common.Unmanaged;

namespace Untech.DataProcessing.Images.Wavelet
{
    public class WaveletTransformationController : IWaveletTransformation
    {
        public static readonly int MaximumLevelNumber = 5;
        public static readonly int MinimumBandSize = 2;

        private readonly IWaveletTransformation _waveletTransformation;

        public WaveletTransformationController(IWaveletTransformation implementaition)
        {
            _waveletTransformation = implementaition;
        }

        public IWaveletTransformation WaveletTransformation
        {
            get { return _waveletTransformation; }
        }

        public int ElementSize { get { return _waveletTransformation.ElementSize; } }

        public void Encode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride = -1,
                             int outputOffset = -1)
        {
            if (outputStride == -1)
                outputStride = inputStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            IntPtr buffer = Marshal.AllocHGlobal(size * ElementSize);

            MemoryAllocator.Copy1D(input, size, inputStride, inputOffset, buffer, 1, 0, ElementSize);

            Encode1D(buffer, size, 1, 0, output, outputStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Encode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride,
                              int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && size > MinimumBandSize)
            {
                _waveletTransformation.Encode1D(input, size, inputStride, inputOffset, output, outputStride,
                                                outputOffset);

                size++;
                size /= 2;
                level++;
                if (level < MaximumLevelNumber && size > MinimumBandSize)
                {
                    MemoryAllocator.Copy1D(output, size, outputStride, outputOffset,
                                           input, inputStride, inputOffset, ElementSize);

                    Encode1D(input, size, inputStride, inputOffset, output, outputStride, outputOffset, level);
                }
            }
        }

        public void Encode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset, IntPtr output,
                             int outputWidthStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            IntPtr buffer = Marshal.AllocHGlobal(width * height * ElementSize);

            MemoryAllocator.Copy2D(input, width, height, inputWidthStride, inputOffset,
                                   buffer, width, 0, ElementSize);

            Encode2D(buffer, width, height, width, 0, output, outputWidthStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Encode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset, IntPtr output, int outputWidthStride,
                             int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize)
            {
                _waveletTransformation.Encode2D(input, width, height, inputWidthStride, inputOffset, output, outputWidthStride,
                                                outputOffset);

                width ++;
                height++;
                width /= 2;
                height /= 2;
                level++;
                if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize)
                {
                    MemoryAllocator.Copy2D(output, width, height, outputWidthStride, outputOffset,
                                           input, inputWidthStride, 0, ElementSize);

                    Encode2D(input, width, height, inputWidthStride, inputOffset, output, outputWidthStride, outputOffset, level);
                }
            }
        }

        public void Encode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride,
                             int inputOffset, IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1,
                             int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputHeightStride == -1)
                outputHeightStride = inputHeightStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            IntPtr buffer = Marshal.AllocHGlobal(width * height * depth * ElementSize);

            MemoryAllocator.Copy3D(input, width, height, depth, inputWidthStride, inputHeightStride, inputOffset,
                                   buffer, width, height, 0, ElementSize);

            Encode3D(buffer, width, height, depth, width, height, 0, output, outputWidthStride, outputHeightStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Encode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride, int inputOffset, IntPtr output, int outputWidthStride, int outputHeightStride,
                             int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize && depth > MinimumBandSize)
            {
                Debug.WriteLine(level);
                _waveletTransformation.Encode3D(input, width, height, depth, inputWidthStride, inputHeightStride, inputOffset, output, outputWidthStride, outputHeightStride, outputOffset);

                width ++;
                height ++;
                depth ++;
                width /= 2;
                height /= 2;
                depth /= 2;
                level++;
                if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize && depth > MinimumBandSize)
                {
                    MemoryAllocator.Copy3D(output, width, height, depth, outputWidthStride, outputHeightStride, outputOffset,
                                           input, inputWidthStride, inputHeightStride, inputOffset, ElementSize);

                    Encode3D(input, width, height, depth, inputWidthStride, inputHeightStride, inputOffset, output, outputWidthStride, outputHeightStride, outputOffset, level);
                }
            }
        }

        public void Decode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride = -1,
                             int outputOffset = -1)
        {
            if (outputStride == -1)
                outputStride = inputStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;


            IntPtr buffer = Marshal.AllocHGlobal(size * ElementSize);

            MemoryAllocator.Copy1D(input, size, inputStride, inputOffset, buffer, 1, 0, ElementSize);

            Decode1D(buffer, size, 1, 0, output, outputStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Decode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride,
                              int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && size > MinimumBandSize)
            {
                Decode1D(input, (size+1) / 2, inputStride, inputOffset, output, outputStride, outputOffset, level + 1);

                _waveletTransformation.Decode1D(input, size, inputStride, inputOffset, output, outputStride,
                                            outputOffset);

                MemoryAllocator.Copy1D(output, size, outputStride, outputOffset,
                                       input, inputStride, inputOffset, ElementSize);
            }
        }

        public void Decode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset, IntPtr output,
                             int outputWidthStride = -1, int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            IntPtr buffer = Marshal.AllocHGlobal(width * height * ElementSize);

            MemoryAllocator.Copy2D(input, width, height, inputWidthStride, inputOffset,
                                   buffer, width, 0, ElementSize);

            Decode2D(buffer, width, height, width, 0, output, outputWidthStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Decode2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset, IntPtr output, int outputWidthStride,
                             int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize)
            {
                Decode2D(input, (width+1) / 2, (height+1) / 2, inputWidthStride, inputOffset, output, outputWidthStride, outputOffset, level + 1);

                _waveletTransformation.Decode2D(input, width, height, inputWidthStride, inputOffset, output, outputWidthStride, outputOffset);

                MemoryAllocator.Copy2D(output, width, height, outputWidthStride, outputOffset,
                                       input, inputWidthStride, inputOffset, ElementSize);
            }
        }

        public void Decode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride,
                             int inputOffset, IntPtr output, int outputWidthStride = -1, int outputHeightStride = -1,
                             int outputOffset = -1)
        {
            if (outputWidthStride == -1)
                outputWidthStride = inputWidthStride;
            if (outputHeightStride == -1)
                outputHeightStride = inputHeightStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            IntPtr buffer = Marshal.AllocHGlobal(width * height * depth * ElementSize);

            MemoryAllocator.Copy3D(input, width, height, depth, inputWidthStride, inputHeightStride, inputOffset,
                                   buffer, width, height, 0, ElementSize);

            Decode3D(buffer, width, height, depth, width, height, 0, output, outputWidthStride, outputHeightStride, outputOffset, 0);

            Marshal.FreeHGlobal(buffer);
        }

        private void Decode3D(IntPtr input, int width, int height, int depth, int inputWidthStride, int inputHeightStride, int inputOffset, IntPtr output, int outputWidthStride, int outputHeightStride,
                             int outputOffset, int level)
        {
            if (level < MaximumLevelNumber && width > MinimumBandSize && height > MinimumBandSize && depth > MinimumBandSize)
            {
                Decode3D(input, (width + 1) / 2, (height+1) / 2, (depth+1) / 2, inputWidthStride, inputHeightStride, inputOffset, output, outputWidthStride, outputHeightStride, outputOffset, level + 1);

                _waveletTransformation.Decode3D(input, width, height, depth, inputWidthStride, inputHeightStride, inputOffset, output, outputWidthStride, outputHeightStride, outputOffset);

                MemoryAllocator.Copy3D(output, width, height, depth, outputWidthStride, outputHeightStride, outputOffset,
                                       input, inputWidthStride, inputHeightStride, inputOffset, ElementSize);
            }
        }
    }
}