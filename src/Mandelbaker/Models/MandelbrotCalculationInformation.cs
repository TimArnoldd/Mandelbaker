using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mandelbaker.Models
{
    public class MandelbrotCalculationInformation
    {
        public DateTime StartDateTime { get; set; }
        public DateTime CalculationDoneDateTime { get; set; }
        public DateTime EndDateTime { get; set; }

        public double CalculationTime => (CalculationDoneDateTime - StartDateTime).TotalSeconds;
        public double FullTime => (EndDateTime - StartDateTime).TotalSeconds;
        public double PrintingTime => (EndDateTime - CalculationDoneDateTime).TotalSeconds;

        public int Resolution { get; set; }
        public string Method { get; set; } = string.Empty;


        public MandelbrotCalculationInformation()
        {

        }
        public MandelbrotCalculationInformation(int resolution, string method)
        {
            Resolution = resolution;
            Method = method;
        }

        public override string ToString()
        {
            return $"{Resolution}px with {Method}: Total = {FullTime}s, Calculation = {CalculationTime}s, Printing = {PrintingTime}s";
        }
    }
}
