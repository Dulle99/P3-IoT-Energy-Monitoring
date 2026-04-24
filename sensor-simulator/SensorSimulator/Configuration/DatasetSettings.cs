using System;
using System.Collections.Generic;
using System.Text;

namespace SensorSimulator.Configuration
{
    public class DatasetSettings
    {
        public string FilePath { get; set; } = string.Empty;
        public int MaxReadings { get; set; } = 50;
        public int DelayMs { get; set; } = 5000;
    }
}
