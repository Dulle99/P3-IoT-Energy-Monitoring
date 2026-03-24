namespace VisualizationService.Configuration
{
    public class InfluxDbSettings
    {
        public string Url { get; set; } = String.Empty;
        public string Token { get; set; } = String.Empty;   
        public string Org { get; set; } = String.Empty;

        public string Bucket { get; set; } = String.Empty;
    }
}
