using System;

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

        public int ResolutionX { get; set; }
        public int ResolutionY { get; set; }
        public string Method { get; set; } = string.Empty;


        public MandelbrotCalculationInformation(int resolutionX, int resolutionY, string method)
        {
            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            Method = method;
        }

        public override string ToString()
        {
            return $"{ResolutionX}x{ResolutionY} with {Method}: Total = {FullTime}s, Calculation = {CalculationTime}s, Printing = {PrintingTime}s";
        }
    }
}
