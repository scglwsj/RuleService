using System;
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
                                            ""Expression"": ""subtotal""
                                        }
                                    }
                                }
                            },
                            {
                                ""RuleName"": ""MultiplyPoints"",
                                ""Operator"": ""Or"",
                                ""Rules"": [
                                    {
                                        ""RuleName"": ""MoreThan200"",
                                        ""ErrorMessage"": ""subtotal is less than or equal to 200"",
                                        ""Expression"": ""subtotal > 200""
                                    },
                                    {
                                        ""RuleName"": ""DoubleEleven"",
                                        ""RuleExpressionType"": ""LambdaExpression"",
                                        ""Expression"": ""month == 11 AND date == 11""
                                    }
                                ],
                                ""Actions"": {
                                    ""OnSuccess"": {
                                        ""Name"": ""OutputExpression"",
                                        ""Context"": {
                                            ""Expression"": ""subtotal""
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
                    input.Add(new RuleParameter("subtotal", args.GetProperty("subtotal").GetDecimal()));
                    input.Add(new RuleParameter("month", args.GetProperty("month").GetInt32()));
                    input.Add(new RuleParameter("date", args.GetProperty("date").GetInt32()));
                    break;
            }

            RuleResponse response = null;
            var resultList = await _engine.ExecuteAllRulesAsync(workFlowName, input.ToArray());

            resultList.OnSuccess(message =>
            {
                var actionResult = resultList
                    .FindAll(ruleResult => ruleResult.ActionResult != null)
                    .Select(ruleResult => ruleResult.ActionResult.Output)
                    .Sum(Convert.ToDecimal);
                response = new RuleResponse() {Message = string.Format(message, actionResult)};
            });

            return response;
        }
    }
}
