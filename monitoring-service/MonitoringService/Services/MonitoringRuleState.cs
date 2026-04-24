using MonitoringService.Dtos;
using MonitoringService.Models;
using System.Data;

namespace MonitoringService.Services
{
    public class MonitoringRuleState
    {
        private readonly object _lock = new object();
        private MonitoringRule _rule = new MonitoringRule();

        public MonitoringRule GetRule()
        {
            lock (_lock)
            {
                return CloneRule(_rule);
            }
        }

        public MonitoringRule UpdateRule(UpdateMonitoringRuleRequest request)
        {
            lock (_lock)
            {
                if(request.Enabled.HasValue)
                {
                    _rule.Enabled = request.Enabled.Value;
                }

                if(!string.IsNullOrEmpty(request.DeviceName))
                {
                    _rule.DeviceName = request.DeviceName;
                }

                if(!string.IsNullOrEmpty(request.ResourceName))
                {
                    _rule.ResourceName = request.ResourceName;
                }

                if (request.Threshold.HasValue)
                {
                    if (request.Threshold <= 0)
                    {
                        throw new ArgumentException("Threshold must be non-negative.");
                    }
                    _rule.Threshold = request.Threshold.Value;
                }

                if (request.RequiredConsecutiveReadings.HasValue)
                {
                   if(request.RequiredConsecutiveReadings <= 0)
                    {
                        throw new ArgumentException("RequiredConsecutiveReadings must be at least 1.");
                    }
                    _rule.RequiredConsecutiveReadings = request.RequiredConsecutiveReadings.Value;
                }

                if(!string.IsNullOrEmpty(request.CommandName))
                {
                    _rule.CommandName = request.CommandName;
                }

                return CloneRule(_rule);
            }
        }

        public static MonitoringRule CloneRule(MonitoringRule rule)
        {
            return new MonitoringRule
            {
                Enabled = rule.Enabled,
                DeviceName = rule.DeviceName,
                ResourceName = rule.ResourceName,
                Threshold = rule.Threshold,
                RequiredConsecutiveReadings = rule.RequiredConsecutiveReadings,
                CommandName = rule.CommandName
            };
        }

        public MonitoringRule ResetToDefaults()
        {
            lock (_lock)
            {
                _rule = new MonitoringRule();
                return CloneRule(_rule);
            }
        }

    }
}
