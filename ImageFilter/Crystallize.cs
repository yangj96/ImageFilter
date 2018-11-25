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
        private static byte[] bitmapSourceToArray(BitmapSource bitmapSource, int stride)
        {
            // Stride = (width) * (bytes per pixel)
            byte[] pixels = new byte[(int)bitmapSource.PixelHeight * stride];

            bitmapSource.CopyPixels(pixels, stride, 0);

            return pixels;
        }

        private static BitmapSource bitmapSourceFromArray(byte[] pixels, int height, int width)
        {
            WriteableBitmap bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * (bitmap.Format.BitsPerPixel / 8), 0);

            return bitmap;
        }

        public static BitmapSource process (ref BitmapSource img, double centerNum, double m)
        {
            int h = img.PixelHeight;
            int w = img.PixelWidth;
            int stride = w * (img.Format.BitsPerPixel / 8);
            //获取像素数据
            byte[] imgPixels = bitmapSourceToArray(img, stride);
            //将像素数组分别转换为rgb数组
            byte[,] rband = new byte[h, w];
            byte[,] gband = new byte[h, w];
            byte[,] bband = new byte[h, w];
            byte[,] alpha = new byte[h, w];
            int p = 0;
            for (int i = 0; i < h; ++i)
            {
                for (int j = 0; j < w; ++j)
                {
                    // 每个像素的指针是按BGRA的顺序存储
                    alpha[i, j] = imgPixels[p + 3];
                    rband[i, j] = imgPixels[p + 2];
                    gband[i, j] = imgPixels[p + 1];
                    bband[i, j] = imgPixels[p];
                    p += 4;   // 偏移一个像素
                }
            }

            SLIC.ApplyEffect(ref rband, ref gband, ref bband, w, h, centerNum, m);

            //根据rgb数组构造返回byte数组
            int resW = w;
            int resH = h;
            byte[] resPixels = new byte[resH * resW * 4];
            int resP = 0;
            for (int i = 0; i < resH; ++i)
            {
                for (int j = 0; j < resW; ++j)
                {
                    resPixels[resP] = bband[i, j];
                    resPixels[resP + 1] = gband[i, j];
                    resPixels[resP + 2] = rband[i, j];
                    resPixels[resP + 3] = alpha[i, j];
                    resP += 4;   // 偏移一个像素
                }
            }

            BitmapSource res = bitmapSourceFromArray(resPixels, h, w);
            return res;
        }
    }
}
