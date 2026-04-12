using System.Text.Json;

namespace VisualizationService.Dtos
{
    public class EdgeXMessageEnvelope
    {
        public string ApiVersion { get; set; } = string.Empty;
        public string RecivedTopic { get; set; } = string.Empty;
        public string CorrelationId { get; set; } = string.Empty;

        public string RequestId { get; set; } = string.Empty;

        public int ErrorCode { get; set; }

        public JsonElement Payload { get; set; }

        public string ContentType { get; set; } = string.Empty;
    }
}
