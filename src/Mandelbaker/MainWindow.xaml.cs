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
            MandelbrotCalculationInformation mci;

            if (_viewModel.Method == "CPU")
            {
                mci = Mandelbrot.SaveCPUMandelbrot(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Filename);
            }
            else
            {
                mci = Mandelbrot.SaveGPUMandelbrot(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Filename);
            }
            _viewModel.Output = "Render complete: " + mci.ToString();
        }
        private void RenderMatrix(object sender, RoutedEventArgs e)
        {
            MandelbrotCalculationInformation mci;
            List<MandelbrotCalculationInformation> mcis;

            (mci, mcis) = Mandelbrot.RenderMatrix(_viewModel.ResolutionX, _viewModel.ResolutionY, _viewModel.Iterations, _viewModel.DimensionSize, _viewModel.Top, _viewModel.Bottom, _viewModel.Left, _viewModel.Right, _viewModel.Directory, _viewModel.Method == "GPU" ? true : false);
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
