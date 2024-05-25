using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AdvancedSecurity
{
    [DisableMustUnderstandValidation]
    public class KeystoreBase : Keystore
    {
        [return: MessageParameter(Name = "CertificationPathID")]
        public virtual CreateCertificationPathResponse CreateCertificationPath(CreateCertificationPathRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertPathValidationPolicyID")]
        public virtual CreateCertPathValidationPolicyResponse CreateCertPathValidationPolicy(CreateCertPathValidationPolicyRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual CreateECCKeyPairResponse CreateECCKeyPair(CreateECCKeyPairRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PKCS10CSR")]
        public virtual CreatePKCS10CSRResponse CreatePKCS10CSR(CreatePKCS10CSRRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual CreateRSAKeyPairResponse CreateRSAKeyPair(CreateRSAKeyPairRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificateID")]
        public virtual CreateSelfSignedCertificateResponse CreateSelfSignedCertificate(CreateSelfSignedCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCertificateResponse DeleteCertificate(DeleteCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCertificationPathResponse DeleteCertificationPath(DeleteCertificationPathRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCertPathValidationPolicyResponse DeleteCertPathValidationPolicy(DeleteCertPathValidationPolicyRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteCRLResponse DeleteCRL(DeleteCRLRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeleteKeyResponse DeleteKey(DeleteKeyRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeletePassphraseResponse DeletePassphrase(DeletePassphraseRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Certificate")]
        public virtual GetAllCertificatesResponse GetAllCertificates(GetAllCertificatesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificationPathID")]
        public virtual GetAllCertificationPathsResponse GetAllCertificationPaths(GetAllCertificationPathsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertPathValidationPolicy")]
        public virtual GetAllCertPathValidationPoliciesResponse GetAllCertPathValidationPolicies(GetAllCertPathValidationPoliciesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Crl")]
        public virtual GetAllCRLsResponse GetAllCRLs(GetAllCRLsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "KeyAttribute")]
        public virtual GetAllKeysResponse GetAllKeys(GetAllKeysRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PassphraseAttribute")]
        public virtual GetAllPassphrasesResponse GetAllPassphrases(GetAllPassphrasesRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Certificate")]
        public virtual GetCertificateResponse GetCertificate(GetCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertificationPath")]
        public virtual GetCertificationPathResponse GetCertificationPath(GetCertificationPathRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CertPathValidationPolicy")]
        public virtual GetCertPathValidationPolicyResponse GetCertPathValidationPolicy(GetCertPathValidationPolicyRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Crl")]
        public virtual GetCRLResponse GetCRL(GetCRLRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "KeyStatus")]
        public virtual GetKeyStatusResponse GetKeyStatus(GetKeyStatusRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "hasPrivateKey")]
        public virtual GetPrivateKeyStatusResponse GetPrivateKeyStatus(GetPrivateKeyStatusRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UploadCertificateResponse UploadCertificate(UploadCertificateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UploadCertificateWithPrivateKeyInPKCS12Response UploadCertificateWithPrivateKeyInPKCS12(UploadCertificateWithPrivateKeyInPKCS12Request request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "CrlID")]
        public virtual UploadCRLResponse UploadCRL(UploadCRLRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "KeyID")]
        public virtual UploadKeyPairInPKCS8Response UploadKeyPairInPKCS8(UploadKeyPairInPKCS8Request request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "PassphraseID")]
        public virtual UploadPassphraseResponse UploadPassphrase(UploadPassphraseRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
