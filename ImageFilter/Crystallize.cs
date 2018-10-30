using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageFilter
{
    class Crystallize
    {
        private byte[] bitmapSourceToArray(BitmapSource bitmapSource, int stride)
        {
            // Stride = (width) * (bytes per pixel)
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private BitmapSource bitmapSourceFromArray(byte[] pixels, int height, int width)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }

        public BitmapSource process (ref BitmapSource img)
        {
            int h = img.PixelHeight;
            int w = img.PixelWidth;

            FormatConvertedBitmap greyImg = new FormatConvertedBitmap();
            greyImg.BeginInit();
            greyImg.Source = img;
            greyImg.DestinationFormat = PixelFormats.Gray8;
            greyImg.EndInit();
            int greyStride = (int)greyImg.PixelWidth * (greyImg.Format.BitsPerPixel / 8);
            byte[] pixels = new byte[h * greyStride];
            greyImg.CopyPixels(pixels, greyStride, 0);
            byte[,] grey = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    grey[i, j] = pixels[p++];
                }
            }

            ////右下灰度梯度
            //d[0, 0] = (Math.Abs(grey[0, 1] - grey[0, 0]) + Math.Abs(grey[1, 0] - grey[0, 0]));
            //d[0, w - 1] = (Math.Abs(grey[1, w - 1] - grey[0, w - 1])) * 2;
            //d[h - 1, 0] = (Math.Abs(grey[h - 1, 1] - grey[h - 1, 0])) * 2;
            //d[h - 1, w - 1] = 0;

            //for (int i = 1; i < w - 1; i++)
            //{
            //    d[0, i] = Math.Abs(grey[0, i + 1] - grey[0, i]) + Math.Abs(grey[1, i] - grey[0, i]);
            //    d[h - 1, i] = Math.Abs(grey[h - 1, i + 1] - grey[h - 1, i]) + Math.Abs(grey[h - 2, i] - grey[h - 1, i]);
            //}

            //for (int i = 1; i < h - 1; i++)
            //{
            //    d[i, 0] = Math.Abs(grey[i + 1, 0] - grey[i, 0]) + Math.Abs(grey[i, 1] - grey[i, 0]);
            //    d[i, w - 1] = Math.Abs(grey[i + 1, w - 1] - grey[i, w - 1]) * 2;
            //}

            //for (int i = 1; i < h - 1; i++)
            //{
            //    for (int j = 1; j < w - 1; j++)
            //    {
            //        d[i, j] = Math.Abs(grey[i + 1, j] - grey[i, j]) + Math.Abs(grey[i, j + 1] - grey[i, j]);
            //    }
            //}

            int resW = w;
            int resH = h;
            byte[] resPixels = new byte[resH * resW * 4];

            BitmapSource res = bitmapSourceFromArray(resPixels, h, w);
            return res;
        }
    }
}
