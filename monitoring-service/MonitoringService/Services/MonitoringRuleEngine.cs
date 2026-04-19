using Microsoft.AspNetCore.Mvc.Formatters;
using MonitoringService.Dtos;
using MonitoringService.Models;
using System.Globalization;

namespace MonitoringService.Services
{
    public class MonitoringRuleEngine
    {
        private readonly ILogger<MonitoringRuleEngine> _logger;

        private const string TargetDeviceName = "smart-meter-1";
        private const string TargetResourceName = "globalActivePower";
        private const double PowerTreshold = 4.5;
        private const int RequiredConsecutiveReadings = 3;

        private int _consecutiveHighPowerReadings = 0;

        public MonitoringRuleEngine(ILogger<MonitoringRuleEngine> logger)
        {
            _logger = logger;
        }

        public MonitoringCommand? Evaluate (EdgeXReading reading)
        {
            if(!string.Equals(reading.DeviceName, TargetDeviceName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if(!string.Equals(reading.ResourceName, TargetResourceName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!double.TryParse(reading.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double globalAtivePower))
            {
                _logger.LogWarning("Failed to parse globalActivePower value: {Value}", reading.Value);
                return null;
            }

            if(globalAtivePower > PowerTreshold)
            {
                _consecutiveHighPowerReadings++;
                _logger.LogInformation("[THRESHOLD EXCEEDED] - High power reading detected: {Value}. Consecutive count: {Count}", globalAtivePower, _consecutiveHighPowerReadings);
            }
            else
            {
                if(_consecutiveHighPowerReadings > 0)
                {
                    _logger.LogInformation("Power reading back to normal: {Value}. Resetting consecutive count.", globalAtivePower);
                }

                _consecutiveHighPowerReadings = 0;
                return null;
            }

            if(_consecutiveHighPowerReadings >= RequiredConsecutiveReadings)
            {
                _consecutiveHighPowerReadings = 0; // Reset after triggering command

                var command = new MonitoringCommand
                {
                    DeviceId = reading.DeviceName,
                    CommandName = "TURN_OFF_HIGH_LOAD_DEVICE",
                    TriggeredAt = DateTime.UtcNow,
                    Reason = $"Power exceeded threshold of {PowerTreshold} for {RequiredConsecutiveReadings} consecutive readings."
                };

                _logger.LogError("ALARM TRIGGERED - {CommandName} for device {DeviceId}. Reason: {Reason}", command.CommandName, command.DeviceId, command.Reason);

                return command;
            }

            return null;
        }

    }
}
