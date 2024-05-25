using CoreWCF;
using SharpOnvifServer.Security;

namespace SharpOnvifServer.AccessControl
{
    [DisableMustUnderstandValidation]
    public class AccessControlBase : PACSPort
    {
        [return: MessageParameter(Name = "Token")]
        public virtual string CreateAccessPoint(AccessPoint AccessPoint)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Token")]
        public virtual string CreateArea(Area Area)
        {
            throw new System.NotImplementedException();
        }

        public virtual void DeleteAccessPoint(string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual void DeleteAccessPointAuthenticationProfile(string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual void DeleteArea(string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual void DisableAccessPoint(string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual void EnableAccessPoint(string Token)
        {
            throw new System.NotImplementedException();
        }

        public virtual void ExternalAuthorization(string AccessPointToken, string CredentialToken, string Reason, Decision Decision)
        {
            throw new System.NotImplementedException();
        }

        public virtual FeedbackResponse Feedback(FeedbackRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AccessPointInfo")]
        public virtual GetAccessPointInfoResponse GetAccessPointInfo(GetAccessPointInfoRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetAccessPointInfoListResponse GetAccessPointInfoList(GetAccessPointInfoListRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetAccessPointListResponse GetAccessPointList(GetAccessPointListRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AccessPoint")]
        public virtual GetAccessPointsResponse GetAccessPoints(GetAccessPointsRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AccessPointState")]
        public virtual AccessPointState GetAccessPointState(string Token)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "AreaInfo")]
        public virtual GetAreaInfoResponse GetAreaInfo(GetAreaInfoRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetAreaInfoListResponse GetAreaInfoList(GetAreaInfoListRequest request)
        {
            throw new System.NotImplementedException();
        }

        public virtual GetAreaListResponse GetAreaList(GetAreaListRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Area")]
        public virtual GetAreasResponse GetAreas(GetAreasRequest request)
        {
            throw new System.NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual ServiceCapabilities GetServiceCapabilities()
        {
            throw new System.NotImplementedException();
        }

        public virtual void ModifyAccessPoint(AccessPoint AccessPoint)
        {
            throw new System.NotImplementedException();
        }

        public virtual void ModifyArea(Area Area)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAccessPoint(AccessPoint AccessPoint)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetAccessPointAuthenticationProfile(string Token, string AuthenticationProfileToken)
        {
            throw new System.NotImplementedException();
        }

        public virtual void SetArea(Area Area)
        {
            throw new System.NotImplementedException();
        }
    }
}
