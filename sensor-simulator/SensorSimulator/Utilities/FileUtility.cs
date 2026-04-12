using SensorSimulator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SensorSimulator.Utilities
{
    public static class FileUtility
    {
        public static List<PowerConsumptionReading> LoadReadings(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1)
            {
                return new List<PowerConsumptionReading>();
            }

            var readings = new List<PowerConsumptionReading>();

            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(';');

                if (parts.Length < 9)
                    continue;

                if (parts[2] == "?" || parts[4] == "?" || parts[5] == "?")
                    continue;

                try
                {
                    var timestamp = DateTime.ParseExact(
                        $"{parts[0]} {parts[1]}",
                        "d/M/yyyy H:mm:ss",
                        CultureInfo.InvariantCulture);

                    var reading = new PowerConsumptionReading
                    {
                        Timestamp = timestamp,
                        GlobalActivePower = double.Parse(parts[2], CultureInfo.InvariantCulture),
                        Voltage = double.Parse(parts[4], CultureInfo.InvariantCulture),
                        GlobalIntensity = double.Parse(parts[5], CultureInfo.InvariantCulture)
                    };

                    readings.Add(reading);
                }
                catch
                {
                    Console.WriteLine($"Error parsing line {i + 1}: {line}");
                }
            }
            return readings;
        }
    }
}
