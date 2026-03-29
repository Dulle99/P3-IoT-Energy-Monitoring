namespace MonitoringService.Models
{
    public class MonitoringCommand
    {
        public string DeviceId { get; set; } = string.Empty;
        public string CommandName { get; set; } = string.Empty;
        public DateTime TriggeredAt { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
