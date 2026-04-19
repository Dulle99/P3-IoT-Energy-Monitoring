namespace MonitoringService.Configuration
{
    public class CoreCommandSettings
    {
        public string BaseUrl { get; set; } = "http://localhost:59882";
        public string CommandName { get; set; } = "LoadShedSwitch";
        public string ResourceName { get; set; } = "loadShedSwitch";
        public string TriggerValue { get; set; } = "true";
    }
}
