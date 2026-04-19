using SensorSimulator.Models;
using SensorSimulator.Services;

namespace SensorSimulator.Utilities
{
    public static class SimulatorEndpointsExtensions
    {
        public static WebApplication MapSimulatorEndpoints(this WebApplication app)
        {
            app.MapPut("/api/LoadShedSwitch", (LoadShedCommandRequest request, SimulatorCommandService commandService) =>
            {
                if (string.IsNullOrEmpty(request.LoadShedSwitch))
                {
                    return Results.BadRequest("LoadShedSwatch is required.");
                }

                var result = commandService.HandleLoadShedCommand(request);
                return Results.Ok(result);
            });

            return app;
        }
    }
}
