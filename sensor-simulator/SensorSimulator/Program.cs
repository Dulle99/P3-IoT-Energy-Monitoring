using System.Globalization;
using Microsoft.Extensions.Configuration;
using SensorSimulator.Configuration;
using SensorSimulator.Models;
using SensorSimulator.Utilities;


var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables()
    .Build();

var datasetSettings = configuration.GetSection("Dataset").Get<DatasetSettings>() ?? new DatasetSettings();
var edgeXSettings = configuration.GetSection("EdgeX").Get<EdgeXSettings>() ?? new EdgeXSettings();

var filePath = datasetSettings.FilePath;
var maxReadings = datasetSettings.MaxReadings;
var delayMs = datasetSettings.DelayMs;
var baseUrl = $"{edgeXSettings.BaseUrl.TrimEnd('/')}/{edgeXSettings.DeviceName}";
Console.WriteLine(baseUrl);

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

foreach(var reading in readings.Take(maxReadings))
{
    Console.WriteLine("Sending reading to EDGEX: " + reading.Timestamp + " - " + reading.GlobalActivePower);

    try
    {
        await HttpUtility.SendingReadingsToEdgeXAsync(baseUrl,httpClient, reading);

        Console.WriteLine("Successfully sent reading to EDGEX.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error sending reading to EDGEX: {ex.Message}");
    }

    Console.WriteLine();
    await Task.Delay(delayMs); // Simulate delay between readings
}

Console.WriteLine("Finished sending readings. Press any key to exit...");
Console.ReadKey();




