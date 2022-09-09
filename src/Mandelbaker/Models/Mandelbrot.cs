using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;

namespace Mandelbaker.Models
{
    public static class Mandelbrot
    {
        public static MandelbrotCalculationInformation SaveCPUMandelbrot(int resolution, int iterations, double xLeft, double yTop, double zoom, string directory, string? filename = null)
        {
            MandelbrotCalculationInformation calculationInformation = new(resolution, nameof(SaveCPUMandelbrot));
            calculationInformation.StartDateTime = DateTime.Now;
            (double xRight, double yBottom) = CorrectAspectRatio(xLeft, yTop, zoom);

            int[] mandelbrot = CalculateCPUMandelbrot(resolution, iterations, xLeft, xRight, yTop, yBottom);

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(resolution, resolution, PixelFormat.Format24bppRgb);
            Rectangle rect = new(0, 0, resolution, resolution);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - resolution * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, resolution, iHeight =>
            {
                Parallel.For(0, resolution, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * resolution + iWidth];
                    int index = (iHeight * resolution + iWidth) * 3 + strideBytes * iHeight;
                    rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(directory);
            bitmap.Save(directory + (filename == null ? $"MB_{resolution}px.png" : filename));

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }
        public static MandelbrotCalculationInformation SaveGPUMandelbrot(int resolution, int iterations, double xLeft, double yTop, double zoom, string directory, string? filename = null)
        {
            MandelbrotCalculationInformation calculationInformation = new(resolution, nameof(SaveGPUMandelbrot));
            calculationInformation.StartDateTime = DateTime.Now;
            (double xRight, double yBottom) = CorrectAspectRatio(xLeft, yTop, zoom);

            int[] mandelbrot = CalculateGPUMandelbrot(resolution, iterations, xLeft, xRight, yTop, yBottom);

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(resolution, resolution, PixelFormat.Format24bppRgb);
            Rectangle rect = new(0, 0, resolution, resolution);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - resolution * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, resolution, iHeight =>
            {
                Parallel.For(0, resolution, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * resolution + iWidth];
                    int index = (iHeight * resolution + iWidth) * 3 + strideBytes * iHeight;
                    rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(directory);
            bitmap.Save(directory + (filename == null ? $"MB_{resolution}px.png" : filename));

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }

        public static (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderMatrix(int resolution, int iterations, int dimensionSize, double xLeft, double yTop, double zoom, string directory, bool gpuAcceleration = true)
        {
            MandelbrotCalculationInformation mci = new(resolution, nameof(RenderMatrix));
            List<MandelbrotCalculationInformation> mcis = new();
            mci.StartDateTime = DateTime.Now;
            directory += @"Matrix\";

            for (int y = 0; y < dimensionSize; y++)
            {
                for (int x = 0; x < dimensionSize; x++)
                {
                    double partialXLeft = xLeft + 3 / zoom / dimensionSize * x;
                    double partialYTop = yTop + -3 / zoom / dimensionSize * y;
                    double partialZoom = zoom * dimensionSize;
                    string filename = $"MB_{resolution / dimensionSize}px_{y * dimensionSize + x}.png";
                    if (gpuAcceleration)
                        mcis.Add(SaveGPUMandelbrot(resolution / dimensionSize, iterations, partialXLeft, partialYTop, partialZoom, directory, filename));
                    else
                        mcis.Add(SaveCPUMandelbrot(resolution / dimensionSize, iterations, partialXLeft, partialYTop, partialZoom, directory, filename));
                }
            }

            mci.CalculationDoneDateTime = mci.StartDateTime;
            mci.EndDateTime = DateTime.Now;
            foreach (MandelbrotCalculationInformation ci in mcis)
            {
                mci.CalculationDoneDateTime = mci.CalculationDoneDateTime.AddSeconds(ci.CalculationTime);
            }

            return (mci, mcis);
        }
        public static (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderAnimation(int resolution, int iterations, double xLeft, double yTop, double zoom, string directory, int fps, int duration, double endZoom, bool gpuAcceleration = true)
        {   // ffmpeg -framerate 60 -i MB_500px_%d.png -pix_fmt yuv420p Animation2.mp4
            MandelbrotCalculationInformation mci = new(resolution, nameof(RenderAnimation));
            List<MandelbrotCalculationInformation> mcis = new();
            mci.StartDateTime = DateTime.Now;
            directory += @"Animation\";
            int frames = fps * duration;
            double zoomStep = Math.Pow(endZoom / zoom, 1.0 / frames);
            double currentZoom = zoom;

            for (int i = 0; i < frames; i++)
            {
                string filename = $"MB_{resolution}px_{i}.png";
                if (gpuAcceleration)
                    mcis.Add(SaveGPUMandelbrot(resolution, iterations, xLeft, yTop, currentZoom, directory, filename));
                else
                    mcis.Add(SaveCPUMandelbrot(resolution, iterations, xLeft, yTop, currentZoom, directory, filename));
                currentZoom *= zoomStep;
            }

            mci.CalculationDoneDateTime = mci.StartDateTime;
            mci.EndDateTime = DateTime.Now;
            foreach (MandelbrotCalculationInformation ci in mcis)
            {
                mci.CalculationDoneDateTime = mci.CalculationDoneDateTime.AddSeconds(ci.CalculationTime);
            }

            return (mci, mcis);
        }

        // TODO: Invert Y axis (currently inverted)
        public static int[] CalculateCPUMandelbrot(int resolution, int iterations, double xLeft, double xRight, double yTop, double yBottom)
        {
            int[] result = new int[resolution * resolution];
            Parallel.For(0, resolution, iHeight =>
            {
                double mandelHeight = iHeight * (yBottom - yTop) / resolution + yTop;
                Parallel.For(0, resolution, iWidth =>
                {
                    double mandelWidth = iWidth * (xRight - xLeft) / resolution + xLeft;
                    var z = new Complex(mandelWidth, mandelHeight);
                    var c = z;
                    int i;
                    for (i = 0; i < iterations; i++)
                    {
                        if (Complex.Abs(z) > 2)
                            break;
                        z = z * z + c;
                    }
                    result[(int)(iHeight * resolution + iWidth)] = i;
                });
            });

            return result;
        }
        public static int[] CalculateGPUMandelbrot(int resolution, int iterations, double xLeft, double xRight, double yTop, double yBottom)
        {
            Context context = Context.CreateDefault();
            Accelerator accelerator = context.CreateCudaAccelerator(0);

            int[] result = new int[resolution * resolution];
            var deviceOutput = accelerator.Allocate1D(new int[resolution * resolution]);

            for (int iHeight = 0; iHeight < resolution; iHeight++)
            {
                double mandelHeight = iHeight * (yBottom - yTop) / resolution + yTop;

                var deviceInput = accelerator.Allocate1D(new double[] { mandelHeight, resolution, xLeft, xRight, iterations, iHeight });
                var loadedKernel = accelerator.LoadAutoGroupedStreamKernel(
                (Index1D iWidth, ArrayView<double> data, ArrayView<int> output) =>
                {
                    double mandelWidth = iWidth * (data[3] - data[2]) / data[1] + data[2];
                    var z = new Complex(mandelWidth, data[0]);
                    var c = z;
                    int i;
                    for (i = 0; i < data[4]; i++)
                    {
                        if (Complex.Abs(z) > 2)
                            break;
                        z = z * z + c;
                    }
                    output[(int)(data[5] * data[1] + iWidth)] = i;
                });

                loadedKernel(resolution, deviceInput.View, deviceOutput.View);
            }

            result = deviceOutput.GetAsArray1D();
            accelerator.Dispose();
            context.Dispose();
            return result;
        }


        public static (double, double) CorrectAspectRatio(double xLeft, double yTop, double zoom)
        {
            double xRight = xLeft + 3 / zoom;
            double yBottom = yTop - 3 / zoom;
            return (xRight, yBottom);
        }

        #region obsolete
        //public MandelbrotCalculationInformation SaveGPUMandelbrotPrintSlow(int width, int height, int iterations)
        //{
        //    MandelbrotCalculationInformation calculationInformation = new(width, height, nameof(SaveGPUMandelbrotPrintSlow));
        //    calculationInformation.StartDateTime = DateTime.Now;

        //    int[] mandelbrot = CalculateGPUMandelbrot(width, height, iterations, XLeft, YTop, Zoom);

        //    calculationInformation.CalculationDoneDateTime = DateTime.Now;

        //    Bitmap bitmap = new(width, height, PixelFormat.Format24bppRgb);
        //    for (int iHeight = 0; iHeight < height; iHeight++)
        //    {
        //        for (int iWidth = 0; iWidth < width; iWidth++)
        //        {
        //            int pixelIterations = mandelbrot[iHeight * width + iWidth];
        //            bitmap.SetPixel(iWidth, iHeight, Color.FromArgb(255, pixelIterations % 3 * 64, pixelIterations % 8 * 32, pixelIterations % 16 * 16));
        //        }
        //    }

        //    bitmap.Save(Directory + (Filename == null ? $"Mandelbrot_{width}x{height}.png" : Filename));

        //    calculationInformation.EndDateTime = DateTime.Now;

        //    return calculationInformation;
        //}
        //public int[] CalculateCPUMandelbrotSingleThread(int width, int height, int iterations)
        //{
        //    CorrectAspectRatio();

        //    int[] result = new int[width * height];
        //    for (double iHeight = 0; iHeight < height; iHeight++)
        //    {
        //        double mandelHeight = iHeight * (YBottom - YTop) / height + YTop;
        //        for (double iWidth = 0; iWidth < width; iWidth++)
        //        {
        //            double mandelWidth = iWidth * (XRight - XLeft) / width + XLeft;
        //            var z = new Complex(mandelWidth, mandelHeight);
        //            var c = z;
        //            int i;
        //            for (i = 0; i < iterations; i++)
        //            {
        //                if (Complex.Abs(z) > 2)
        //                    break;
        //                z = z * z + c;
        //            }
        //            result[(int)(iHeight * width + iWidth)] = i;
        //        }
        //    }

        //    return result;
        //}
        //public MandelbrotCalculationInformation SaveCPUMandelbrotSingleThreadPrintSlow(int width, int height, int iterations)
        //{
        //    MandelbrotCalculationInformation calculationInformation = new(width, height, nameof(SaveCPUMandelbrotSingleThreadPrintSlow));
        //    calculationInformation.StartDateTime = DateTime.Now;

        //    int[] mandelbrot = CalculateCPUMandelbrotSingleThread(width, height, iterations);

        //    calculationInformation.CalculationDoneDateTime = DateTime.Now;

        //    Bitmap bitmap = new(width, height, PixelFormat.Format24bppRgb);
        //    for (int iHeight = 0; iHeight < height; iHeight++)
        //    {
        //        for (int iWidth = 0; iWidth < width; iWidth++)
        //        {
        //            int pixelIterations = mandelbrot[iHeight * width + iWidth];
        //            bitmap.SetPixel(iWidth, iHeight, Color.FromArgb(255, pixelIterations % 3 * 64, pixelIterations % 8 * 32, pixelIterations % 16 * 16));
        //        }
        //    }
        //    bitmap.Save(Directory + (Filename == null ? $"Mandelbrot_{width}x{height}.png" : Filename));

        //    calculationInformation.EndDateTime = DateTime.Now;

        //    return calculationInformation;
        //}
        //public MandelbrotCalculationInformation SaveCPUMandelbrotPrintSlow(int width, int height, int iterations)
        //{
        //    MandelbrotCalculationInformation calculationInformation = new(width, height, nameof(SaveCPUMandelbrotPrintSlow));
        //    calculationInformation.StartDateTime = DateTime.Now;

        //    int[] mandelbrot = CalculateCPUMandelbrot(width, height, iterations);

        //    calculationInformation.CalculationDoneDateTime = DateTime.Now;

        //    Bitmap bitmap = new(width, height, PixelFormat.Format24bppRgb);
        //    for (int iHeight = 0; iHeight < height; iHeight++)
        //    {
        //        for (int iWidth = 0; iWidth < width; iWidth++)
        //        {
        //            int pixelIterations = mandelbrot[iHeight * width + iWidth];
        //            bitmap.SetPixel(iWidth, iHeight, Color.FromArgb(255, pixelIterations % 3 * 64, pixelIterations % 8 * 32, pixelIterations % 16 * 16));
        //        }
        //    }

        //    bitmap.Save(Directory + (Filename == null ? $"Mandelbrot_{width}x{height}.png" : Filename));

        //    calculationInformation.EndDateTime = DateTime.Now;

        //    return calculationInformation;
        //}
        #endregion
    }
}
