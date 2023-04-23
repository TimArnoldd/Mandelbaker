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
        public string Method { get; set; } = "CPU";
        public List<string> Methods { get; set; }
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
        public int FPS
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
        private int _duration = 10;
        public int Duration
        {
            get => _duration;
            set
            {
                if (value < 1)
                    _duration = 1;
                else if (value > 10 * 3600)
                    _duration = 10 * 3600;
                else
                    _duration = value;
            }
        }
        public double EndXLeft { get; set; } = -1.187729;
        public double EndYTop { get; set; } = 0.242367;
        public double EndZoom { get; set; } = 300000;

        #endregion


        public MandelbrotGeneratorViewModel()
        {
            Methods = new()
            {
                "CPU",
                "GPU"
            };
        }


        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
