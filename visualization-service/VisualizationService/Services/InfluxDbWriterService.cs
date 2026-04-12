using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using System.Globalization;
using VisualizationService.Configuration;
using VisualizationService.Dtos;
using VisualizationService.DTOs;
using VisualizationService.Utilities;

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

        public async Task WriteReadingAsync(EdgeXReading reading)
        {
            try
            {
                if(!ParsingUtility.TryMapFieldName(reading.ResourceName, out var fieldName))
                    { _logger.LogWarning("Unrecognized resource name {ResourceName} for device {DeviceName}", reading.ResourceName, reading.DeviceName);
                      return;
                }
                if(!double.TryParse(reading.Value,NumberStyles.Float,CultureInfo.InvariantCulture, out var numericValue))
                {
                    _logger.LogWarning("Failed to parse value {Value} for resource {ResourceName} of device {DeviceName}", reading.Value, reading.ResourceName, reading.DeviceName);
                    return;
                }

                using var client = new InfluxDBClient(_settings.Url, _settings.Token);
                var writeApi = client.GetWriteApiAsync();

                var timestamp = ParsingUtility.ConvertUnixNanosecondsToDateTime(reading.Origin);

                var point = PointData
                    .Measurement("energy_readings")
                    .Tag("deviceId", reading.DeviceName)
                    .Field(fieldName, numericValue)
                    .Timestamp(timestamp, WritePrecision.Ns);
                
                await writeApi.WritePointAsync(point, _settings.Bucket, _settings.Org);

                _logger.LogInformation("Successfully wrote reading for device {DeviceName}, resource {ResourceName} to InfluxDB", reading.DeviceName, reading.ResourceName);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write data to InfluxDB");
            }
        }


    }
}
