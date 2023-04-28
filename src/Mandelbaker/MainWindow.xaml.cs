using Mandelbaker.Enums;
using Mandelbaker.Models;
using Mandelbaker.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;

namespace Mandelbaker
{
    public partial class MainWindow : Window
    {
        MandelbrotGeneratorViewModel _viewModel;
        public MainWindow()
        {
            _viewModel = new();
            DataContext = _viewModel;
            InitializeComponent();
        }

        private void RenderMandelbrot(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.ResolutionX > 65535 ||
                    _viewModel.ResolutionY > 65535 ||
                    checked(_viewModel.ResolutionX * _viewModel.ResolutionY) > 715776516)
                {
                    throw new Exception();
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Due to limitations in .NET 7.0 the following criteria must be met:\n- A maximum pixel dimension of 65535\n- A maximum amount of pixels of 715'776'516 (equal to 26'754 squared or 35'664x20'061 in for 16:9 images)", "Image too large", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            MandelbrotCalculationInformation mci;

            if (_viewModel.Method == CalculationMethod.GPUFloat)
            {
                mci = Mandelbrot.SaveFloatGPUMandelbrot(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Filename);
            }
            else if (_viewModel.Method == CalculationMethod.GPUDouble)
            {
                mci = Mandelbrot.SaveDoubleGPUMandelbrot(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Filename);
            }
            else
            {
                mci = Mandelbrot.SaveCPUMandelbrot(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Filename);
            }
            _viewModel.Output = "Render complete: " + mci.ToString();
        }
        private void RenderMatrix(object sender, RoutedEventArgs e)
        {
            MandelbrotCalculationInformation mci;
            List<MandelbrotCalculationInformation> mcis;

            (mci, mcis) = Mandelbrot.RenderMatrix(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.DimensionSize, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Method);
            _viewModel.Output = "Render complete: " + mci.ToString();

            string jsonString = JsonSerializer.Serialize((mci, mcis), new JsonSerializerOptions() { WriteIndented = true });

            Directory.CreateDirectory(@"C:\Mandelbaker\CalculationInformation\");
            DateTime now = DateTime.Now;
            string date = now.ToString("dd-MM-yyyy_HH-mm-ss");
            string jsonFilename = @$"C:\Mandelbaker\CalculationInformation\Matrix_{_viewModel.ResolutionX}x{_viewModel.ResolutionY}_{date}.json";
            File.WriteAllText(jsonFilename, jsonString);
        }
        //private void RenderAnimation(object sender, RoutedEventArgs e)
        //{
        //    MandelbrotCalculationInformation mci;
        //    List<MandelbrotCalculationInformation> mcis;

        //    (mci, mcis) = Mandelbrot.RenderAnimation(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.XLeft, _viewModel.YTop, _viewModel.Zoom, _viewModel.Directory, _viewModel.FPS, _viewModel.Duration, _viewModel.EndZoom, _viewModel.Method == "GPU" ? true : false);
        //    _viewModel.Output = "Render complete: " + mci.ToString();

        //    string jsonString = JsonSerializer.Serialize((mci, mcis), new JsonSerializerOptions() { WriteIndented = true });

        //    Directory.CreateDirectory(@"C:\Mandelbaker\CalculationInformation\");
        //    DateTime now = DateTime.Now;
        //    string date = now.ToString("dd-MM-yyyy_HH-mm-ss");
        //    string jsonFilename = @$"C:\Mandelbaker\CalculationInformation\Animation{_viewModel.ResolutionX}x{_viewModel.ResolutionY}_{_viewModel.FPS}fps_{_viewModel.Duration}s_{date}.json";
        //    File.WriteAllText(jsonFilename, jsonString);
        //}
        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    _viewModel.Directory = dialog.SelectedPath;
                }
            }
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_viewModel.Directory))
                Process.Start("explorer.exe", _viewModel.Directory);
        }
    }
}
