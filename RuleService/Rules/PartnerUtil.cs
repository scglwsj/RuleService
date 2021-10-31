using System.Collections.Generic;

namespace RuleService.Rules
{
    public static class PartnerUtil
    {
        private static readonly List<string> ValidPartnerNames = new()
        {
            "AS", "DF", "GH"
        };

        public static bool ValidatePartner(string partnerName) => ValidPartnerNames.Contains(partnerName);
    }
}
