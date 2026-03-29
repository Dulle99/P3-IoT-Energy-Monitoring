using MonitoringService.Dtos;
using MQTTnet;
using System.Text;
using System.Text.Json;

namespace MonitoringService.Services
{
    public class MqttMonitoringSubscribeService : BackgroundService
    {
        private readonly ILogger<MqttMonitoringSubscribeService> _logger;
        private readonly MonitoringRuleEngine _ruleEngine;
        private IMqttClient? _mqttClient;

        public MqttMonitoringSubscribeService(ILogger<MqttMonitoringSubscribeService> logger, MonitoringRuleEngine ruleEngine)
        {
            _logger = logger;
            _ruleEngine = ruleEngine;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    _logger.LogInformation("MQTT message is recived: {payload}", payload);

                    var message = JsonSerializer.Deserialize<PowerConsumptionMessage>(
                       payload,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       });

                    if (message is null)
                    {
                        _logger.LogWarning("Failed to deserialize MQTT message: {payload}", payload);
                        return Task.CompletedTask;
                    }

                    var command = _ruleEngine.Evaluate(message);

                    if(command is not null)
                    {
                        _logger.LogInformation("Monitoring command is triggered: {commandName} for device {deviceId} because {reason}",
                            command.CommandName, command.DeviceId, command.Reason);
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing MQTT message");  
                }

                return Task.CompletedTask;
            };

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .Build();

            try
            {
                await _mqttClient.ConnectAsync(options, stoppingToken);
                _logger.LogInformation("MonitoringService povezan na MQTT broker.");

                await _mqttClient.SubscribeAsync("/iot/energy/readings", cancellationToken: stoppingToken);
                _logger.LogInformation("MonitoringService subscribed na topic: /iot/energy/readings");
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
    }
}
