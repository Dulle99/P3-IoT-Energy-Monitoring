namespace MonitoringService.Configuration
{
    public class MqttSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        public string Topic { get; set; } = "edgex/events/#";
    }
}
