using Microsoft.AspNetCore.Mvc.Formatters;
using MonitoringService.Dtos;
using MonitoringService.Models;

namespace MonitoringService.Services
{
    public class MonitoringRuleEngine
    {
        private readonly ILogger<MonitoringRuleEngine> _logger;

        private const double PowerTreshold = 4.5;
        private const int RequiredConsecutiveReadings = 3;
        private int _consecutiveHighPowerReadings = 0;

        public MonitoringRuleEngine(ILogger<MonitoringRuleEngine> logger)
        {
            _logger = logger;
        }

        public MonitoringCommand? Evaluate (PowerConsumptionMessage message)
        {
            if (message.GlobalActivePower > PowerTreshold)
            {
                _consecutiveHighPowerReadings++;

                _logger.LogWarning(
                    "Prekoracen prag. DeviceId: {DeviceId}, GlobalActivePower: {GlobalActivePower}, Uzastopna prekoračenja: {Count}",
                    message.DeviceId,
                    message.GlobalActivePower,
                    _consecutiveHighPowerReadings);
            }
            else
            {
                if(_consecutiveHighPowerReadings > 0)
                {
                    _logger.LogInformation("Value returned below threshold. Reset counter for device {DeviceId}.",
                        message.DeviceId);
                }
                _consecutiveHighPowerReadings = 0;
                return null;
            }

            if(_consecutiveHighPowerReadings > RequiredConsecutiveReadings)
            {
                _consecutiveHighPowerReadings = 0;

                var command = new MonitoringCommand
                {
                    DeviceId = message.DeviceId,
                    CommandName = "TURN OFF HIGH LOAD DEVICE",
                    TriggeredAt = DateTime.UtcNow,
                    Reason = $"GlobalActivePower was greater than {PowerTreshold} during {RequiredConsecutiveReadings} consecutive readings."
                };

                _logger.LogError("ALARM: Generated command {CommandName} for device {DeviceId}. Reason: {Reason}",
                    command.CommandName,
                    command.DeviceId,
                    command.Reason);

                return command;
            }
            return null;
        }

    }
}
