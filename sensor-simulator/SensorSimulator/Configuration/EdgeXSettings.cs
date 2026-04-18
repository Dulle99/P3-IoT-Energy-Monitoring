using System;
using System.Collections.Generic;
using System.Text;

namespace SensorSimulator.Configuration
{
    public class EdgeXSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:59986/api/v3/resource";
        public string DeviceName { get; set; } = "smart-meter-1";
    }
}
