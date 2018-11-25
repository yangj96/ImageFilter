using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ImageFilter
{
    class VignetteEffect
    {
        List<double> aVals; //融合区域系列椭圆的宽向坐标         
        List<double> bVals; //融合区域系列椭圆的高向坐标         
        List<double> aValsMidPoints;
        List<double> bValsMidPoints;
        List<double> imageWeight;    // 原图融合权重
        List<double> vignetteWeight; // 暗角融合权重
        double coverage = 97.5;
        int bandPixels;
        int numberSteps;

        public VignetteEffect()
        {
            aVals = new List<double>();
            bVals = new List<double>();
            aValsMidPoints = new List<double>();
            bValsMidPoints = new List<double>();     
            imageWeight = new List<double>();
            vignetteWeight = new List<double>();
        }

        private void SetupParameters(int width, int height)
        {
            aVals.Clear();
            bVals.Clear();
            aValsMidPoints.Clear();
            bValsMidPoints.Clear();
            imageWeight.Clear();
            vignetteWeight.Clear();

            bandPixels = (width + height) / 4;
            numberSteps = 100;

            double a0, b0, aEll, bEll;
            double stepSize = bandPixels * 1.0 / numberSteps;
            double bandPixelsHalf = 0.5 * bandPixels;
            double vignetteWidth = width * coverage / 100.0;
            double vignetteHeight = height * coverage / 100.0;
            double vignetteWidthHalf = vignetteWidth * 0.5;
            double vignetteHeightHalf = vignetteHeight * 0.5;
            a0 = vignetteWidthHalf - bandPixelsHalf; //融合区域系列椭圆宽向起始坐标
            b0 = vignetteHeightHalf - bandPixelsHalf; //融合区域系列椭圆高向起始坐标

            //计算融合区域系列椭圆的坐标数组
            for (int i = 0; i <= numberSteps; ++i)
            {
                aEll = a0 + stepSize * i;
                bEll = b0 + stepSize * i;
                aVals.Add(aEll);
                bVals.Add(bEll);
            }
            for (int i = 0; i < numberSteps; ++i)
            {
                aEll = a0 + stepSize * (i + 0.5);
                bEll = b0 + stepSize * (i + 0.5);
                aValsMidPoints.Add(aEll);
                bValsMidPoints.Add(bEll);
            }

            // 计算融合权重
            double weight1, weight2, arg, argCosVal;
            double arguFactor = Math.PI / bandPixels;
            for (int i = 0; i < numberSteps; ++i)
            {
                arg = arguFactor * (aValsMidPoints[i] - a0);
                argCosVal = Math.Cos(arg);
                weight1 = 0.5 * (1.0 + argCosVal);
                weight2 = 0.5 * (1.0 - argCosVal);
                imageWeight.Add(weight1);
                vignetteWeight.Add(weight2);
            }
        }

        public void ApplyEffect(int width, int height, ref byte[,] rband, ref byte[,] gband, ref byte[,] bband)
        {
            SetupParameters(width, height);
            for (int el = 0; el < height; ++el)
            {
                for (int k = 0; k < width; ++k)
                {
                    double widthHalf = width * 0.5;
                    double heightHalf = height * 0.5;

                    double xprime = k - widthHalf;
                    double yprime = el - heightHalf;

                    double factor1 = 1.0 * Math.Abs(xprime) / aVals[0];
                    double factor2 = 1.0 * Math.Abs(yprime) / bVals[0];
                    double factor3 = 1.0 * Math.Abs(xprime) / aVals[numberSteps];
                    double factor4 = 1.0 * Math.Abs(yprime) / bVals[numberSteps];

                    //判断点在椭圆内外 
                    double potential1 = factor1 * factor1 + factor2 * factor2 - 1.0;
                    double potential2 = factor3 * factor3 + factor4 * factor4 - 1.0;

                    byte r, g, b;
                    byte redBorder = 0;
                    byte greenBorder = 0;
                    byte blueBorder = 0;
                    if (potential1 <= 0.0)
                    {
                        // 点在椭圆内
                        r = rband[el, k];
                        g = gband[el, k];
                        b = bband[el, k];
                    }
                    else if (potential2 >= 0.0)
                    {
                        // 点在椭圆外
                        r = redBorder;
                        g = greenBorder;
                        b = blueBorder;
                    }
                    else
                    {
                        // 点在融合区域
                        int j, j1;

                        for (j = 1; j < numberSteps; ++j)
                        {
                            factor1 = Math.Abs(xprime) / aVals[j];
                            factor2 = Math.Abs(yprime) / bVals[j];

                            double potential = factor1 * factor1 + factor2 * factor2 - 1.0;
                            if (potential < 0.0) break;
                        }
                        j1 = j - 1;
                        r = (byte)(rband[el, k] * imageWeight[j1] + redBorder * vignetteWeight[j1]);
                        g = (byte)(gband[el, k] * imageWeight[j1] + greenBorder * vignetteWeight[j1]);
                        b = (byte)(bband[el, k] * imageWeight[j1] + blueBorder * vignetteWeight[j1]);
                    }
                    rband[el, k] = r;
                    gband[el, k] = g;
                    bband[el, k] = b;
                }
            }
        }
    }
}
