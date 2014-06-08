using System;
using System.Runtime.InteropServices;

namespace Untech.DataProcessing.Common.Unmanaged
{
    public static unsafe class MemoryAllocator
    {
        public static void* New<T>(int elementCount) where T : struct
        {
            return Marshal.AllocHGlobal(Marshal.SizeOf(typeof(T)) * elementCount).ToPointer();
        }

        public static void* NewAndInit<T>(int elementCount) where T : struct
        {
            var newSizeInBytes = Marshal.SizeOf(typeof(T)) * elementCount;
            var newArrayPointer = (byte*)Marshal.AllocHGlobal(newSizeInBytes).ToPointer();

            for (int i = 0; i < newSizeInBytes; i++)
                *(newArrayPointer + i) = 0;

            return newArrayPointer;
        }

        public static void Free(void* pointerToUnmanagedMemory)
        {
            Marshal.FreeHGlobal(new IntPtr(pointerToUnmanagedMemory));
        }

        public static void* Resize<T>(void* oldPointer, int newElementCount) where T : struct
        {
            return (Marshal.ReAllocHGlobal(new IntPtr(oldPointer), new IntPtr(Marshal.SizeOf(typeof(T)) * newElementCount))).ToPointer();
        }

        public static void Copy<T>(void* source, void* dest, int elementCount)
        {
            var newSizeInBytes = Marshal.SizeOf(typeof(T)) * elementCount;

            var newSourcePointer = (byte*)source;
            var newDestPointer = (byte*)dest;

            while (newSizeInBytes-- > 0)
                *(newDestPointer + newSizeInBytes) = *(newSourcePointer + newSizeInBytes);
        }

        public static void Copy1D(IntPtr input, int size, int inputStride, int inputOffset,
                                  IntPtr output, int outputStride, int outputOffset,
                                  int elementSize)
        {
            if (elementSize == 4)
                Copy1D((int*)input.ToPointer(), size, inputStride, inputOffset,
                       (int*)output.ToPointer(), outputStride, outputOffset);
            else if (elementSize == 2)
                Copy1D((short*)input.ToPointer(), size, inputStride, inputOffset,
                       (short*)output.ToPointer(), outputStride, outputOffset);
            else
                throw new ArgumentException();
        }




        public static void Copy2D(IntPtr input, int width, int height, int inputWidthStride, int inputOffset,
                                  IntPtr output, int outputWidthStride, int outputOffset, int elementSize)
        {
            if (elementSize == 4)
                Copy2D((int*)input.ToPointer(), width, height, inputWidthStride, inputOffset,
                    (int*)output.ToPointer(), outputWidthStride, outputOffset);
            else if (elementSize == 2)
                Copy2D((short*)input.ToPointer(), width, height, inputWidthStride, inputOffset,
                    (short*)output.ToPointer(), width, 0);
            else
                throw new ArgumentException();
        }

        public static void Copy3D(IntPtr input, int width, int height, int depth, int inputWidthStride,
                                  int inputHeightStride, int inputOffset,
                                  IntPtr output, int outputWidthStride, int outputHeightStride, int outputOffset, int elementSize)
        {
            if (elementSize == 4)
                Copy3D((int*)input.ToPointer(), width, height, depth, inputWidthStride, inputHeightStride, inputOffset,
                    (int*)output.ToPointer(), outputWidthStride, outputHeightStride, outputOffset);
            else if (elementSize == 2)
                Copy3D((short*)input.ToPointer(), width, height, depth, inputWidthStride, inputHeightStride, inputOffset,
                    (short*)output.ToPointer(), outputWidthStride, outputHeightStride, outputOffset);
            else
                throw new ArgumentException();
        }

        public static void Copy1D(int* input, int size, int inputStride, int inputOffset, int* output,
                                int outputStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int i = 0; i < size; i++)
            {
                *(output + i * outputStride) = *(input + i * inputStride);
            }
        }
        public static void Copy1D(short* input, int size, int inputStride, int inputOffset, short* output,
                                int outputStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int i = 0; i < size; i++)
            {
                *(output + i * outputStride) = *(input + i * inputStride);
            }
        }
        public static void Copy2D(int* input, int width, int height, int inputWidthStride, int inputOffset,
                                  int* output, int outputWidthStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    *(output + row * outputWidthStride + column) = *(input + row * inputWidthStride + column);
                }
            }
        }
        public static void Copy2D(short* input, int width, int height, int inputWidthStride, int inputOffset,
                                  short* output, int outputWidthStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int row = 0; row < height; row++)
            {
                for (int column = 0; column < width; column++)
                {
                    *(output + row * outputWidthStride + column) = *(input + row * inputWidthStride + column);
                }
            }
        }
        public static void Copy3D(int* input, int width, int height, int depth, int inputWidthStride,
                                  int inputHeightStride, int inputOffset,
                                  int* output, int outputWidthStride, int outputHeightStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int d = 0; d < depth; d++)
            {
                int* inputSurface = input + d * inputWidthStride * inputHeightStride;
                int* outputSurface = output + d * outputWidthStride * outputHeightStride;

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        *(outputSurface + row * outputWidthStride + column) = *(inputSurface + row * inputWidthStride + column);
                    }
                }
            }
        }
        public static void Copy3D(short* input, int width, int height, int depth, int inputWidthStride,
                                  int inputHeightStride, int inputOffset,
                                  short* output, int outputWidthStride, int outputHeightStride, int outputOffset)
        {
            input += inputOffset;
            output += outputOffset;

            for (int d = 0; d < depth; d++)
            {
                short* inputSurface = input + d * inputWidthStride * inputHeightStride;
                short* outputSurface = output + d * outputWidthStride * outputHeightStride;

                for (int row = 0; row < height; row++)
                {
                    for (int column = 0; column < width; column++)
                    {
                        *(outputSurface + row * outputWidthStride + column) = *(inputSurface + row * inputWidthStride + column);
                    }
                }
            }
        }
    }
}