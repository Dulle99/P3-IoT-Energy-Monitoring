using Microsoft.AspNetCore.Mvc.Formatters;
using MonitoringService.Dtos;
using MonitoringService.Models;
using System.Globalization;

namespace MonitoringService.Services
{
    public class MonitoringRuleEngine
    {
        private readonly ILogger<MonitoringRuleEngine> _logger;
        private readonly MonitoringRuleState _ruleState;

        private int _conescutiveHighPowerReadings = 0;
        private bool _alarmAlreadyTriggered = false;

        public MonitoringRuleEngine(ILogger<MonitoringRuleEngine> logger, MonitoringRuleState ruleState)
        {
            _logger = logger;
            _ruleState = ruleState;
        }

        public MonitoringCommand? Evaluate(EdgeXReading reading)
        {
            var rule = _ruleState.GetRule();

            if (!rule.Enabled)
            {
                return null;
            }

            if (!string.Equals(reading.DeviceName, rule.DeviceName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!string.Equals(reading.ResourceName, rule.ResourceName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!double.TryParse(reading.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double readingValue))
            {
                _logger.LogWarning("Failed to parse reading value: {Value}", reading.Value);
                return null;
            }

            if (readingValue > rule.Threshold)
            {
                _conescutiveHighPowerReadings++;
                _logger.LogInformation("[THRESHOLD EXCEEDED] - High reading detected: {Value}. Consecutive count: {Count}", readingValue, _conescutiveHighPowerReadings);
            }
            else
            {
                if (_conescutiveHighPowerReadings > 0 || _alarmAlreadyTriggered)
                {
                    _logger.LogInformation("Reading back to normal: {Value}. Resetting consecutive count.", readingValue);
                }
                _conescutiveHighPowerReadings = 0;
                _alarmAlreadyTriggered= false;
                return null;
            }

            if (_conescutiveHighPowerReadings >= rule.RequiredConsecutiveReadings)
            {
                if (_alarmAlreadyTriggered) { 
                    _logger.LogInformation("Alarm already triggered for this event. Skipping duplicate command.");
                    return null;
                }

                {
                    _conescutiveHighPowerReadings = 0; // Reset after triggering command
                    _alarmAlreadyTriggered = true; // Set flag to indicate alarm has been triggered
                    var command = new MonitoringCommand
                    {
                        DeviceId = reading.DeviceName,
                        CommandName = rule.CommandName,
                        TriggeredAt = DateTime.UtcNow,
                        Reason = $"Reading exceeded threshold of {rule.Threshold} for {rule.RequiredConsecutiveReadings} consecutive readings."
                    };

                    _logger.LogError("ALARM TRIGGERED - {CommandName} for device {DeviceId}. Reason: {Reason}", command.CommandName, command.DeviceId, command.Reason);
                    return command;
                }
            }
            return null;
        }

        public void ResetCounter()
        {
            _conescutiveHighPowerReadings = 0;
            _alarmAlreadyTriggered= false;
            _logger.LogInformation("Consecutive high reading counter reset manually.");
        }
    }
}



        /*
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
        */

   
