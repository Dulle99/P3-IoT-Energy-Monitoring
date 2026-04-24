namespace MonitoringService.Dtos
{
    public class UpdateMonitoringRuleRequest
    {
        public bool? Enabled { get; set; }

        public string? DeviceName { get; set; }

        public string? ResourceName { get; set; }

        public double? Threshold { get; set; }

        public int? RequiredConsecutiveReadings { get; set; }

        public string? CommandName { get; set; }
    }
}
