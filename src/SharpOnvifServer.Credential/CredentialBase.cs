using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.Credential
{
    [DisableMustUnderstandValidation]
    public class CredentialBase : CredentialPort
    {
        public virtual AddToBlacklistResponse AddToBlacklist(AddToBlacklistRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual AddToWhitelistResponse AddToWhitelist(AddToWhitelistRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateCredential(Credential Credential, CredentialState State)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteBlacklist()
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteCredential(string Token)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCredentialAccessProfilesResponse DeleteCredentialAccessProfiles(DeleteCredentialAccessProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteCredentialIdentifier(string CredentialToken, string CredentialIdentifierTypeName)
        {
            throw new NotImplementedException();
        }

        public virtual void DeleteWhitelist()
        {
            throw new NotImplementedException();
        }

        public virtual void DisableCredential(string Token, string Reason)
        {
            throw new NotImplementedException();
        }

        public virtual void EnableCredential(string Token, string Reason)
        {
            throw new NotImplementedException();
        }

        public virtual GetBlacklistResponse GetBlacklist(GetBlacklistRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CredentialAccessProfile")]
        public virtual GetCredentialAccessProfilesResponse GetCredentialAccessProfiles(GetCredentialAccessProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CredentialIdentifier")]
        public virtual GetCredentialIdentifiersResponse GetCredentialIdentifiers(GetCredentialIdentifiersRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CredentialInfo")]
        public virtual GetCredentialInfoResponse GetCredentialInfo(GetCredentialInfoRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetCredentialInfoListResponse GetCredentialInfoList(GetCredentialInfoListRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetCredentialListResponse GetCredentialList(GetCredentialListRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Credential")]
        public virtual GetCredentialsResponse GetCredentials(GetCredentialsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "State")]
        public virtual CredentialState GetCredentialState(string Token)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "FormatTypeInfo")]
        public virtual GetSupportedFormatTypesResponse GetSupportedFormatTypes(GetSupportedFormatTypesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual GetWhitelistResponse GetWhitelist(GetWhitelistRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void ModifyCredential(Credential Credential)
        {
            throw new NotImplementedException();
        }

        public virtual RemoveFromBlacklistResponse RemoveFromBlacklist(RemoveFromBlacklistRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RemoveFromWhitelistResponse RemoveFromWhitelist(RemoveFromWhitelistRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void ResetAntipassbackViolation(string CredentialToken)
        {
            throw new NotImplementedException();
        }

        public virtual void SetCredential(CredentialData CredentialData)
        {
            throw new NotImplementedException();
        }

        public virtual SetCredentialAccessProfilesResponse SetCredentialAccessProfiles(SetCredentialAccessProfilesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetCredentialIdentifier(string CredentialToken, CredentialIdentifier CredentialIdentifier)
        {
            throw new NotImplementedException();
        }
    }
}
