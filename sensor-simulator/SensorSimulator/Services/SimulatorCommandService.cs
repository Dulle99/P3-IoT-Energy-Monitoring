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

        public object HandleLoadShedCommand(string value)
        {
            if (!bool.TryParse(value, out var enabled))
            {
                throw new ArgumentException("Invalid value for LoadShedSwitch. Expected 'true' or 'false'.");
            }

            simulatorState.LoadShedEnabled = enabled;

            logger.LogInformation(
                "Load shed command received. LoadShedEnabled set to {Enabled}",
                enabled);

            return new
            {
                status = "accepted",
                loadShedEnabled = enabled
            };
        }
    }
}
