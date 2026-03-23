using System.Globalization;
using SensorSimulator.Models;

var filePath = @"C:\Projects\P3 IoT Energy Monitoring\dataset\household_power_consumption_sample.txt";

Console.WriteLine(filePath);

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    Console.WriteLine("End of program, press any key to exit...");
    Console.ReadKey();
    return;
}

var reading = LoadReadings(filePath);

Console.WriteLine($"Readings loaded: {reading.Count}");
Console.WriteLine();

foreach (var r in reading.Take(10)) //za sada neka uzima 10 redova
{
    Console.WriteLine(
        $"[SENT] Time: {r.Timestamp:yyyy-MM-dd HH:mm:ss}, " +
        $"GlobalActivePower: {r.GlobalActivePower}, " +
        $"Voltage: {r.Voltage}, " +
        $"GlobalIntensity: {r.GlobalIntensity}");

    await Task.Delay(2000); //salje za sad na console svakih 2 sekunda, kasnije ce se slati na MQTT broker
}

Console.WriteLine();
Console.WriteLine("End of program, press any key to exit...");
Console.ReadKey();

static List<PowerConsumptionReading> LoadReadings(string filePath)
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


