using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Untech.DataProcessing.Images.Hyperspectral
{
    public class HyperspectralImage : IDisposable
    {
        private readonly HyperspectralImageInfo _imageInfo;
        private readonly IntPtr _data;

        private bool _disposed;

        protected HyperspectralImage(HyperspectralImageInfo info, IntPtr data)
        {
            _disposed = false;
            _imageInfo = info;
            _data = data;
        }

        public HyperspectralImageInfo ImageInfo
        {
            get { return _imageInfo; }
        }

        public UInt16 this[int x, int y, int band]
        {
            get
            {
                unsafe
                {
                    var pointer = (UInt16*)_data.ToPointer();
                    return *(pointer + _imageInfo.Width * _imageInfo.Height * band + _imageInfo.Width * y + x);
                }
            }
            set
            {
                unsafe
                {
                    var pointer = (UInt16*)_data.ToPointer();
                    *(pointer + _imageInfo.Width * _imageInfo.Height * band + _imageInfo.Width * y + x) = value;
                }
            }
        }

        public static HyperspectralImage Load(Stream stream, HyperspectralImageInfo info)
        {
            using (var reader = new BinaryReader(stream))
            {
                var dataPtr = Marshal.AllocHGlobal(info.Bands * info.Height * info.Width * 2);

                unsafe
                {
                    var destPointer = (UInt16*)dataPtr.ToPointer();

                    for (int height = 0; height < info.Height; height++)
                    {
                        for (int width = 0; width < info.Width; width++)
                        {
                            for (int band = 0; band < info.Bands; band++)
                            {
                                ushort  t = reader.ReadByte();
                                t <<= 8;
                                t += reader.ReadByte();
                                
                                *(destPointer + info.Width * info.Height * band + info.Width * height + width) = t;
                            }
                        }
                    }
                }

                return new HyperspectralImage(info, dataPtr);
            }
        }

        public static HyperspectralImage Load(IntPtr data, HyperspectralImageInfo info)
        {
            var dataPtr = Marshal.AllocHGlobal(info.Bands * info.Height * info.Width * 2);

            unsafe
            {
                var sourcePointer = (UInt16*)data.ToPointer();
                var destPointer = (UInt16*)dataPtr.ToPointer();

                for (int i = 0; i < info.Bands*info.Height*info.Width; i++)
                {
                    *(destPointer + i) = *(sourcePointer + i);
                }
            }

            return new HyperspectralImage(info, dataPtr);
        }

        public static void Save(HyperspectralImage image, Stream stream)
        {
            using (var writer = new BinaryWriter(stream))
            {
                unsafe
                {
                    var sourcePointer = (UInt16*)image._data.ToPointer();

                    for (int height = 0; height < image._imageInfo.Height; height++)
                    {
                        for (int width = 0; width < image._imageInfo.Width; width++)
                        {
                            for (int band = 0; band < image._imageInfo.Bands; band++)
                            {
                                var t = *(sourcePointer + image._imageInfo.Width * image._imageInfo.Height * band + image._imageInfo.Width * height + width);
                                byte msb = (byte) (t >> 8);
                                byte lsb = unchecked ((byte) t);
                                writer.Write(msb);
                                writer.Write(lsb);
                            }
                        }
                    }
                }
            }
        }


        public IntPtr ToPointer()
        {
            return _data;
        }

        public int SizeInBytes()
        {
            return _imageInfo.Bands*_imageInfo.Height*_imageInfo.Width*2;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Marshal.FreeHGlobal(_data);
                _disposed = true;
            }
        }
    }
}