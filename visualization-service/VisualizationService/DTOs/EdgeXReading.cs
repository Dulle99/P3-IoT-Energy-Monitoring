namespace VisualizationService.Dtos
{
    public class EdgeXReading
    {
        public string Id { get; set; } = string.Empty;
        public long Origin { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public string ProfileName { get; set; } = string.Empty;
        public string ValueType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
