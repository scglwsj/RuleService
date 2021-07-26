using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace RuleService.Rules
{
    [Route("rules")]
    [ApiController]
    public class RuleController : ControllerBase
    {
        private readonly RuleService _ruleService;

        public RuleController(RuleService ruleService)
        {
            _ruleService = ruleService;
        }

        [HttpPost("verification")]
        public Task<RuleResponse> Verify(RuleRequest request) =>
            _ruleService.Verify(
                request.RuleName,
                request.Args
            );
    }
}
