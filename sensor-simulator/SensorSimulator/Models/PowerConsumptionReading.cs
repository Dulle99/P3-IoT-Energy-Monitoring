using System;
using System.Collections.Generic;
using System.Text;

namespace SensorSimulator.Models
{
    public class PowerConsumptionReading
    {
        public DateTime Timestamp { get; set; }
        public double GlobalActivePower { get; set; }
        public double Voltage { get; set; }
        public double GlobalIntensity { get; set; }
    }
}
