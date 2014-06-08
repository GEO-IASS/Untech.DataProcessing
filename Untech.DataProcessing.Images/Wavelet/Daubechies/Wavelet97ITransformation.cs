using System;
using System.Diagnostics;

namespace Untech.DataProcessing.Images.Wavelet.Daubechies
{
    public class Wavelet97ITransformation : BaseWaveletTransformation
    {
        private const float Alpha = -1.586134342f;
        private const float Beta = -0.052980118f;
        private const float Gamma = 0.882911075f;
        private const float Delta = 0.443506852f;
        private const float K = 1.230174105f;
        private const float ReverseK = 1 / K;

        public override int ElementSize
        {
            get { return sizeof (float); }
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
                Encode1D((float*)input.ToPointer(), size, inputStride, inputOffset, (float*)output.ToPointer(), outputStride, outputOffset);
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
                Decode1D((float*)input.ToPointer(), size, inputStride, inputOffset, (float*)output.ToPointer(), outputStride, outputOffset);
            }
        }


        private unsafe void Encode1D(float* input, int size, int inputStride, int inputOffset, float* output, int outputStride, int outputOffset)
        {
            int highSize = size / 2;
            int lowSize = size - highSize;
            int count;

            // Move start postition
            input += inputOffset;
            output += outputOffset;

            float* low = output;
            float* high = output + lowSize * outputStride;


            // Alpha side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                float t = *(input + 2 * inputStride * i) + *(input + (2 * i + 2) * inputStride);
                t *= Alpha;
                t += *(input + (2 * i + 1) * inputStride);
                *(high + i * outputStride) = t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(input + 2 * inputStride * i);
                t *= 2 * Alpha;
                t += *(input + (2 * i + 1) * inputStride);
                *(high + i * outputStride) = t;
            }

            // Beta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                float t = *(high + (i - 1) * outputStride) + *(high + i * outputStride);
                t *= Beta;
                t += *(input + 2 * inputStride * i);
                *(low + i * outputStride) = t;
            }
            //
            {
                float t = *high;
                t *= 2 * Beta;
                t += *input;
                *low = t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                float t = *(high + (i - 1) * outputStride);
                t *= 2 * Beta;
                t += *(input + 2 * i * inputStride);
                *(low + i * outputStride) = t;
            }

            // Gamma side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                float t = *(low + i * outputStride) + *(low + (i + 1) * outputStride);
                t *= Gamma;
                t += *(high + i * outputStride);
                *(high + i * outputStride) = t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(low + i * outputStride);
                t *= 2 * Gamma;
                t += *(high + i * outputStride);
                *(high + i * outputStride) = t;
            }

            // Delta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                float t = *(high + (i - 1) * outputStride) + *(high + i * outputStride);
                t *= Delta;
                t += *(low + i * outputStride);
                *(low + i * outputStride) = t;
            }
            //
            {
                float t = *high;
                t *= 2 * Delta;
                t += *low;
                *low = t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                float t = *(high + (i - 1) * outputStride);
                t *= 2 * Delta;
                t += *(low + i * outputStride);
                *(low + i * outputStride) = t;
            }

            for (int i = 0; i < highSize; i++)
            {
                *(low + i * outputStride) *= ReverseK;
                *(high + i * outputStride) *= K;
            }
            if (lowSize != highSize)
            {
                *(low + (lowSize - 1) * outputStride) *= ReverseK;
            }
        }

        private unsafe void Decode1D(float* input, int size, int inputStride, int inputOffset, float* output, int outputStride, int outputOffset)
        {
            int highSize = size / 2;
            int lowSize = size - highSize;
            int count;

            // Move start postition
            input += inputOffset;
            output += outputOffset;

            float* low = input;
            float* high = input + lowSize * inputStride;

            for (int i = 0; i < highSize; i++)
            {
                *(output + 2 * i * outputStride) = *(low + i * inputStride) * K;
                *(output + (2 * i + 1) * outputStride) = *(high + i * inputStride) * ReverseK;
            }
            if (lowSize != highSize)
            {
                *(output + 2 * (lowSize - 1) * outputStride) = *(low + (lowSize - 1) * inputStride) * K;
            }


            // Delta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                float t = *(output + (2 * i - 1) * outputStride) + *(output + (2 * i + 1) * outputStride);
                t *= -Delta;
                t += *(output + 2 * i * outputStride);
                *(output + 2 * i * outputStride) = t;
            }
            //
            {
                float t = *(output + outputStride);
                t *= -2 * Delta;
                t += *output;
                *output = t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                float t = *(output + (2 * i - 1) * outputStride);
                t *= -2 * Delta;
                t += *(output + 2 * i * outputStride);
                *(output + 2 * i * outputStride) = t;
            }

            // Gamma side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                float t = *(output + 2 * i * outputStride) + *(output + (2 * i + 2) * outputStride);
                t *= -Gamma;
                t += *(output + (2 * i + 1) * outputStride);
                *(output + (2 * i + 1) * outputStride) = t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(output + 2 * i * outputStride);
                t *= -2 * Gamma;
                t += *(output + (2 * i + 1) * outputStride);
                *(output + (2 * i + 1) * outputStride) = t;
            }

            // Beta side
            count = size / 2;
            for (int i = 1; i < count; i++)
            {
                float t = *(output + (2 * i - 1) * outputStride) + *(output + (2 * i + 1) * outputStride);
                t *= -Beta;
                t += *(output + 2 * i * outputStride);
                *(output + 2 * i * outputStride) = t;
            }
            //
            {
                float t = *(output + outputStride);
                t *= -2 * Beta;
                t += *output;
                *output = t;
            }
            if ((size & 1) == 1)
            {
                int i = lowSize - 1;
                float t = *(output + (2 * i - 1) * outputStride);
                t *= -2 * Beta;
                t += *(output + 2 * i * outputStride);
                *(output + 2 * i * outputStride) = t;
            }

            // Alpha side
            count = (size - 1) / 2;
            for (int i = 0; i < count; i++)
            {
                float t = *(output + 2 * outputStride * i) + *(output + (2 * i + 2) * outputStride);
                t *= -Alpha;
                t += *(output + (2*i + 1) * outputStride);
                *(output + (2 * i + 1) * outputStride) = t;
            }
            if ((size & 1) == 0)
            {
                int i = highSize - 1;
                float t = *(output + 2 * outputStride * i);
                t *= -2 * Alpha;
                t += *(output + (2*i+1) * outputStride);
                *(output + (2 * i + 1) * outputStride) = t;
            }
        }
    }
}