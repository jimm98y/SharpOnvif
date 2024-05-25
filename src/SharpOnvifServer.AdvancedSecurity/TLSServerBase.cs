using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class TLSServerBase : TLSServer
    {
        public virtual AddCertPathValidationPolicyAssignmentResponse AddCertPathValidationPolicyAssignment(AddCertPathValidationPolicyAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual AddServerCertificateAssignmentResponse AddServerCertificateAssignment(AddServerCertificateAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertPathValidationPolicyID")]
        public virtual GetAssignedCertPathValidationPoliciesResponse GetAssignedCertPathValidationPolicies(GetAssignedCertPathValidationPoliciesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificationPathID")]
        public virtual GetAssignedServerCertificatesResponse GetAssignedServerCertificates(GetAssignedServerCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "clientAuthenticationRequired")]
        public virtual bool GetClientAuthenticationRequired()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "cnMapsToUser")]
        public virtual bool GetCnMapsToUser()
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Versions")]
        public virtual string GetEnabledTLSVersions()
        {
            throw new NotImplementedException();
        }

        public virtual RemoveCertPathValidationPolicyAssignmentResponse RemoveCertPathValidationPolicyAssignment(RemoveCertPathValidationPolicyAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual RemoveServerCertificateAssignmentResponse RemoveServerCertificateAssignment(RemoveServerCertificateAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ReplaceCertPathValidationPolicyAssignmentResponse ReplaceCertPathValidationPolicyAssignment(ReplaceCertPathValidationPolicyAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual ReplaceServerCertificateAssignmentResponse ReplaceServerCertificateAssignment(ReplaceServerCertificateAssignmentRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual void SetClientAuthenticationRequired(bool clientAuthenticationRequired)
        {
            throw new NotImplementedException();
        }

        public virtual void SetCnMapsToUser(bool cnMapsToUser)
        {
            throw new NotImplementedException();
        }

        public virtual void SetEnabledTLSVersions(string Versions)
        {
            throw new NotImplementedException();
        }
    }
}
