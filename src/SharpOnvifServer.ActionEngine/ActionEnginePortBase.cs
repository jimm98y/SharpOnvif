using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.ActionEngine
{
    [DisableMustUnderstandValidation]
    public class ActionEnginePortBase : ActionEnginePort
    {
        public virtual CreateActionsResponse CreateActions(CreateActionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual CreateActionTriggersResponse CreateActionTriggers(CreateActionTriggersRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteActionsResponse DeleteActions(DeleteActionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteActionTriggersResponse DeleteActionTriggers(DeleteActionTriggersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Action")]
        public virtual GetActionsResponse GetActions(GetActionsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "ActionTrigger")]
        public virtual GetActionTriggersResponse GetActionTriggers(GetActionTriggersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ActionEngineCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SupportedActions")]
        public virtual SupportedActions GetSupportedActions()
        {
            throw new NotImplementedException();
        }

        public virtual ModifyActionsResponse ModifyActions(ModifyActionsRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ModifyActionTriggersResponse ModifyActionTriggers(ModifyActionTriggersRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
