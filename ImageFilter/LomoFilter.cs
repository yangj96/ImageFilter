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
    class LomoFilter
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

        private static byte ModeSmoothLight(byte basePixel, byte mixPixel)
        {
            int res = 0;
            res = mixPixel > 128 ? ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((Math.Sqrt((float)basePixel / 255.0f)) * 255.0f - (float)basePixel) / 255.0f)) :
                  ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((float)basePixel - (float)basePixel * (float)basePixel / 255.0f) / 255.0f));
            return (byte)Math.Min(255, Math.Max(0, res));
        }

        //private static byte ModeExclude(byte basePixel, byte mixPixel)
        //{
        //    int res = 0;
        //    res = (mixPixel + basePixel) - mixPixel * basePixel / 128;
        //    return (byte)Math.Min(255, Math.Max(0, res));
        //}

        private static byte ContrastModify(int degree, byte basePixel)
        {
            if (degree < -100) degree = -100;
            if (degree > 100) degree = 100;
            double contrast = (100.0 + degree) / 100.0;
            contrast *= contrast;
            double pixel = ((basePixel / 255.0 - 0.5) * contrast + 0.5) * 255;
            if (pixel < 0) pixel = 0;
            if (pixel > 255) pixel = 255;
            return (byte)pixel;
        }

        private static byte BrightModify(int degree, byte basePixel)
        {
            if (degree < -255) degree = -255;
            if (degree > 255) degree = 255;
            int pixel = basePixel + degree;
            if (pixel < 0) pixel = 0;
            if (pixel > 255) pixel = 255;
            return (byte)pixel;
        }

        public static BitmapSource process (ref BitmapSource img)
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
            //将原图和原图进行柔光图层混合&对比度亮度增强
           for (int i = 0; i < h; i++)
           {
                for(int j = 0; j < w; j++)
                {
                    rband[i, j] = ModeSmoothLight(rband[i, j], rband[i, j]);
                    gband[i, j] = ModeSmoothLight(gband[i, j], gband[i, j]);
                    bband[i, j] = ModeSmoothLight(bband[i, j], bband[i, j]);
                    //rband[i, j] = ModeExclude(rband[i, j], 15);
                    //gband[i, j] = ModeExclude(gband[i, j], 6);
                    //bband[i, j] = ModeExclude(bband[i, j], 90);
                    rband[i, j] = ContrastModify(15, rband[i, j]);
                    gband[i, j] = ContrastModify(15, gband[i, j]);
                    bband[i, j] = ContrastModify(15, bband[i, j]);
                    rband[i, j] = BrightModify(55, rband[i, j]);
                    gband[i, j] = BrightModify(55, gband[i, j]);
                    bband[i, j] = BrightModify(55, bband[i, j]);
                }
            }
            //增加暗角
            VignetteEffect ve = new VignetteEffect();
            ve.ApplyEffect(w, h, ref rband, ref gband, ref bband);
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
