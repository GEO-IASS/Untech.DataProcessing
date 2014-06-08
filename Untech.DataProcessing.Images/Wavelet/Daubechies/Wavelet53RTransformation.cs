using System;

namespace Untech.DataProcessing.Images.Wavelet.Daubechies
{
    public class Wavelet53RTransformation : BaseWaveletTransformation
    {
        public override int ElementSize
        {
            get { return sizeof (short); }
        }

        public override void Encode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride = -1,
                                      int outputOffset = -1)
        {
            if (outputStride == -1)
                outputStride = inputStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            unsafe
            {
                Encode1D((short*)input.ToPointer(), size, inputStride, inputOffset, (short*)output.ToPointer(), outputStride, outputOffset);
            }
        }

        public override void Decode1D(IntPtr input, int size, int inputStride, int inputOffset, IntPtr output, int outputStride = -1,
                                      int outputOffset = -1)
        {
            if (outputStride == -1)
                outputStride = inputStride;
            if (outputOffset == -1)
                outputOffset = inputOffset;

            unsafe
            {
                Decode1D((short*)input.ToPointer(), size, inputStride, inputOffset, (short*)output.ToPointer(), outputStride, outputOffset);
            }
        }

        private unsafe void Encode1D(short* input, int size, int inputStride, int inputOffset, short* output, int outputStride, int outputOffset)
        {
            int highSize = size / 2;
            int lowSize = size - highSize;
            int count;

            // Move start postition
            input += inputOffset;
            output += outputOffset;

            short* low = output;
            short* high = output + lowSize * outputStride;


            // Alpha side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                int t = *(input + 2 * inputStride * i) + *(input + (2 * i + 2) * inputStride);
                t >>= 1;
                t = -t;
                t += *(input + (2 * i + 1) * inputStride);
                *(high + i * outputStride) = (short)t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(input + 2 * inputStride * i);
                t = -t;
                t += *(input + (2 * i + 1) * inputStride);
                *(high + i * outputStride) = (short)t;
            }

            // Beta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                int t = *(high + (i - 1) * outputStride) + *(high + i * outputStride) + 2;
                t >>= 2;
                t += *(input + 2 * inputStride * i);
                *(low + i * outputStride) = (short)t;
            }
            //
            {
                int t = *high * 2 + 2;
                t >>= 2;
                t += *input;
                *low = (short)t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                int t = *(high + (i - 1) * outputStride) * 2 + 2;
                t >>= 2;
                t += *(input + 2 * inputStride * i);
                *(low + i * outputStride) = (short)t;
            }
        }

        private unsafe void Decode1D(short* input, int size, int inputStride, int inputOffset, short* output, int outputStride, int outputOffset)
        {
            int highSize = size / 2;
            int lowSize = size - highSize;
            int count;

            // Move start postition
            input += inputOffset;
            output += outputOffset;

            short* low = input;
            short* high = input + lowSize * inputStride;


            // Beta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                int t = *(high + (i - 1) * inputStride) + *(high + i * inputStride) + 2;
                t >>= 2;
                t = -t;
                t += *(low + i * inputStride);
                *(output + 2 * outputStride * i) = (short)t;
            }
            //
            {
                int t = *high * 2 + 2;
                t >>= 2;
                t = -t;
                t += *low;
                *output = (short)t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                int t = *(high + (i - 1) * inputStride) * 2 + 2;
                t >>= 2;
                t = -t;
                t += *(low + i * inputStride);
                *(output + 2 * outputStride * i) = (short)t;
            }

            // Alpha side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                int t = *(output + 2 * outputStride * i) + *(output + (2 * i + 2) * outputStride);
                t >>= 1;
                t += *(high + i * inputStride);
                *(output + (2 * i + 1) * outputStride) = (short)t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(output + 2 * outputStride * i);
                t += *(high + i * inputStride);
                *(output + (2 * i + 1) * outputStride) = (short)t;
            }


        }
    }
}