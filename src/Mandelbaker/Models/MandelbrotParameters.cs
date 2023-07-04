using Mandelbaker.Enums;

namespace Mandelbaker.Models
{
    public class MandelbrotParameters
    {
        public int ResolutionX { get; set; }
        public int ResolutionY { get; set; }
        public int Iterations { get; set; }
        public double Top { get; set; }
        public double Bottom { get; set; }
        public double Left { get; set; }
        public double Right { get; set; }
        public string Directory { get; set; } = string.Empty;
        public string? Filename { get; set; }
        public CalculationMethod Method { get; set; }

        public int MatrixDimensionSize { get; set; }

        public int AnimationFps { get; set; }
        public int AnimationDuration { get; set; }
        public double AnimationEndX { get; set; }
        public double AnimationEndY { get; set; }
        public double AnimationEndZoom { get; set; }
        public bool AnimationCleanDirectory { get; set; }

        public MandelbrotParameters Clone()
        {
            return new()
            {
                ResolutionX = ResolutionX,
                ResolutionY = ResolutionY,
                Iterations = Iterations,
                Top = Top,
                Bottom = Bottom,
                Left = Left,
                Right = Right,
                Directory = Directory,
                Filename = Filename,
                Method = Method,
                MatrixDimensionSize = MatrixDimensionSize,
                AnimationFps = AnimationFps,
                AnimationDuration = AnimationDuration,
                AnimationEndX = AnimationEndX,
                AnimationEndY = AnimationEndY,
                AnimationEndZoom = AnimationEndZoom,
                AnimationCleanDirectory = AnimationCleanDirectory
            };
        }
    }
}
