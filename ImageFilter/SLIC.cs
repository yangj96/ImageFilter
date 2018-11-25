using ColorMine.ColorSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageFilter
{
    class SLIC
    {
        private static void RGBtoLAB(byte r, byte g, byte b, ref double l, ref double a, ref double labb)
        {
            Rgb myRGB = new Rgb
            {
                R = (double)r,
                G = (double)g,
                B = (double)b
            };

            Lab myLAB = myRGB.To<Lab>();
            l = myLAB.L;
            a = myLAB.A;
            labb = myLAB.B;
        }

        private static void LABtoRGB(double l, double a, double labb, ref byte r, ref byte g, ref byte b)
        {
            Lab myLAB = new Lab
            {
                L = l,
                A = a,
                B = labb
            };

            Rgb myRGB = myLAB.To<Rgb>();
            r = (byte)myRGB.R;
            g = (byte)myRGB.G;
            b = (byte)myRGB.B;
        }

        private static Center[] createCenters(int w, int h, double[,] lband, double[,] aband, double[,] labbband, double numberOfCenters, double S)
        {
            List<Center> centers = new List<Center>();
            for (double x = S; x < h - S / 2; x += S)
                for (double y = S; y < w - S / 2; y += S)
                {
                    int xx = (int)Math.Floor(x);
                    int yy = (int)Math.Floor(y);

                    double L = lband[xx, yy];
                    double A = aband[xx, yy];
                    double B = labbband[xx, yy];

                    centers.Add(new Center(xx, yy, L, A, B, 0));
                }
            return centers.ToArray();
        }

        public static void ApplyEffect(ref byte[,] rband, ref byte[,] gband, ref byte[,] bband, int w, int h, double numberOfCenters, double m)
        {
            //颜色空间转换
            double[,] lband = new double[h, w];
            double[,] aband = new double[h, w];
            double[,] labbband = new double[h, w];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    RGBtoLAB(rband[i,j], gband[i,j], bband[i,j], ref lband[i,j], ref aband[i,j], ref labbband[i,j]);
                }
            }
            //计算每个超像素分割区域的面积
            double S = Math.Sqrt((w * h) / numberOfCenters);

            //生成聚类中心
            Center[] centers = createCenters(w, h, lband, aband, labbband, numberOfCenters, S);
            //生成聚类标签
            double[,] labels = new double[h, w];
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    labels[i, j] = -1;
                }
            }

            for (int iteration = 0; iteration < 10; iteration++)
            {
                double[,] lengths = new double[h, w];
                for (int ii = 0; ii < h; ii++)
                {
                    for (int j = 0; j < w; j++)
                    {
                        lengths[ii, j] = Double.MaxValue;
                    }
                }

                int i = 0;
                foreach (Center center in centers)
                {
                    for (int k = (int)Math.Round(center.X - S); k < (int)Math.Round(center.X + S); k++)
                        for (int l = (int)Math.Round(center.Y - S); l < (int)Math.Round(center.Y + S); l++)
                            if (k >= 0 && k < h && l >= 0 && l < w)
                            {
                                double L = lband[k, l];
                                double A = aband[k, l];
                                double B = labbband[k, l];

                                double Dc = Math.Sqrt(Math.Pow(L - center.L, 2) + Math.Pow(A - center.A, 2) + Math.Pow(B - center.B, 2));
                                double Ds = Math.Sqrt(Math.Pow(l - center.Y, 2) + Math.Pow(k - center.X, 2));
                                double length = Math.Sqrt(Math.Pow(Dc, 2) + Math.Pow(Ds / 2, 2) * Math.Pow(m, 2));

                                if (length < lengths[k, l])
                                {
                                    lengths[k, l] = length;
                                    labels[k, l] = i;
                                }
                            }
                    i++;
                }
                centers = calculateNewCenters(lband, aband, labbband, w, h, centers, labels);
            }

            //块内均值
            for (int y = 0; y < w; ++y)
                for (int x = 0; x < h; ++x)
                    if (labels[x, y] != -1)
                    {
                        lband[x, y] = centers[(int)Math.Floor(labels[x, y])].L;
                        aband[x, y] = centers[(int)Math.Floor(labels[x, y])].A;
                        labbband[x, y] = centers[(int)Math.Floor(labels[x, y])].B;
                    }


            //颜色空间转回
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    LABtoRGB(lband[i, j], aband[i, j], labbband[i, j], ref rband[i, j], ref gband[i, j], ref bband[i, j]);
                }
            }
        }

        private static Center[] calculateNewCenters(double[,] lband, double[,] aband, double[,] labbband, int w, int h, Center[] centers, double[,] labels)
        {
            Center[] newCenters = new Center[centers.Length];

            for (int i = 0; i < centers.Length; i++)
                newCenters[i] = new Center(0, 0, 0, 0, 0, 0);

            for (int y = 0; y < w; ++y)
                for (int x = 0; x < h; ++x)
                {
                    int centerIndex = (int)Math.Floor(labels[x, y]);
                    if (centerIndex != -1)
                    {
                        double L = lband[x, y];
                        double A = aband[x, y];
                        double B = labbband[x, y];

                        newCenters[centerIndex].X += x;
                        newCenters[centerIndex].Y += y;
                        newCenters[centerIndex].L += L;
                        newCenters[centerIndex].A += A;
                        newCenters[centerIndex].B += B;
                        newCenters[centerIndex].COUNT += 1;
                    }
                }

            foreach (Center center in newCenters)
            {
                if (center.COUNT != 0)
                {
                    center.X = Math.Round(center.X / center.COUNT);
                    center.Y = Math.Round(center.Y / center.COUNT);
                    center.L /= center.COUNT;
                    center.A /= center.COUNT;
                    center.B /= center.COUNT;
                }
            }

            return newCenters;
        }
    }

}
