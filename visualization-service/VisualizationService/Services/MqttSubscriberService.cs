using MQTTnet;
using System.Text;
using System.Text.Json;
using VisualizationService.DTOs;

namespace VisualizationService.Services
{
    public class MqttSubscriberService : BackgroundService
    {
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
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    _logger.LogInformation($"[RECEIVED] Topic: {e.ApplicationMessage.Topic}");

                    var message = JsonSerializer.Deserialize<PowerConsumptionMessage>(payload,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (message != null)
                    {
                        _logger.LogInformation($"[PARSED] Device: {message.DeviceId}, " +
                            $"Power: {message.GlobalActivePower}W, " +
                            $"Voltage: {message.Voltage}V, " +
                            $"Intensity: {message.GlobalIntensity}A");

                        await _influxDbWriterService.WriteAsync(message);
                    }
                    else
                    {
                        _logger.LogWarning("[PARSE FAILED] Could not parse MQTT message payload.");
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

                await _mqttClient.SubscribeAsync("/iot/energy/readings", cancellationToken: stoppingToken);
                _logger.LogInformation("Subscribed to topic: /iot/energy/readings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker");
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
    }
}
