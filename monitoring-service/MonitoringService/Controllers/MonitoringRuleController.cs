using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MonitoringService.Dtos;
using MonitoringService.Services;

namespace MonitoringService.Controllers
{
    [Route("api/monitoring/rule")]
    [ApiController]
    public class MonitoringRuleController : ControllerBase
    {
        private readonly MonitoringRuleState _ruleState;
        private readonly MonitoringRuleEngine _ruleEngine;
        private readonly EdgeXCommandService _edgeXCommandService;

        public MonitoringRuleController(MonitoringRuleState ruleState, MonitoringRuleEngine ruleEngine, EdgeXCommandService edgeXCommandService)
        {
            _ruleState = ruleState;
            _ruleEngine = ruleEngine;
            _edgeXCommandService = edgeXCommandService;
        }

        [HttpGet]
        public IActionResult GetRule()
        {
            var rule = _ruleState.GetRule();
            if (rule == null)
            {
                return NotFound();
            }
            return Ok(rule);
        }

        [HttpPut]
        public IActionResult UpdateRule([FromBody] UpdateMonitoringRuleRequest request)
        {
            try
            {
                var updatedRule = _ruleState.UpdateRule(request);
                _ruleEngine.ResetCounter();

                return Ok(new
                {
                    Message = "Monitoring rule updated successfully.",
                    UpdatedRule = updatedRule
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("reset-counter")]
        public IActionResult ResetCounter()
        {
            _ruleEngine.ResetCounter();

            return Ok(new
            {
                message = "Monitoring rule counter reset successfully."
            });
        }

        [HttpPost("reset-defaults")]
        public IActionResult ResetToDefaults()
        {
            var defaultRule = _ruleState.ResetToDefaults();
            _ruleEngine.ResetCounter();

            return Ok(new
            {
                message = "Monitoring rule reset to default values.",
                rule = defaultRule
            });
        }

        
        [HttpPost("load-shed/on")]
        public async Task<IActionResult> EnableLoadShed(CancellationToken cancellationToken)
        {
            var rule = _ruleState.GetRule();

            await _edgeXCommandService.SendLoadShedCommandAsync(
                rule.DeviceName,
                true,
                cancellationToken);

            _ruleEngine.ResetCounter();

            return Ok(new
            {
                message = "Load shedding enabled through EdgeX command.",
                deviceName = rule.DeviceName,
                loadShedEnabled = true
            });
        }

        //For testing purposes, we can also have an endpoint to disable load shedding. In a real-world scenario, this might be triggered by a different event or condition.
        [HttpPost("load-shed/off")]
        public async Task<IActionResult> DisableLoadShed(CancellationToken cancellationToken)
        {
            var rule = _ruleState.GetRule();

            await _edgeXCommandService.SendLoadShedCommandAsync(
                rule.DeviceName,
                false,
                cancellationToken);

            _ruleEngine.ResetCounter();

            return Ok(new
            {
                message = "Load shedding disabled through EdgeX command.",
                deviceName = rule.DeviceName,
                loadShedEnabled = false
            });
        }
    }
}
