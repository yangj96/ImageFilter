## ImageFilter

##### 1.     项目简述 #####

本次项目包括Lomo滤镜和晶格化两个滤镜效果，使用C# WPF实现。

###### 1.1 Lomo滤镜 ######

Lomo滤镜的特点是色彩浓郁、高饱和度并常常伴随照片暗角，因此实现该滤镜时通过柔光混合、亮度对比度增强以及暗角模板叠加多个步骤组合完成。此类风格滤镜的实现大同小异，更多滤镜的算法公式可参考 https://www.jianshu.com/p/03450bce19d5。

首先将原图进行图层混合处理，结果色根据原图各点RGB通道数值选择不同的计算公式，从而使得通道数值大于50%灰色的像素点的结果色比原图稍亮，而数值小于或等于50%灰色的像素点结果色就比原图稍暗， 具体计算公式如下：

$ C=\begin{cases} A + (2 *B - 255) * (A -A *A / 255) / 255 \;\; if\; B <=128 \\ A + (2 *B - 255) * (Sqrt(A/255)*255 -A)/255 \;otherwise \end{cases}$

然后分别对混合结果进行对比度增强和亮度增强。上述步骤的具体实现方法如下：
```C#
private static byte ModeSmoothLight(byte basePixel, byte mixPixel)
 {
     int res = 0;
     res = mixPixel > 128 ? ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((Math.Sqrt((float)basePixel / 255.0f)) * 255.0f - (float)basePixel) / 255.0f)) :
     ((int)((float)basePixel + ((float)mixPixel + (float)mixPixel - 255.0f) * ((float)basePixel - (float)basePixel * (float)basePixel / 255.0f) / 255.0f));
     return (byte)Math.Min(255, Math.Max(0, res));
 }

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
```
最后将黑色的椭圆暗角模板与上述结果进行叠加。暗角特效在Vignette类中实现，主要的算法实现原理是首先建立两个以图像中心为中心的椭圆边界将图像划分为Zone A，Zone B和Zone C三个区域，然后根据图像中各像素点在椭圆边界内外的判断对像素点进行操作：Zone A保持原始图像的像素值，Zone C完全使用纯黑色的边角颜色，Zone B则进行暗角和原图的融合叠加。为了实现融合区域的渐变效果，Zone B建立了一系列以图像中心为中心的椭圆，各个相邻椭圆间的区域根据其边界的坐标距离使用不同的融合权重系数以实现中间亮到四个角渐暗的暗角效果，具体的融合公式如下所示：

$P=l_1*w_1+l_2*w_2$
$w_1 = 1/2(1+\cos(\pi s/d)), w_2 = 1/2(1-\cos(\pi s/d)),$

关于渐变暗角的融合权重系数的代码具体实现如下：

``` C#
List<double> aVals; //融合区域系列椭圆的宽向坐标          
 List<double> bVals; //融合区域系列椭圆的高向坐标          
 List<double> aValsMidPoints; 
 List<double> bValsMidPoints; 
 List<double> imageWeight;    // 原图融合权重系数 
 List<double> vignetteWeight; // 暗角融合权重系数 

 double a0 = vignetteWidthHalf - bandPixelsHalf; //融合区域系列椭圆宽向起始坐标 
 double b0 = vignetteHeightHalf - bandPixelsHalf; //融合区域系列椭圆高向起始坐标 

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
```

###### 1.2 晶格化效果 ######

晶格化效果的实现使用SLIC简单线性迭代聚类算法实现超像素分割然后对分割后各像素内的点进行像素平均。

SLIC算法使用k-means聚类实现，首先根据用户界面设置的聚类中心个数的超参数初始化k个聚类中心，也即超像素中心，将其均匀分布到图像的像素点上。初始化label数组保存每一个像素点所属的超像素标签，初始化lengths数组保存各个像素点到所属的超像素中心的距离。

如果图片包含N个像素，要分割成K个超像素，那么每个超像素的大小是N/K ，超像素之间的距离$S=\sqrt{N/K}$。为了将算法复杂度降低为O(n)，在聚类时将搜索区域限制在2S*2S范围内。聚类的目标是使各个像素到所属的超像素中心的距离之和最小。实现时首先将图像的RGB颜色转换为LAB颜色空间，使得计算距离时能够同时考虑LAB颜色信息和XY距离信息，并可以通过用户界面设置的M值调整颜色和距离的比重。

对每一个超像素中心x，搜索它2S*2S范围内的点：如果点到超像素中心x的5维信息的距离小于这个点到它原来所属的超像素中心的距离，那么说明这个点属于超像素x，更新lengths数组和label数组，然后对每个聚类中心，找到所有label值为该聚类中心的点，求他们的平均值从而更新得到k个新的聚类中心。上述过程迭代十次，该部分核心实现代码如下所示：
```c#
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
```

##### 2.     运行环境配置       #####

运行环境：windows操作系统 .Net Framework 4.6.1

双击 ./ImageFilter.exe即可运行，注意需要保证用于空间颜色转换的ColorMine.dll与当前可执行程序位于同一目录下。

##### 3.     程序运行结果 #####

Lomo滤镜和晶格化效果的运行结果分别如下图所示：

![](https://upload-images.jianshu.io/upload_images/2764802-98d2bbfd3946a3bd.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

图3.1  Lomo滤镜运行结果截图

![](https://upload-images.jianshu.io/upload_images/2764802-ebe07c50afd20626.png?imageMogr2/auto-orient/strip%7CimageView2/2/w/1240)

图3.2 聚类个数=800，M=2时 晶格化效果运行结果截图
