using MQTTnet;
using System.Text;
using System.Text.Json;
using VisualizationService.Dtos;
using VisualizationService.DTOs;

namespace VisualizationService.Services
{
    public class MqttSubscriberService : BackgroundService
    {
        private const string TargetDeviceName = "smart-meter-1";

        private readonly ILogger<MqttSubscriberService> _logger;
        private readonly InfluxDbWriterService _influxDbWriterService;
        private IMqttClient? _mqttClient;

        public MqttSubscriberService(ILogger<MqttSubscriberService> logger, InfluxDbWriterService influxDbWritterService) 
        { 
            _logger = logger;
            _influxDbWriterService = influxDbWritterService;

        }

        #region MQTT Client Setup, Connection, Subscription, and Message Handling
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();


            _mqttClient.ApplicationMessageReceivedAsync += async e =>
            {
                #region Logging received message
                try
                {
                    var rawPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    _logger.LogInformation($"[RECEIVED] Topic: {e.ApplicationMessage.Topic}, Payload: {rawPayload}");

                    var edgeXEvent = TryExtractEdgeXEvent(rawPayload);

                    if(edgeXEvent is null) { 
                        _logger.LogWarning("Failed to extract EdgeXEvent from MQTT message.");
                        return;
                    }

                    if(!string.Equals(edgeXEvent.DeviceName, TargetDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"[IGNORING] - Received event from device '{edgeXEvent.DeviceName}', which does not match target device '{TargetDeviceName}'.");
                        return;
                    }

                    _logger.LogInformation($"[PARSED] Device: {edgeXEvent.DeviceName}, " +
                        $"Profile: {edgeXEvent.ProfileName}, " +
                        $"Source: {edgeXEvent.SourceName}, " +
                        $"Readings Count: {edgeXEvent.Readings?.Count ?? 0}");

                    foreach(var reading in edgeXEvent.Readings)
                    {
                        _logger.LogInformation($"[READING] Resource: {reading.ResourceName}, " +
                            $"Value: {reading.Value}, " +
                            $"ValueType: {reading.ValueType}, " +
                            $"Origin: {reading.Origin}");
                        await _influxDbWriterService.WriteReadingAsync( reading);
                    }
                }
                catch (Exception ex) { _logger.LogError(ex, "Error while parsing MQTT message"); }

                return;

                #endregion Logging received message
            };

            #region MQTT Client Connection and Subscription
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options, stoppingToken);
                _logger.LogInformation("Connected to MQTT broker at localhost:1883");

                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("edgex/events/#").Build(), stoppingToken);
                _logger.LogInformation("Subscribed to topic 'edgex/events/#'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker or subscribing to topic");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
            #endregion MQTT Client Connection and Subscription
        }

        #endregion MQTT Client Setup, Connection, Subscription, and Message Handling

        #region Graceful MQTT Client Disconnection
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("Disconnected from MQTT broker");
            }
            await base.StopAsync(cancellationToken);
        }

        #endregion Dispose MQTT Client Resources

        #region ParsingEdgeXEvent

        private EdgeXEvent? TryExtractEdgeXEvent(string rawMessage)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var doc = JsonDocument.Parse(rawMessage);
            var root = doc.RootElement;


            if (!root.TryGetProperty("payload", out var payloadElement))
            {
                _logger.LogWarning("MQTT message does not contain 'payload' property.");
                return null;
            }

            // If payload is a JSON object, try to parse it directly
            if (payloadElement.ValueKind == JsonValueKind.Object)
            {
                if (payloadElement.TryGetProperty("event", out var eventElement))
                {
                    return JsonSerializer.Deserialize<EdgeXEvent>(eventElement.GetRawText(), options);
                }

                return JsonSerializer.Deserialize<EdgeXEvent>(payloadElement.GetRawText(), options);
            }

            // If payload is a string, assume it's a base64-encoded JSON
            if (payloadElement.ValueKind == JsonValueKind.String)
            {
                var base64Payload = payloadElement.GetString();

                if (string.IsNullOrWhiteSpace(base64Payload))
                {
                    return null;
                }

                var decodedBytes = Convert.FromBase64String(base64Payload);
                var decodedJson = Encoding.UTF8.GetString(decodedBytes);

                using var decodedDoc = JsonDocument.Parse(decodedJson);
                var decodedRoot = decodedDoc.RootElement;

                if (decodedRoot.TryGetProperty("event", out var eventElement))
                {
                    return JsonSerializer.Deserialize<EdgeXEvent>(
                        eventElement.GetRawText(),
                        options);
                }

                return JsonSerializer.Deserialize<EdgeXEvent>(
                    decodedRoot.GetRawText(),
                    options);
            }

            return null;
        }

        #endregion ParsingEdgeXEvent
    }
}
