namespace MonitoringService.Models
{
    public class MonitoringRule
    {
        public bool Enabled { get; set; } = true;

        public string DeviceName { get; set; } = "smart-meter-1";

        public string ResourceName { get; set; } = "globalActivePower";

        public double Threshold { get; set; } = 2;

        public int RequiredConsecutiveReadings { get; set; } = 3;

        public string CommandName { get; set; } = "TURN_OFF_HIGH_LOAD_DEVICE";
    }
}
