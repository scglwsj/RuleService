using System.Text.Json;

namespace RuleService.Rules
{
    public class RuleRequest
    {
        public string RuleName { get; init; }
        public JsonElement Args { get; init; }
    }
}
