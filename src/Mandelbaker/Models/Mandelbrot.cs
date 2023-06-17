using FFMpegCore;
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
    public class Mandelbrot
    {
        private Context? _context;
        private Device? _device;
        private Accelerator? _accelerator;
        private Action<Index1D, ArrayView<double>, ArrayView<int>>? _loadedDoubleKernel;
        private Action<Index1D, ArrayView<float>, ArrayView<int>>? _loadedFloatKernel;

        public MandelbrotParameters Parameters { get; set; }
        private MandelbrotParameters P => Parameters;


        public Mandelbrot(MandelbrotParameters parameters)
        {
            Parameters = parameters;
            SetupAccelerator();
        }


        public MandelbrotCalculationInformation SaveMandelbrot() => SaveMandelbrot(P);
        public MandelbrotCalculationInformation SaveMandelbrot(MandelbrotParameters p)
        {
            MandelbrotCalculationInformation calculationInformation = new(p.ResolutionX, p.ResolutionY, p.Method.ToString())
            {
                StartDateTime = DateTime.Now
            };
            (p.Top, p.Bottom, p.Left, p.Right) = Helpers.AdaptCoordinatesToAspectRatio(p.ResolutionX, p.ResolutionY, p.Top, p.Bottom, p.Left, p.Right);

            int[] mandelbrot = p.Method switch
            {
                CalculationMethod.GPUDouble => CalculateDoubleGPUMandelbrot(p),
                CalculationMethod.GPUFloat => CalculateFloatGPUMandelbrot(p),
                _ => CalculateCPUMandelbrot(p),
            };

            calculationInformation.CalculationDoneDateTime = DateTime.Now;

            Bitmap bitmap = new(p.ResolutionX, p.ResolutionY, PixelFormat.Format24bppRgb); // Max bitmap size is 715'776'516 pixels
            Rectangle rect = new(0, 0, p.ResolutionX, p.ResolutionY);
            BitmapData bmpData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            int strideBytes = bmpData.Stride - p.ResolutionY * 3;
            byte[] rgbValues = new byte[bytes];

            Parallel.For(0, p.ResolutionY, iHeight =>
            {
                Parallel.For(0, p.ResolutionX, iWidth =>
                {
                    int pixelIterations = mandelbrot[iHeight * p.ResolutionX + iWidth];
                    int index = (iHeight * p.ResolutionY + iWidth) * 3 + strideBytes * iHeight;

                    int r, g, b;
                    if (pixelIterations == p.Iterations)
                        Helpers.HSVtoRGB(0, 0, 0, out r, out g, out b);
                    else
                        Helpers.HSVtoRGB(pixelIterations % 256 / 255.0 * 359, 1, 1, out r, out g, out b);

                    rgbValues[index] = (byte)b;     // B
                    rgbValues[index + 1] = (byte)g; // G
                    rgbValues[index + 2] = (byte)r; // R

                    // Old color scheme
                    //rgbValues[index] = (byte)(pixelIterations % 16 * 16);       // B
                    //rgbValues[index + 1] = (byte)(pixelIterations % 8 * 32);    // G
                    //rgbValues[index + 2] = (byte)(pixelIterations % 3 * 64);    // R
                });
            });
            Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(bmpData);

            Directory.CreateDirectory(p.Directory);
            bitmap.Save(p.Directory + (p.Filename ?? $"MB_{p.ResolutionX}x{p.ResolutionY}.png"));
            bitmap.Dispose();

            calculationInformation.EndDateTime = DateTime.Now;

            return calculationInformation;
        }

        public (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderMatrix()
        {
            MandelbrotCalculationInformation mci = new(P.ResolutionX, P.ResolutionY, nameof(RenderMatrix));
            List<MandelbrotCalculationInformation> mcis = new();
            mci.StartDateTime = DateTime.Now;
            (P.Top, P.Bottom, P.Left, P.Right) = Helpers.AdaptCoordinatesToAspectRatio(P.ResolutionX, P.ResolutionY, P.Top, P.Bottom, P.Left, P.Right);
            var parameters = P.Clone();
            parameters.Directory = P.Directory += @"Matrix\";
            parameters.ResolutionX = P.ResolutionX / P.MatrixDimensionSize;
            parameters.ResolutionY = P.ResolutionY / P.MatrixDimensionSize;

            for (int y = 0; y < P.MatrixDimensionSize; y++)
            {
                parameters.Top = P.Top - (P.Top - P.Bottom) / P.MatrixDimensionSize * y;
                parameters.Bottom = parameters.Top - (P.Top - P.Bottom) / P.MatrixDimensionSize;
                for (int x = 0; x < P.MatrixDimensionSize; x++)
                {
                    parameters.Left = P.Left + (P.Right - P.Left) / P.MatrixDimensionSize * x;
                    parameters.Right = parameters.Left + (P.Right - P.Left) / P.MatrixDimensionSize;
                    parameters.Filename = $"MB_{parameters.ResolutionX}x{parameters.ResolutionY}_{y * P.MatrixDimensionSize + x}.png";

                    mcis.Add(SaveMandelbrot(parameters));
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

        public (MandelbrotCalculationInformation, List<MandelbrotCalculationInformation>) RenderAnimation()
        {
            (double startTop, double startBottom, double startLeft, double startRight) = Helpers.AdaptCoordinatesToAspectRatio(P.ResolutionX, P.ResolutionY, P.Top, P.Bottom, P.Left, P.Right);
            int frameCount = P.AnimationFps * P.AnimationDuration;
            var parameters = P.Clone();
            parameters.Directory = P.Directory + @"Animation\";

            if (P.AnimationCleanDirectory &&
                Directory.Exists(parameters.Directory))
                Directory.Delete(parameters.Directory, true);

            MandelbrotCalculationInformation mci = new(P.ResolutionX, P.ResolutionY, nameof(RenderAnimation));
            List<MandelbrotCalculationInformation> mcis = new();
            mci.StartDateTime = DateTime.Now;

            double startZoom = 1 / ((startTop - startBottom) / 3);
            double zoomStep = Math.Pow(P.AnimationEndZoom / startZoom, 1.0 / (frameCount - 1));
            List<string> filenames = new();

            for (int i = 0; i < frameCount; i++)
            {
                string filename = $"MB_{P.ResolutionX}x{P.ResolutionY}_{i}.png";
                parameters.Filename = filename;
                filenames.Add(parameters.Directory + parameters.Filename);

                double currentZoom = startZoom * Math.Pow(zoomStep, i);
                parameters.Top = P.AnimationEndY + 1.5 / currentZoom;
                parameters.Bottom = P.AnimationEndY - 1.5 / currentZoom;
                parameters.Left = P.AnimationEndX - 1.5 / currentZoom;
                parameters.Right = P.AnimationEndX + 1.5 / currentZoom;

                mcis.Add(SaveMandelbrot(parameters));
            }

            // ffmpeg -r 60 -i MB_3840x2160_%d.png -pix_fmt yuv420p Animation.mp4
            _ = FFMpegArguments
                .FromConcatInput(filenames, options => options.WithFramerate(P.AnimationFps))
                .OutputToFile($"{P.Directory}Animation_{P.ResolutionX}x{P.ResolutionY}_{P.AnimationDuration}s_{P.AnimationFps}Fps_{P.AnimationEndX.Round(3)}x{P.AnimationEndY.Round(3)}.mp4", true, options => options
                    .ForcePixelFormat("yuv420p"))
                .ProcessSynchronously();

            if (P.AnimationCleanDirectory)
                Directory.Delete(parameters.Directory, true);

            mci.CalculationDoneDateTime = DateTime.Now;
            mci.EndDateTime = mci.CalculationDoneDateTime;

            return (mci, mcis);
        }

        private int[] CalculateCPUMandelbrot(MandelbrotParameters p)
        {
            int[] result = new int[p.ResolutionX * p.ResolutionY];

            Parallel.For(0, p.ResolutionX * p.ResolutionY, index =>
            {
                int iHeight = index / p.ResolutionX;
                int iWidth = index % p.ResolutionX;

                double mandelHeight = p.Top - (p.Top - p.Bottom) * iHeight / p.ResolutionY;
                double mandelWidth = p.Left + (p.Right - p.Left) * iWidth / p.ResolutionX;

                var z = new Complex(mandelWidth, mandelHeight);
                var c = z;
                int i;
                for (i = 0; i < p.Iterations; i++)
                {
                    if (Complex.Abs(z) > 2)
                        break;
                    z = z * z + c;
                }
                result[index] = i;
            });

            return result;
        }
        private int[] CalculateDoubleGPUMandelbrot(MandelbrotParameters p)
        {
            if (_context == null ||
                _accelerator == null ||
                _loadedDoubleKernel == null)
                throw new Exception("GPU Acceleration with double values did not initialize correctly.");

            int[] result = new int[p.ResolutionX * p.ResolutionY];
            var deviceOutput = _accelerator.Allocate1D(new int[p.ResolutionX * p.ResolutionY]);
            var deviceInput = _accelerator.Allocate1D(new double[] { p.ResolutionX, p.ResolutionY, p.Iterations, p.Top, p.Bottom, p.Left, p.Right });

            _loadedDoubleKernel(p.ResolutionX * p.ResolutionY, deviceInput.View, deviceOutput.View);

            result = deviceOutput.GetAsArray1D();
            return result;
        }
        private int[] CalculateFloatGPUMandelbrot(MandelbrotParameters p)
        {
            if (_context == null ||
                _accelerator == null ||
                _loadedFloatKernel == null)
                throw new Exception("GPU Acceleration with float values did not initialize correctly.");

            int[] result = new int[p.ResolutionX * p.ResolutionY];
            var deviceOutput = _accelerator.Allocate1D(new int[p.ResolutionX * p.ResolutionY]);
            var deviceInput = _accelerator.Allocate1D(new float[] { p.ResolutionX, p.ResolutionY, p.Iterations, (float)p.Top, (float)p.Bottom, (float)p.Left, (float)p.Right });

            _loadedFloatKernel(p.ResolutionX * p.ResolutionY, deviceInput.View, deviceOutput.View);

            result = deviceOutput.GetAsArray1D();
            return result;
        }


        private void SetupAccelerator()
        {
            _context = Context.CreateDefault();
            var devices = _context.Devices.Where(x => x.AcceleratorType != AcceleratorType.CPU);
            if (!devices.Any())
            {
                MessageBox.Show("No compatible graphics processor could be found.", "GPU initialization failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (devices.Count() == 1)
            {
                _device = devices.First();
            }
            else
            {
                DeviceSelector deviceSelector = new(devices.Select(x => x.Name).ToList());
                if (deviceSelector.ShowDialog() != true)
                {
                    MessageBox.Show("Graphics processor could not be set.", "GPU initialization failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                _device = devices.First(x => x.Name == deviceSelector.SelectedDeviceName);
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
                        if (Helpers.Abs32(z) > 2)
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
    }
}
