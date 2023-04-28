using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mandelbaker
{
    /// <summary>
    /// Interaction logic for DeviceSelector.xaml
    /// </summary>
    public partial class DeviceSelector : Window
    {
        private DeviceSelectorViewModel _vm;
        public string SelectedDeviceName => _vm.SelectedDeviceName;
        public DeviceSelector(List<string> deviceNames)
        {
            _vm = new DeviceSelectorViewModel(deviceNames);
            DataContext = _vm;
            InitializeComponent();
        }

        private void DetermineDevice(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    public class DeviceSelectorViewModel
    {
        public List<string> DeviceNames { get; set; }
        public string SelectedDeviceName { get; set; }

        public DeviceSelectorViewModel(List<string> deviceNames)
        {
            DeviceNames = deviceNames;
            SelectedDeviceName = deviceNames[0];
        }
    }
}
