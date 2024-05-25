using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AccessRules
{
    [DisableMustUnderstandValidation]
    public class AccessRulesBase : AccessRulesPort
    {
        [return: MessageParameter(Name = "Token")]
        public virtual string CreateAccessProfile(AccessProfile AccessProfile)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteAccessProfile(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AccessProfileInfo")]
        public virtual GetAccessProfileInfoResponse GetAccessProfileInfo(GetAccessProfileInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAccessProfileInfoListResponse GetAccessProfileInfoList(GetAccessProfileInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAccessProfileListResponse GetAccessProfileList(GetAccessProfileListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AccessProfile")]
        public virtual GetAccessProfilesResponse GetAccessProfiles(GetAccessProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void ModifyAccessProfile(AccessProfile AccessProfile)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAccessProfile(AccessProfile AccessProfile)
        {
            throw new NotImplementedException();
        }
    }
}
