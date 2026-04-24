using Microsoft.Extensions.Options;
using MonitoringService.Configuration;
using System.Runtime;

namespace MonitoringService.Services
{
    public class EdgeXCommandService
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<EdgeXCommandService> logger;
        private readonly CoreCommandSettings commandSettings;

        public EdgeXCommandService(HttpClient httpClient, ILogger<EdgeXCommandService> logger, IConfiguration configuration)
        {
            this.httpClient = httpClient;
            this.logger = logger;
            this.commandSettings = configuration.GetSection("CoreCommand").Get<CoreCommandSettings>() ?? new CoreCommandSettings();
        }
        public async Task SendLoadShedCommandAsync(string deviceName, CancellationToken cancellationToken = default)
        {
            await SendLoadShedCommandAsync(deviceName, true, cancellationToken);
        }
        public async Task SendLoadShedCommandAsync(string deviceName, bool enable, CancellationToken cancellationToken = default)
        {
            var url = $"{commandSettings.BaseUrl.TrimEnd('/')}/api/v3/device/name/{deviceName}/{commandSettings.CommandName}";

            var body = new Dictionary<string, string>
            {
                [commandSettings.ResourceName] = enable.ToString().ToLowerInvariant()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(body);
            using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync(url, content, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to send command to EdgeX. Status: {StatusCode}, Response: {ResponseBody}", response.StatusCode, responseBody);
                return;
            }
            else
            {
                logger.LogInformation("Successfully sent command to EdgeX - Device: {DeviceName}. Response: {ResponseBody}", deviceName, responseBody);
            }
        }
    }
}
