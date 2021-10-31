using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RulesEngine.Extensions;
using RulesEngine.Models;
using JsonSerializer = System.Text.Json.JsonSerializer;

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
                    ""WorkflowName"": ""Transformation"",
                    ""Rules"": [
                        {
                            ""RuleName"": ""TransForMore"",
                            ""SuccessEvent"": ""Transfer points succeed"",
                            ""RuleExpressionType"": ""LambdaExpression"",
                            ""Expression"": ""PartnerUtil.ValidatePartner(partnerName)"",
                            ""ErrorMessage"": ""Transfer points failed, invalid partner name"",
                            ""ErrorType"": ""Error""
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
                },
                {
                    ""WorkflowName"": ""Events"",
                    ""Rules"": [
                        {
                            ""RuleName"": ""Completed one of the events"",
                            ""ErrorMessage"": ""None of the events is completed"",
                            ""ErrorType"": ""Error"",
                            ""SuccessEvent"": ""You got the event points!"",
                            ""RuleExpressionType"": ""LambdaExpression"",
                            ""localParams"": [
                                {
                                    ""name"": ""completedEvents"",
                                    ""expression"": ""events.Where(Status == \""Completed\"")""
                                }
                            ],
                            ""Expression"": ""completedEvents.Any()""
                        }
                    ]
                }
            ]";
            var workflowRules = JsonConvert.DeserializeObject<WorkflowRules[]>(data);

            var reSettings = new ReSettings
            {
                CustomTypes = new[] {typeof(PartnerUtil)}
            };
            _engine = new RulesEngine.RulesEngine(workflowRules, null, reSettings);
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
                case "Transformation":
                    input.Add(new RuleParameter("partnerName", args.GetProperty("partnerName").GetString()));
                    break;
                case "Events":
                    input.Add(new RuleParameter("events", JsonSerializer.Deserialize<List<Event>>(
                        args.GetProperty("events").GetRawText(),
                        new JsonSerializerOptions {PropertyNameCaseInsensitive = true})));
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
                response = new RuleResponse {Message = string.Format(message, actionResult)};
            });

            resultList.OnFail(() => { response = new RuleResponse {Message = "Trigger rule failed"}; });

            return response;
        }
    }

    public class Event
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }
}
