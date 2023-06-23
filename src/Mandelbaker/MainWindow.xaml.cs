using Mandelbaker.BL;
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
        private readonly MandelbrotGeneratorViewModel _viewModel;
        private readonly Mandelbrot _mandelbrot;
        public MainWindow()
        {
            _viewModel = new();
            DataContext = _viewModel;
            InitializeComponent();
            _mandelbrot = new(_viewModel.ToMandelbrotParameters());
        }

        private void RenderMandelbrot(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_viewModel.ResolutionX > 65535 ||
                    _viewModel.ResolutionY > 65535 ||
                    checked(_viewModel.ResolutionX * _viewModel.ResolutionY) > 715776516) throw new Exception();
            }
            catch
            {
                System.Windows.MessageBox.Show(
                    "Due to limitations in .NET 6.0 the following criteria must be met:\n" +
                    "- A maximum pixel dimension of 65535\n" +
                    "- A maximum amount of pixels of 715'776'516 (equal to 26'754 squared or 35'664x20'061 in for 16:9 images)",
                    "Image too large",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            MandelbrotCalculationInformation mci;

            try
            {
                _mandelbrot.Parameters = _viewModel.ToMandelbrotParameters();
                mci = _mandelbrot.SaveMandelbrot();
                _viewModel.Output = "Render complete: " + mci.ToString();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RenderMatrix(object sender, RoutedEventArgs e)
        {
            MandelbrotCalculationInformation mci;
            List<MandelbrotCalculationInformation> mcis;

            try
            {
                _mandelbrot.Parameters = _viewModel.ToMandelbrotParameters();
                (mci, mcis) = _mandelbrot.RenderMatrix();
                _viewModel.Output = "Render complete: " + mci.ToString();

                string jsonString = JsonSerializer.Serialize(mcis, new JsonSerializerOptions() { WriteIndented = true });

                Directory.CreateDirectory(@"C:\Mandelbaker\CalculationInformation\");
                DateTime now = DateTime.Now;
                string date = now.ToString("dd-MM-yyyy_HH-mm-ss");
                string jsonFilename = @$"C:\Mandelbaker\CalculationInformation\Matrix_{_viewModel.ResolutionX}x{_viewModel.ResolutionY}_{date}.json";
                File.WriteAllText(jsonFilename, jsonString);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void RenderAnimation(object sender, RoutedEventArgs e)
        {
            MandelbrotCalculationInformation mci;
            List<MandelbrotCalculationInformation> mcis;

            try
            {
                _mandelbrot.Parameters = _viewModel.ToMandelbrotParameters();
                (mci, mcis) = _mandelbrot.RenderAnimation();
                _viewModel.Output = "Render complete: " + mci.ToString();

                string jsonString = JsonSerializer.Serialize(mcis, new JsonSerializerOptions() { WriteIndented = true });

                Directory.CreateDirectory(@"C:\Mandelbaker\CalculationInformation\");
                DateTime now = DateTime.Now;
                string date = now.ToString("dd-MM-yyyy_HH-mm-ss");
                string jsonFilename = @$"C:\Mandelbaker\CalculationInformation\Animation{_viewModel.ResolutionX}x{_viewModel.ResolutionY}_{_viewModel.AnimationFps}fps_{_viewModel.AnimationDuration}s_{date}.json";
                File.WriteAllText(jsonFilename, jsonString);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message, "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                _viewModel.Directory = dialog.SelectedPath;
            }
        }
        private void OpenFolder(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(_viewModel.Directory))
                Process.Start("explorer.exe", _viewModel.Directory);
        }
    }
}
