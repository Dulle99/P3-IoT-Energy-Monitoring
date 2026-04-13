namespace MonitoringService.Dtos
{
    public class EdgeXEventWrapper
    {
        public string ApiVersion { get; set; } = string.Empty;
        public string RequestId { get; set; } = string.Empty;
        public EdgeXEvent? Event { get; set; }
    }
}
