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

        public MonitoringRuleController(MonitoringRuleState ruleState, MonitoringRuleEngine ruleEngine)
        {
            _ruleState = ruleState;
            _ruleEngine = ruleEngine;
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

        [HttpPost]
        public IActionResult ResetCounter()
        {
            _ruleEngine.ResetCounter();
            return Ok(new
            {
                Message = "Monitoring rule reset to default values."
            });
        }
    }
}
