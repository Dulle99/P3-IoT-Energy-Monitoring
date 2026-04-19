using Microsoft.Extensions.Options;
using SensorSimulator.Configuration;
using SensorSimulator.Models;
using SensorSimulator.Utilities;

namespace SensorSimulator.Services
{
    public class SensorReadingPublisherService : BackgroundService
    {
        private readonly DatasetSettings _datasetSettings;
        private readonly EdgeXSettings _edgeXSettings;
        private readonly SimulatorState _simulatorState;
        private readonly ILogger<SensorReadingPublisherService> _logger;

        public SensorReadingPublisherService(IOptions<DatasetSettings> datasetOptions, IOptions<EdgeXSettings> edgeXOptions,
            SimulatorState simulatorState, ILogger<SensorReadingPublisherService> logger)
        {
            _datasetSettings = datasetOptions.Value;
            _edgeXSettings = edgeXOptions.Value;
            _simulatorState = simulatorState;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var filePath = _datasetSettings.FilePath;
            var maxReadings = _datasetSettings.MaxReadings;
            var delayMs = _datasetSettings.DelayMs;
            var baseUrl = $"{_edgeXSettings.BaseUrl.TrimEnd('/')}/{_edgeXSettings.DeviceName}";

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return;
            }

            var readings = FileUtility.LoadReadings(filePath);
            _logger.LogInformation("Readings loaded: {Count}", readings.Count);

            using var httpClient = new HttpClient();

            foreach (var originalReading in readings.Take(maxReadings))
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                // Apply load shed if enabled in the simulator state
                var reading = ApplyLoadShedIfNeeded(originalReading);

                _logger.LogDebug(
                    "Sending reading to EdgeX: {Timestamp} - {Power}",
                    reading.Timestamp,
                    reading.GlobalActivePower);

                try
                {
                    await HttpUtility.SendingReadingsToEdgeXAsync(baseUrl, httpClient, reading);
                    _logger.LogInformation("Successfully sent reading to EdgeX.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reading to EdgeX.");
                }

                await Task.Delay(delayMs, stoppingToken);
            }

            _logger.LogInformation("Finished sending readings.");
        }

        private PowerConsumptionReading ApplyLoadShedIfNeeded(PowerConsumptionReading reading)
        {
            if (!_simulatorState.LoadShedEnabled)
                return reading;

            return  new PowerConsumptionReading
            {
                Timestamp = reading.Timestamp,
                GlobalActivePower = Math.Min(reading.GlobalActivePower, 2.5), // Limit power to 2.5 kW during load shed
                Voltage = reading.Voltage,
                GlobalIntensity = Math.Min(reading.GlobalIntensity, 10.0) // Limit intensity to 10 A during load shed
            };
        }
    }
}
