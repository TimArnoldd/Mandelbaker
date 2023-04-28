using ILGPU;
using ILGPU.Runtime;
using Mandelbaker.Enums;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace Mandelbaker.Models
{
    public static class Mandelbrot
    {
        private static Context? _context;
        private static Device? _device;
        private static Accelerator? _accelerator;
        private static Action<Index1D, ArrayView<double>, ArrayView<int>>? _loadedDoubleKernel;
        private static Action<Index1D, ArrayView<float>, ArrayView<int>>? _loadedFloatKernel;

        public static void Initialize()
        {
            SetupAccelerator();
        }

        public static MandelbrotCalculationInformation SaveCPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right, string directory, string? filename = null)
        {
            MandelbrotCalculationInformation calculationInformation = new(resolutionX, resolutionY, nameof(SaveCPUMandelbrot));
            calculationInformation.StartDateTime = DateTime.Now;
            (double aTop, double aBottom, double aLeft, double aRight) = AdaptToAspectRatio(resolutionX, resolutionY, top, bottom, left, right);

            int[] mandelbrot = CalculateCPUMandelbrot(resolutionX, resolutionY, iterations, aTop, aBottom, aLeft, aRight);

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(resolutionX, resolutionY, PixelFormat.Format24bppRgb); // Max bitmap size is 715'776'516 pixels
            Rectangle rect = new(0, 0, resolutionX, resolutionY);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - resolutionY * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, resolutionY, iHeight =>
            {
                Parallel.For(0, resolutionX, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * resolutionX + iWidth];
                    int index = (iHeight * resolutionY + iWidth) * 3 + strideBytes * iHeight;
                    rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(directory);
            bitmap.Save(directory + (filename == null ? $"MB_{resolutionX}x{resolutionY}.png" : filename));
            bitmap.Dispose();

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }
        public static MandelbrotCalculationInformation SaveDoubleGPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right, string directory, string? filename = null)
        {
            MandelbrotCalculationInformation calculationInformation = new(resolutionX, resolutionY, nameof(SaveDoubleGPUMandelbrot));
            calculationInformation.StartDateTime = DateTime.Now;
            (double aTop, double aBottom, double aLeft, double aRight) = AdaptToAspectRatio(resolutionX, resolutionY, top, bottom, left, right);

            int[] mandelbrot = CalculateDoubleGPUMandelbrot(resolutionX, resolutionY, iterations, aTop, aBottom, aLeft, aRight);

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(resolutionX, resolutionY, PixelFormat.Format24bppRgb);
            Rectangle rect = new(0, 0, resolutionX, resolutionY);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - resolutionY * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, resolutionY, iHeight =>
            {
                Parallel.For(0, resolutionX, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * resolutionX + iWidth];
                    int index = (iHeight * resolutionY + iWidth) * 3 + strideBytes * iHeight;
                    rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(directory);
            bitmap.Save(directory + (filename == null ? $"MB_{resolutionX}x{resolutionY}.png" : filename));
            bitmap.Dispose();

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }
        public static MandelbrotCalculationInformation SaveFloatGPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right, string directory, string? filename = null)
        {
            MandelbrotCalculationInformation calculationInformation = new(resolutionX, resolutionY, nameof(SaveFloatGPUMandelbrot));
            calculationInformation.StartDateTime = DateTime.Now;
            (double aTop, double aBottom, double aLeft, double aRight) = AdaptToAspectRatio(resolutionX, resolutionY, top, bottom, left, right);

            int[] mandelbrot = CalculateFloatGPUMandelbrot(resolutionX, resolutionY, iterations, aTop, aBottom, aLeft, aRight);

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(resolutionX, resolutionY, PixelFormat.Format24bppRgb);
            Rectangle rect = new(0, 0, resolutionX, resolutionY);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - resolutionY * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, resolutionY, iHeight =>
            {
                Parallel.For(0, resolutionX, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * resolutionX + iWidth];
                    int index = (iHeight * resolutionY + iWidth) * 3 + strideBytes * iHeight;
                    rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(directory);
            bitmap.Save(directory + (filename == null ? $"MB_{resolutionX}x{resolutionY}.png" : filename));
            bitmap.Dispose();

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }

        public static (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderMatrix(int resolutionX, int resolutionY, int iterations, int dimensionSize, double top, double bottom, double left, double right, string directory, CalculationMethod method)
        {
            MandelbrotCalculationInformation mci = new(resolutionX, resolutionY, nameof(RenderMatrix));
            List<MandelbrotCalculationInformation> mcis = new();
            mci.StartDateTime = DateTime.Now;
            directory += @"Matrix\";
            (double aTop, double aBottom, double aLeft, double aRight) = AdaptToAspectRatio(resolutionX, resolutionY, top, bottom, left, right);

            for (int y = 0; y < dimensionSize; y++)
            {
                double partialTop = aTop - (aTop - aBottom) / dimensionSize * y;
                double partialBottom = partialTop - (aTop - aBottom) / dimensionSize;
                for (int x = 0; x < dimensionSize; x++)
                {
                    double partialLeft = aLeft + (aRight - aLeft) / dimensionSize * x;
                    double partialRight = partialLeft + (aRight + aLeft) / dimensionSize;
                    string filename = $"MB_{resolutionX / dimensionSize}x{resolutionY / dimensionSize}_{y * dimensionSize + x}.png";

                    switch (method)
                    {
                        case CalculationMethod.CPU:
                            mcis.Add(SaveCPUMandelbrot(resolutionX / dimensionSize, resolutionY / dimensionSize, iterations, partialTop, partialBottom, partialLeft, partialRight, directory, filename));
                            break;
                        case CalculationMethod.GPUFloat:
                            mcis.Add(SaveFloatGPUMandelbrot(resolutionX / dimensionSize, resolutionY / dimensionSize, iterations, partialTop, partialBottom, partialLeft, partialRight, directory, filename));
                            break;
                        case CalculationMethod.GPUDouble:
                            mcis.Add(SaveDoubleGPUMandelbrot(resolutionX / dimensionSize, resolutionY / dimensionSize, iterations, partialTop, partialBottom, partialLeft, partialRight, directory, filename));
                            break;
                    }
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
        //public static (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderAnimation(int resolutionX, int resolutionY, int iterations, double xLeft, double yTop, double zoom, string directory, int fps, int duration, double endZoom, bool gpuAcceleration = true)
        //{   // ffmpeg -framerate 60 -i MB_500px_%d.png -pix_fmt yuv420p Animation2.mp4
        //    MandelbrotCalculationInformation mci = new(resolutionX, resolutionY, nameof(RenderAnimation));
        //    List<MandelbrotCalculationInformation> mcis = new();
        //    mci.StartDateTime = DateTime.Now;
        //    directory += @"Animation\";
        //    int frames = fps * duration;
        //    double zoomStep = Math.Pow(endZoom / zoom, 1.0 / frames);
        //    double currentZoom = zoom;

        //    for (int i = 0; i < frames; i++)
        //    {
        //        string filename = $"MB_{resolutionX}x{resolutionY}_{i}.png";
        //        if (gpuAcceleration)
        //            mcis.Add(SaveGPUMandelbrot(resolutionX, resolutionY, iterations, xLeft, yTop, currentZoom, directory, filename));
        //        else
        //            mcis.Add(SaveCPUMandelbrot(resolutionX, resolutionY, iterations, xLeft, yTop, currentZoom, directory, filename));
        //        currentZoom *= zoomStep;
        //    }

        //    mci.CalculationDoneDateTime = mci.StartDateTime;
        //    mci.EndDateTime = DateTime.Now;
        //    foreach (MandelbrotCalculationInformation ci in mcis)
        //    {
        //        mci.CalculationDoneDateTime = mci.CalculationDoneDateTime.AddSeconds(ci.CalculationTime);
        //    }

        //    return (mci, mcis);
        //}

        private static int[] CalculateCPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right)
        {
            int[] result = new int[resolutionX * resolutionY];

            Parallel.For(0, resolutionX * resolutionY, index =>
            {
                int iHeight = index / resolutionX;
                int iWidth = index % resolutionX;

                double mandelHeight = top - (top - bottom) * iHeight / resolutionY;
                double mandelWidth = left + (right - left) * iWidth / resolutionX;

                var z = new Complex(mandelWidth, mandelHeight);
                var c = z;
                int i;
                for (i = 0; i < iterations; i++)
                {
                    if (Complex.Abs(z) > 2)
                        break;
                    z = z * z + c;
                }
                result[index] = i;
            });

            return result;
        }
        private static int[] CalculateDoubleGPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right)
        {
            if (_context == null ||
                _accelerator == null ||
                _loadedDoubleKernel == null)
                throw new Exception("GPU Acceleration with double values did not initialize correctly.");

            int[] result = new int[resolutionX * resolutionY];
            var deviceOutput = _accelerator.Allocate1D(new int[resolutionX * resolutionY]);
            var deviceInput = _accelerator.Allocate1D(new double[] { resolutionX, resolutionY, iterations, top, bottom, left, right });

            _loadedDoubleKernel(resolutionX * resolutionY, deviceInput.View, deviceOutput.View);

            result = deviceOutput.GetAsArray1D();
            return result;
        }
        private static int[] CalculateFloatGPUMandelbrot(int resolutionX, int resolutionY, int iterations, double top, double bottom, double left, double right)
        {
            if (_context == null ||
                _accelerator == null ||
                _loadedFloatKernel == null)
                throw new Exception("GPU Acceleration with float values did not initialize correctly.");

            int[] result = new int[resolutionX * resolutionY];
            var deviceOutput = _accelerator.Allocate1D(new int[resolutionX * resolutionY]);
            var deviceInput = _accelerator.Allocate1D(new float[] { resolutionX, resolutionY, iterations, (float)top, (float)bottom, (float)left, (float)right });

            _loadedFloatKernel(resolutionX * resolutionY, deviceInput.View, deviceOutput.View);

            result = deviceOutput.GetAsArray1D();
            return result;
        }


        private static (double, double, double, double) AdaptToAspectRatio(int resolutionX, int resolutionY, double top, double bottom, double left, double right)
        {
            double targetRatio = (double)resolutionX / resolutionY;
            double currentRatio = (right - left) / (top - bottom);
            if (targetRatio > currentRatio) // = targetRatio wider
            {
                double width = (top - bottom) * targetRatio;
                double deltaWidth = width - (right - left);
                right += deltaWidth / 2;
                left -= deltaWidth / 2;
            }
            else if (targetRatio < currentRatio) // = targetRatio taller
            {
                double height = (right - left) / targetRatio;
                double deltaHeight = height - (top - bottom);
                top += deltaHeight / 2;
                bottom -= deltaHeight / 2;
            }
            return (top, bottom, left, right);
        }

        private static void SetupAccelerator()
        {
            _context = Context.CreateDefault();
            _device = _context.Devices.Where(x => x.AcceleratorType != AcceleratorType.CPU).FirstOrDefault();
            if (_device == null)
            {
                MessageBox.Show("No compatible graphics processor could be found.", "GPU initialization failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _accelerator = _device.CreateAccelerator(_context);
            try
            {
                _loadedFloatKernel = _accelerator.LoadAutoGroupedStreamKernel(
                (Index1D index, ArrayView<float> input, ArrayView<int> output) =>
                {
                    int iHeight = index / (int)input[0];
                    int iWidth = index % (int)input[0];

                    float mandelHeight = input[3] - (input[3] - input[4]) * iHeight / input[1];
                    float mandelWidth = input[5] + (input[6] - input[5]) * iWidth / input[0];

                    var z = new Complex32(mandelWidth, mandelHeight);
                    var c = z;
                    int i;
                    for (i = 0; i < input[2]; i++)
                    {
                        if (Abs(z) > 2)
                            break;
                        z = z * z + c;
                    }
                    output[index] = i; // cast to int?
                });
                _loadedDoubleKernel = _accelerator.LoadAutoGroupedStreamKernel(
                (Index1D index, ArrayView<double> input, ArrayView<int> output) =>
                {
                    int iHeight = index / (int)input[0];
                    int iWidth = index % (int)input[0];

                    double mandelHeight = input[3] - (input[3] - input[4]) * iHeight / input[1];
                    double mandelWidth = input[5] + (input[6] - input[5]) * iWidth / input[0];

                    var z = new Complex(mandelWidth, mandelHeight);
                    var c = z;
                    int i;
                    for (i = 0; i < input[2]; i++)
                    {
                        if (Complex.Abs(z) > 2)
                            break;
                        z = z * z + c;
                    }
                    output[index] = i; // cast to int?
                });
            }
            catch (Exception ex)
            {
                string messageBoxText = "GPU Acceleration may not be fully supported:\n";
                if (ex.InnerException is CapabilityNotSupportedException cnse)
                {
                    messageBoxText += cnse.Message;
                }
                else
                {
                    messageBoxText += $"{ex.Message}\n{ex.InnerException?.Message}";
                }
                MessageBox.Show(messageBoxText, "GPU initialization failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static float Abs(Complex32 x) // Needed because MathNet.Numerics.Complex32.Abs() is using double calculations internally
        {
            if (float.IsNaN(x.Real) || float.IsNaN(x.Imaginary))
            {
                return float.NaN;
            }

            if (float.IsInfinity(x.Real) || float.IsInfinity(x.Imaginary))
            {
                return float.PositiveInfinity;
            }

            float num = MathF.Abs(x.Real);
            float num2 = MathF.Abs(x.Imaginary);
            if (num > num2)
            {
                float num3 = num2 / num;
                return num * MathF.Sqrt(1.0f + num3 * num3);
            }

            if (num == 0f)
            {
                return num2;
            }

            float num4 = num / num2;
            return num2 * MathF.Sqrt(1.0f + num4 * num4);
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
