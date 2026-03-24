using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using VisualizationService.Configuration;
using VisualizationService.DTOs;

namespace VisualizationService.Services
{
    public class InfluxDbWriterService
    {
        private readonly ILogger<InfluxDbWriterService> _logger;
        private readonly InfluxDbSettings _settings;    

        public InfluxDbWriterService(ILogger<InfluxDbWriterService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _settings = configuration.GetSection("InfluxDb").Get<InfluxDbSettings>() ?? new InfluxDbSettings();
        }

        public async Task WriteAsync(PowerConsumptionMessage message)
        {
            try
            {
                using var client = new InfluxDBClient(_settings.Url, _settings.Token);
                var writeApi = client.GetWriteApiAsync();

                var point = PointData
                    .Measurement("energy_readings")
                    .Tag("deviceId", message.DeviceId)
                    .Field("globalActivePower", message.GlobalActivePower)
                    .Field("voltage", message.Voltage)
                    .Field("globalIntensity", message.GlobalIntensity)
                    .Timestamp(message.Timestamp, WritePrecision.S);
                
                await writeApi.WritePointAsync(point, _settings.Bucket, _settings.Org);

                _logger.LogInformation("Successfully wrote data to InfluxDB for device {DeviceId} at {Timestamp}", message.DeviceId, message.Timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write data to InfluxDB");
            }
        }
    }
}
