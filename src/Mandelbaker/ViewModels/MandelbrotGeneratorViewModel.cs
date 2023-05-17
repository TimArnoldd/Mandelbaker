using Mandelbaker.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mandelbaker.ViewModels
{
    public class MandelbrotGeneratorViewModel : INotifyPropertyChanged
    {
        #region Generic Properties
        public int ResolutionX { get; set; } = 1000;
        public int ResolutionY { get; set; } = 1000;
        public int Iterations { get; set; } = 255;
        public double Top { get; set; } = 1.5;
        public double Bottom { get; set; } = -1.5;
        public double Left { get; set; } = -2;
        public double Right { get; set; } = 1;
        private string _directory = @"C:\Mandelbaker\Output\";
        public string Directory
        {
            get => _directory;
            set
            {
                _directory = value;
                if (value.Last() != '/' && value.Last() != '\\')
                    _directory += @"\";
                NotifyPropertyChanged();
            }
        }
        public CalculationMethod Method { get; set; }
        public string MethodString
        {
            get => Method.ToString();
            set => Method = (CalculationMethod)Enum.Parse(typeof(CalculationMethod), value);
        }
        public List<string> Methods { get; set; } = new();
        private string _output = string.Empty;
        public string Output
        {
            get => _output;
            set
            {
                _output = value;
                NotifyPropertyChanged();
            }
        }

        #endregion

        #region Single Image Properties

        private string? _filename;
        public string? Filename
        {
            get => _filename;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    _filename = null;
                    return;
                }

                if (value.EndsWith(".png"))
                {
                    if (value.Length > 4)
                    {
                        _filename = value;
                    }
                }
                else
                {
                    _filename = value + ".png";
                }
            }
        }

        #endregion

        #region Matrix Properties

        private int _dimensionSize = 5;
        public int DimensionSize
        {
            get => _dimensionSize;
            set
            {
                if (value >= 1)
                {
                    _dimensionSize = value;
                }
            }
        }

        #endregion

        #region Animation Properties

        private int _fps = 30;
        public int Fps
        {
            get => _fps;
            set
            {
                if (value < 1)
                    _fps = 1;
                else if (value > 120)
                    _fps = 120;
                else
                    _fps = value;
            }
        }
        private int _videoDuration = 10;
        public int VideoDuration
        {
            get => _videoDuration;
            set
            {
                if (value < 1)
                    _videoDuration = 1;
                else if (value > 10 * 3600)
                    _videoDuration = 10 * 3600;
                else
                    _videoDuration = value;
            }
        }
        public double EndX { get; set; } = 0.36024044343761435;
        public double EndY { get; set; } = -0.6413130610648031;
        public double EndZoom { get; set; } = 3000000000000000;
        public bool CleanAnimationDirectory { get; set; } = true;

        #endregion


        public MandelbrotGeneratorViewModel()
        {
            foreach (var method in Enum.GetValues(typeof(CalculationMethod)))
            {
                Methods.Add(method.ToString());
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
