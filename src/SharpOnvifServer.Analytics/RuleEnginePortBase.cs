using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Analytics
{
    [DisableMustUnderstandValidation]
    public class RuleEnginePortBase : RuleEnginePort
    {
        public virtual CreateRulesResponse CreateRules(CreateRulesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteRulesResponse DeleteRules(DeleteRulesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "RuleOptions")]
        public virtual GetRuleOptionsResponse GetRuleOptions(GetRuleOptionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Rule")]
        public virtual GetRulesResponse GetRules(GetRulesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SupportedRules")]
        public virtual SupportedRules GetSupportedRules(string ConfigurationToken)
        {
            throw new NotImplementedException();
        }

        public virtual ModifyRulesResponse ModifyRules(ModifyRulesRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
