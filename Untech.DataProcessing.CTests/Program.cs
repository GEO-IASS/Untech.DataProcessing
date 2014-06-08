using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Untech.DataProcessing.Common.Unmanaged;
using Untech.DataProcessing.Compression.Arithmetic;
using Untech.DataProcessing.Compression.Huffman;
using Untech.DataProcessing.Images.Hyperspectral;
using Untech.DataProcessing.Images.Metrics;
using Untech.DataProcessing.Images.SPIHT;
using Untech.DataProcessing.Images.Wavelet;
using Untech.DataProcessing.Images.Wavelet.Daubechies;

namespace Untech.DataProcessing.CTests
{
    class Program
    {
        private static void Main(string[] args)
        {

            //string path = @"E:\";
            //string origin = @"aviris_sc10.raw";
            //string chsStage = @"aviris_stage_1to{0}.chs";
            //string resStage = @"aviris_stage_1to{0}.raw";

            //var originImage = HyperspectralImage.Load(File.Open(Path.Combine(path, origin), FileMode.Open),
            //                                          new HyperspectralImageInfo { Bands = 224, Height = 512, Width = 640 });

            //DummyTest(originImage);
            //Console.WriteLine();

            //FlatWaveletTest(originImage);
            //Console.WriteLine();
            //HierarchicalWaveletTest(originImage);
            //Console.WriteLine();

            //double ratio = 1.0;
            //CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            //Console.WriteLine();
            
            //originImage.Dispose();


            string path = @"E:\SPIHT";
            string origin = @"aviris_small.raw";
            string chsStage = @"aviris_stage_1to{0}.chs";
            string resStage = @"aviris_stage_1to{0}.raw";

            var originImage = HyperspectralImage.Load(File.Open(Path.Combine(path, origin), FileMode.Open),
                                                      new HyperspectralImageInfo { Bands = 32, Height = 64, Width = 64 });

            DummyTest(originImage);
            Console.WriteLine();

            FlatWaveletTest(originImage);
            Console.WriteLine();
            HierarchicalWaveletTest(originImage);
            Console.WriteLine();

            

            
            double ratio = 1.0;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();
            ratio = 1.0 / 2;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();
            ratio = 1.0 / 4;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();
            ratio = 1.0 / 8;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();
            ratio = 1.0 / 16;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();
            ratio = 1.0 / 64;
            CompressTest(originImage, Path.Combine(path, string.Format(chsStage, 1 / ratio)), Path.Combine(path, string.Format(resStage, 1 / ratio)), ratio);
            Console.WriteLine();

            originImage.Dispose();
        }

        private static void DummyTest(HyperspectralImage image)
        {
            Console.WriteLine("Dummy Test. PSNR = {0}",
                              PSNRCalculator.Calculate(image.ToPointer(), image.ToPointer(), image.ImageInfo));
            Console.WriteLine("Dummy Test. MSE = {0}",
                              PSNRCalculator.CalculateMSE(image.ToPointer(), image.ToPointer(), image.ImageInfo));
        }

        private static void FlatWaveletTest(HyperspectralImage image)
        {
            IntPtr waveletData = Marshal.AllocHGlobal(image.SizeInBytes());
            IntPtr restoredData = Marshal.AllocHGlobal(image.SizeInBytes());

            var wavelet = new Wavelet53RTransformation();

            wavelet.Encode3D(image.ToPointer(), image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, waveletData);


            wavelet.Decode3D(waveletData, image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, restoredData);

            Console.WriteLine("Flat Wavelet only. PSNR = {0}", PSNRCalculator.Calculate(image.ToPointer(), restoredData, image.ImageInfo));
            Console.WriteLine("Flat Wavelet only. MSE = {0}", PSNRCalculator.CalculateMSE(image.ToPointer(), restoredData, image.ImageInfo));

            Marshal.FreeHGlobal(waveletData);
            Marshal.FreeHGlobal(restoredData);
        }

        private static void HierarchicalWaveletTest(HyperspectralImage image)
        {
            IntPtr waveletData = Marshal.AllocHGlobal(image.SizeInBytes());
            IntPtr restoredData = Marshal.AllocHGlobal(image.SizeInBytes());

            var wavelet = new WaveletTransformationController(new Wavelet53RTransformation());

            wavelet.Encode3D(image.ToPointer(), image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, waveletData);


            wavelet.Decode3D(waveletData, image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, restoredData);

            Console.WriteLine("Hierarchical Wavelet only. PSNR = {0}", PSNRCalculator.Calculate(image.ToPointer(), restoredData, image.ImageInfo));
            Console.WriteLine("Hierarchical Wavelet only. MSE = {0}", PSNRCalculator.CalculateMSE(image.ToPointer(), restoredData, image.ImageInfo));

            Marshal.FreeHGlobal(waveletData);
            Marshal.FreeHGlobal(restoredData);
        }

        private static void CompressTest(HyperspectralImage image, string tempPath, string restoredPath, double compression)
        {
            var sw = new Stopwatch();
            sw.Start();

            IntPtr tempData = Marshal.AllocHGlobal(image.SizeInBytes());
            IntPtr restoredData = Marshal.AllocHGlobal(image.SizeInBytes());

            var wavelet = new WaveletTransformationController(new Wavelet53RTransformation());

            wavelet.Encode3D(image.ToPointer(), image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, tempData);


            var budget = (long)(compression * image.SizeInBytes() * 8);

            using (var stream = File.Open(tempPath, FileMode.Create))
            {
                var coder = new ArithmeticCoder(stream);
                var compressor = new Spiht3DCoder(tempData,
                                                    image.ImageInfo.Width,
                                                    image.ImageInfo.Height,
                                                    image.ImageInfo.Bands);

                compressor.Encode(coder, budget);
            }
            GC.Collect();

            using (var stream = File.Open(tempPath, FileMode.Open))
            {
                var decoder = new ArithmeticDecoder(stream);
                var decompressor = new Spiht3DDecoder(tempData,
                                                      image.ImageInfo.Width,
                                                      image.ImageInfo.Height,
                                                      image.ImageInfo.Bands);

                decompressor.Decode(decoder, budget);
            }
            GC.Collect();

            

            wavelet.Decode3D(tempData, image.ImageInfo.Width, image.ImageInfo.Height, image.ImageInfo.Bands,
                             image.ImageInfo.Width, image.ImageInfo.Height, 0, restoredData);

            using (var stream = File.Open(restoredPath, FileMode.Create))
            {
                HyperspectralImage.Save(HyperspectralImage.Load(restoredData, image.ImageInfo), stream);
            }

            Console.WriteLine("Compression 1:{0}. PSNR = {1}", 1 / compression, PSNRCalculator.Calculate(image.ToPointer(), restoredData, image.ImageInfo));
            Console.WriteLine("Compression 1:{0}. MSE = {1}", 1 / compression, PSNRCalculator.CalculateMSE(image.ToPointer(), restoredData, image.ImageInfo));
            sw.Stop();

            Console.WriteLine("{0}:{1} (Ticks: {2})", sw.Elapsed.Seconds, sw.Elapsed.Milliseconds, sw.ElapsedTicks);


            Marshal.FreeHGlobal(tempData);
            Marshal.FreeHGlobal(restoredData);
        }
    }
}
