using System;
using System.Collections.Generic;
using System.Text;

namespace SensorSimulator.DTOs
{
    public class PowerConsumptionMessage
    {
        public string DeviceId { get; set; } = "smart-meter-1";
        public DateTime Timestamp { get; set; }
        public double GlobalActivePower { get; set; }
        public double Voltage { get; set; }
        public double GlobalIntensity { get; set; }
    }
}
