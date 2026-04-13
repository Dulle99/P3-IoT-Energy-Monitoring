using MonitoringService.Configuration;
using MonitoringService.Dtos;
using MQTTnet;
using System.Text;
using System.Text.Json;

namespace MonitoringService.Services
{
    public class MqttMonitoringSubscribeService : BackgroundService
    {
        private readonly ILogger<MqttMonitoringSubscribeService> _logger;
        private readonly MqttSettings _mqttSettings;

        private const string TargetDeviceName = "smart-meter-1";

        private readonly MonitoringRuleEngine _ruleEngine;
        private IMqttClient? _mqttClient;

        public MqttMonitoringSubscribeService(ILogger<MqttMonitoringSubscribeService> logger, MonitoringRuleEngine ruleEngine, IConfiguration config)
        {
            _logger = logger;
            _ruleEngine = ruleEngine;
            _mqttSettings = config.GetSection("Mqtt").Get<MqttSettings>() ?? new MqttSettings();
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    var rawPayloadMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                    _logger.LogInformation("Received MQTT message on topic {topic}: {message}", 
                        e.ApplicationMessage.Topic,
                        rawPayloadMessage);

                    var edgeXEvent = TryExtractEdgeXEvent(rawPayloadMessage);

                    if(edgeXEvent is null)
                    {
                        _logger.LogWarning("Failed to extract EdgeXEvent from MQTT message: {rawMessage}",
                            rawPayloadMessage);
                        return Task.CompletedTask;
                    }

                    if(!string.Equals(edgeXEvent.DeviceName, TargetDeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("[IGNORING] - Received event from device {deviceName}, which does not match target device {targetDevice}.",
                            edgeXEvent.DeviceName, 
                            TargetDeviceName);
                        return Task.CompletedTask;
                    }

                    _logger.LogInformation("[PARSED] - EdgeXEvent from device {deviceName} with {readingCount} readings.", edgeXEvent.DeviceName, edgeXEvent.Readings.Count);

                    foreach(var reading in edgeXEvent.Readings)
                    {
                        _logger.LogInformation("[READING] - Evaluating reading {resourceName} with value {value} from device {deviceName}.",
                            reading.ResourceName,
                            reading.Value,
                            reading.DeviceName);

                        var command = _ruleEngine.Evaluate(reading);

                        if(command is not null)
                        {
                            _logger.LogCritical("ALARM TRIGGERED - {CommandName} for device {DeviceId}. Reason: {Reason}",
                                command.CommandName, 
                                command.DeviceId, 
                                command.Reason);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message");
                }

                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_mqttSettings.Host, _mqttSettings.Port)
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options, stoppingToken);
                _logger.LogInformation("MonitoringService connected to MQTT broker. {host}:{port}", _mqttSettings.Host, _mqttSettings.Port);

                await _mqttClient.SubscribeAsync(_mqttSettings.Topic, cancellationToken: stoppingToken);
                _logger.LogInformation("MonitoringService subscribed to {topic}", _mqttSettings.Topic);
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_mqttClient is not null && _mqttClient.IsConnected)
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("Monitoring MQTT client diskonektovan.");
            }

            await base.StopAsync(cancellationToken);
        }

        private EdgeXEvent? TryExtractEdgeXEvent(string rawMessage)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            using var doc = JsonDocument.Parse(rawMessage);
            var root = doc.RootElement;

            if(!root.TryGetProperty("payload", out var payloadElement))
            {
                _logger.LogWarning("MQTT message does not contain 'payload' property: {rawMessage}", rawMessage);
                return null;
            }

            // If payload is a JSON object, we can directly try to deserialize it as EdgeXEvent
            if (payloadElement.ValueKind == JsonValueKind.Object)
            {
                if(payloadElement.TryGetProperty("event", out var eventElement))
                {
                    return JsonSerializer.Deserialize<EdgeXEvent>(eventElement.GetRawText(), options);
                }

                return JsonSerializer.Deserialize<EdgeXEvent>(payloadElement.GetRawText(), options);
            }

            // If payload is a string, we assume it's a base64-encoded JSON string
            if (payloadElement.ValueKind == JsonValueKind.String)
            {
                var base64String = payloadElement.GetString();

                if (string.IsNullOrEmpty(base64String))
                {
                    _logger.LogWarning("Payload is an empty string in MQTT message: {rawMessage}", rawMessage);
                    return null;
                }

                var decodedBytes = Convert.FromBase64String(base64String);
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
            _logger.LogWarning("Payload in MQTT message is neither an object nor a string: {rawMessage}", rawMessage);
            return null;
        }
    }
}
