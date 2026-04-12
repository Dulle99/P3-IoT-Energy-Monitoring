using System.Globalization;
using SensorSimulator.Models;
using SensorSimulator.Utilities;

var filePath = @"C:\Projects\P3 IoT Energy Monitoring\dataset\household_power_consumption_sample.txt";

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    Console.WriteLine("End of program, press any key to exit...");
    Console.ReadKey();
    return;
}

var readings = FileUtility.LoadReadings(filePath);
Console.WriteLine("Readings loaded: " + readings.Count);
Console.WriteLine();

using var httpClient = new HttpClient();    

foreach(var reading in readings.Take(20))
{
    Console.WriteLine("Sending reading to EDGEX: " + reading.Timestamp + " - " + reading.GlobalActivePower);

    try
    {
        await HttpUtility.SendingReadingsToEdgeXAsync(httpClient, reading);

        Console.WriteLine("Successfully sent reading to EDGEX.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending reading to EDGEX: {ex.Message}");
    }

    Console.WriteLine();
    await Task.Delay(1000); // Simulate delay between readings
}

Console.WriteLine("Finished sending readings. Press any key to exit...");
Console.ReadKey();




