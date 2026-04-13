using System.Text.Json;

namespace MonitoringService.Dtos
{
    public class EdgeXMessageEnvelope
    {
        public string ApiVersion { get; set; } = string.Empty;
        public string ReceivedTopic { get; set; } = string.Empty;
        public string CorrelationID { get; set; } = string.Empty;
        public string RequestID { get; set; } = string.Empty;
        public int ErrorCode { get; set; }
        public JsonElement Payload { get; set; }
        public string ContentType { get; set; } = string.Empty;

    }
}
