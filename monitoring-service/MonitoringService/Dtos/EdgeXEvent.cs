namespace MonitoringService.Dtos
{
    public class EdgeXEvent
    {
        public string ApiVersion { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string SourceName { get; set; } = string.Empty;
        public long Origin { get; set; }
        public List<EdgeXReading> Readings { get; set; } = new();
    }
}
