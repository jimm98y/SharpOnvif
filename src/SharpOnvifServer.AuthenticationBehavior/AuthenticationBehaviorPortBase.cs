using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AuthenticationBehavior
{
    [DisableMustUnderstandValidation]
    public class AuthenticationBehaviorPortBase : AuthenticationBehaviorPort
    {
        [return: MessageParameter(Name = "Token")]
        public virtual string CreateAuthenticationProfile(AuthenticationProfile AuthenticationProfile)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateSecurityLevel(SecurityLevel SecurityLevel)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteAuthenticationProfile(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteSecurityLevel(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AuthenticationProfileInfo")]
        public virtual GetAuthenticationProfileInfoResponse GetAuthenticationProfileInfo(GetAuthenticationProfileInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAuthenticationProfileInfoListResponse GetAuthenticationProfileInfoList(GetAuthenticationProfileInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetAuthenticationProfileListResponse GetAuthenticationProfileList(GetAuthenticationProfileListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "AuthenticationProfile")]
        public virtual GetAuthenticationProfilesResponse GetAuthenticationProfiles(GetAuthenticationProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SecurityLevelInfo")]
        public virtual GetSecurityLevelInfoResponse GetSecurityLevelInfo(GetSecurityLevelInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetSecurityLevelInfoListResponse GetSecurityLevelInfoList(GetSecurityLevelInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetSecurityLevelListResponse GetSecurityLevelList(GetSecurityLevelListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "SecurityLevel")]
        public virtual GetSecurityLevelsResponse GetSecurityLevels(GetSecurityLevelsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        public virtual void ModifyAuthenticationProfile(AuthenticationProfile AuthenticationProfile)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifySecurityLevel(SecurityLevel SecurityLevel)
        {
            throw new NotImplementedException();
        }

        public virtual void SetAuthenticationProfile(AuthenticationProfile AuthenticationProfile)
        {
            throw new NotImplementedException();
        }

        public virtual void SetSecurityLevel(SecurityLevel SecurityLevel)
        {
            throw new NotImplementedException();
        }
    }
}
