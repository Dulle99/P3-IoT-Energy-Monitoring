using SensorSimulator.Models;
using SensorSimulator.Services;

namespace SensorSimulator.Utilities
{
    public static class SimulatorEndpointsExtensions
    {
        public static WebApplication MapSimulatorEndpoints(this WebApplication app)
        {
            app.MapPut("/api/LoadShedSwitch", async (HttpRequest request, SimulatorCommandService commandService) =>
            {
                Console.WriteLine($"Incoming Content-Type: {request.ContentType}");

                using var reader = new StreamReader(request.Body);
                var rawBody = await reader.ReadToEndAsync();

                if (string.IsNullOrEmpty(rawBody))
                {
                    return Results.BadRequest("Request body is empty.");
                }

                var value = rawBody.Trim('"'); // Remove potential surrounding quotes

                var result = commandService.HandleLoadShedCommand(value);
                return Results.Ok(result);
            });

            return app;
        }
    }
}
