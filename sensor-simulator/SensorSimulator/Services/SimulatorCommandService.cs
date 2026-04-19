using SensorSimulator.Models;

namespace SensorSimulator.Services
{
    public class SimulatorCommandService
    {
        private readonly SimulatorState simulatorState;
        private readonly ILogger<SimulatorCommandService> logger;

        public SimulatorCommandService(SimulatorState simulatorState, ILogger<SimulatorCommandService> logger)
        {
            this.simulatorState = simulatorState;
            this.logger = logger;
        }   

        public object HandleLoadShedCommand(LoadShedCommandRequest request)
        {
            var enabled = request.LoadShedSwitch.Equals("true", StringComparison.OrdinalIgnoreCase);
            simulatorState.LoadShedEnable = enabled;

            logger.LogInformation("Load shed command received. LoadShedEnable set to {Enabled}", enabled);

            return new
            {
                status = "accepted",
                loadShedEnabled = enabled
            };
        }
    }
}
