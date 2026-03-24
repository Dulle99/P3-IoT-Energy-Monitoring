using System.Globalization;
using System.Text.Json;
using MQTTnet;
using SensorSimulator.DTOs;
using SensorSimulator.Models;

var filePath = @"C:\Projects\P3 IoT Energy Monitoring\dataset\household_power_consumption_sample.txt";
var brokerHost = "localhost";
var brokerPort = 1883;
var topic = "/iot/energy/readings";

Console.WriteLine("File path: " + filePath);

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

#region Initial MQTT Client Setup

var factory = new MqttClientFactory();
var mqqtClient = factory.CreateMqttClient();
var options = new MqttClientOptionsBuilder()
    .WithTcpServer(brokerHost, brokerPort)
    .Build();

try
{
    await mqqtClient.ConnectAsync(options);
    Console.WriteLine($"Connected to MQTT broker at {brokerHost}:{brokerPort}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to MQTT broker: {ex.Message}");
    Console.WriteLine("End of program, press any key to exit...");
    Console.ReadKey();
    return;
}

#endregion Initial MQTT Client Setup


foreach (var r in reading.Take(10)) //za sada neka uzima 10 redova
{
    //sending message to MQTT broker
    var message = new PowerConsumptionMessage
    {
        DeviceId = "smart-meter-1",
        Timestamp = r.Timestamp,
        GlobalActivePower = r.GlobalActivePower,
        Voltage = r.Voltage,
        GlobalIntensity = r.GlobalIntensity
    };

    var payload = JsonSerializer.Serialize(message);

    var applicationMessage = new MqttApplicationMessageBuilder()
        .WithTopic(topic)
        .WithPayload(payload)
        .Build();

    await mqqtClient.PublishAsync(applicationMessage);

    Console.WriteLine($"[PUBLISHED] Topic: {topic}");
    Console.WriteLine(payload);
    Console.WriteLine();

    await Task.Delay(2000); //salje za sada na console svakih 2 sekunda, kasnije ce se slati na MQTT broker
}

await mqqtClient.DisconnectAsync();

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


