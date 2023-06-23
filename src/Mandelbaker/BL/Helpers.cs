using MathNet.Numerics;
using System;

namespace Mandelbaker.BL
{
    public class Helpers
    {
        public static (double, double, double, double) AdaptCoordinatesToAspectRatio(
            int resolutionX,
            int resolutionY,
            double top,
            double bottom,
            double left,
            double right)
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

        public static void HSVtoRGB(double h, double s, double v, out int r, out int g, out int b)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
            double m = v - c;

            double r1 = 0;
            double g1 = 0;
            double b1 = 0;

            if (h >= 0 && h < 60)
            {
                r1 = c;
                g1 = x;
            }
            else if (h >= 60 && h < 120)
            {
                r1 = x;
                g1 = c;
            }
            else if (h >= 120 && h < 180)
            {
                g1 = c;
                b1 = x;
            }
            else if (h >= 180 && h < 240)
            {
                g1 = x;
                b1 = c;
            }
            else if (h >= 240 && h < 300)
            {
                r1 = x;
                b1 = c;
            }
            else if (h >= 300 && h < 360)
            {
                r1 = c;
                b1 = x;
            }

            r = Convert.ToInt32((r1 + m) * 255);
            g = Convert.ToInt32((g1 + m) * 255);
            b = Convert.ToInt32((b1 + m) * 255);
        }

        public static float Abs32(Complex32 x) // Needed because MathNet.Numerics.Complex32.Abs() is using double calculations internally
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
    }
}
