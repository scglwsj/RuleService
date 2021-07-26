using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RulesEngine.Extensions;
using RulesEngine.Models;

namespace RuleService.Rules
{
    public class RuleService
    {
        private readonly RulesEngine.RulesEngine _engine;

        public RuleService()
        {
            const string data = @"
                [
                    {
                        ""WorkflowName"": ""Register"",
                        ""Rules"": [
                            {
                                ""RuleName"": ""Give100Points"",
                                ""SuccessEvent"": ""Register success and earn 100 points"",
                                ""RuleExpressionType"": ""LambdaExpression"",
                                ""Expression"": ""true""
                            }
                        ]
                    },
                    {
                        ""WorkflowName"": ""Payment"",
                        ""Rules"": [
                            {
                                ""RuleName"": ""GivePointsByPayment"",
                                ""SuccessEvent"": ""Pay success and earn {0} points"",
                                ""RuleExpressionType"": ""LambdaExpression"",
                                ""Expression"": ""true"",
                                ""Actions"": {
                                    ""OnSuccess"": {
                                        ""Name"": ""OutputExpression"",
                                        ""Context"": {
                                            ""Expression"": ""subtotal * 0.5""
                                        }
                                    }
                                }
                            }
                        ]
                    }
                ]";

            var workflowRules = JsonConvert.DeserializeObject<WorkflowRules[]>(data);
            _engine = new RulesEngine.RulesEngine(workflowRules);
        }

        public async Task<RuleResponse> Verify(string workFlowName, JsonElement args)
        {
            var input = new List<RuleParameter>();
            switch (workFlowName)
            {
                case "Payment":
                    input.Add(new RuleParameter("subtotal", args.GetProperty("subtotal").GetInt64()));
                    break;
            }

            RuleResponse response = null;
            var resultList = await _engine.ExecuteAllRulesAsync(workFlowName, input.ToArray());

            resultList.OnSuccess(message =>
            {
                var actionResult = resultList
                    .FirstOrDefault(ruleResult => ruleResult.ActionResult != null)
                    ?.ActionResult.Output;
                response = new RuleResponse() {message = string.Format(message, actionResult)};
            });

            return response;
        }
    }
}
