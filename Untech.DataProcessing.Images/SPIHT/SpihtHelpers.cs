using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Untech.DataProcessing.Images.SPIHT
{
    public unsafe static class SpihtHelpers
    {
        public static bool AbsoluteCompare(short* x, short* y, int size)
        {
            for (int i = 0; i < size; i++)
                if (Math.Abs(*(x + i)) != Math.Abs(*(y + i)))
                {
                    Console.WriteLine("{0}, {1}",Math.Abs(*(x + i)) , Math.Abs(*(y + i)));
                    return false;
                };


            return true;
        }

        public static bool AbsoluteCompare(short* x, short* y, int width, int height, int depth)
        {
            for (byte band = 0; band < depth; band++)
            {
                for (short row = 0; row < height; row++)
                {
                    for (short column = 0; column < width; column++)
                    {
                        var i = band*width*height + row*width + column;
                        if (Math.Abs(*(x + i)) != Math.Abs(*(y + i)))
                        {
                            Console.WriteLine("{2},{3},{4}:{0}, {1}", Math.Abs(*(x + i)), Math.Abs(*(y + i)), row, column, band);
                            //return false;
                        };
                    }
                }
            }

            return true;
        }
        public static short AbsoluteMax(short* x, int size)
        {
            short tmp = Math.Abs(*x);

            for (int i = 1; i < size; i++)
                if (Math.Abs(*(x + i)) > tmp)
                    tmp = Math.Abs(*(x + i));

            return tmp;
        }

        public static List<SubbandSize> CalculateSubbands(int width, int height, int depth)
        {
            var subbands = new List<SubbandSize>();

            var levels = Wavelet.WaveletTransformationController.MaximumLevelNumber;
            var minSize = Wavelet.WaveletTransformationController.MinimumBandSize;

            while (levels-- > 0)
            {
                if (width > minSize && height > minSize && depth > minSize)
                {
                    width ++;
                    height ++;
                    depth ++;

                    width /= 2;
                    height /= 2;
                    depth /= 2;

                    subbands.Add(new SubbandSize {Width = width, Height = height, Depth = depth});
                }
                else
                {
                    break;
                }
            }

            return subbands;
        }


        
    }
}
