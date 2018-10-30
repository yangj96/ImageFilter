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

        private byte ModeSmoothLight(byte basePixel, byte mixPixel)
        {
            int res = 0;
            res = mixPixel > 128 ? ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((Math.Sqrt((float)basePixel / 255.0f)) * 255.0f - (float)basePixel) / 255.0f)) :
                  ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((float)basePixel - (float)basePixel * (float)basePixel / 255.0f) / 255.0f));
            return (byte)Math.Min(255, Math.Max(0, res));
        }

        private byte ModeExclude(byte basePixel, byte mixPixel)
        {
            int res = 0;
            res = (mixPixel + basePixel) - mixPixel * basePixel / 128;
            return (byte)Math.Min(255, Math.Max(0, res));
        }

        private int getDistance(int x1, int y1, int x2, int y2)
        {
            int x = x1 - x2;
            int y = y1 - y2;
            return (int)Math.Sqrt(x * x + y * y);
        }


        //TODO 生成暗角模板叠加

        private byte Vignette(byte basePixel, double radius, int h, int w, int y, int x)
        {
            int res = 0;
            int distance = getDistance(y, x, h / 2, w / 2);
            // 在半径外的点直接设置为黑色
            if (distance > radius)
            {
                res = 0;
            }
            // 依据点到图像中点的距离设置像素的颜色
            // 模拟一个渐变的alpha遮罩的效果
            else
            {
                float ratio = (float)(1 - distance * 1.0 / radius);
                res = (int)(basePixel * ratio);
            }
            return (byte)res;
        }

        public BitmapSource process (ref BitmapSource img)
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
           double radius = (h > w) ? h * 0.8 : w * 0.8;
            //将原图和原图进行柔光图层混合&蓝色风格排除图层混合 
           for (int i = 0; i < h; i++)
           {
                for(int j = 0; j < w; j++)
                {
                    rband[i, j] = ModeSmoothLight(rband[i, j], rband[i, j]);
                    gband[i, j] = ModeSmoothLight(gband[i, j], gband[i, j]);
                    bband[i, j] = ModeSmoothLight(bband[i, j], bband[i, j]);
                    rband[i, j] = ModeExclude(rband[i, j], 0);
                    gband[i, j] = ModeExclude(gband[i, j], 6);
                    bband[i, j] = ModeExclude(bband[i, j], 103);
                    rband[i, j] = Vignette(rband[i, j], radius, h, w, i, j);
                    gband[i, j] = Vignette(gband[i, j], radius, h, w, i, j);
                    bband[i, j] = Vignette(bband[i, j], radius, h, w, i, j);
                }
            }
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
