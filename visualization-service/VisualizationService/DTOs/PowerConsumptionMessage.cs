namespace VisualizationService.DTOs
{
    public class PowerConsumptionMessage
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public double GlobalActivePower { get; set; }
        public double Voltage { get; set; }
        public double GlobalIntensity { get; set; }
    }
}
