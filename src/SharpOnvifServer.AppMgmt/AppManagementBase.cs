using CoreWCF;
using SharpOnvifServer.Security;
using System;

namespace SharpOnvifServer.AppMgmt
{
    [DisableMustUnderstandValidation]
    public class AppManagementBase : AppManagement
    {
        public virtual ActivateResponse Activate(ActivateRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual DeactivateResponse Deactivate(DeactivateRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Info")]
        public virtual GetAppsInfoResponse GetAppsInfo(GetAppsInfoRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "DeviceId")]
        public virtual GetDeviceIdResponse GetDeviceId(GetDeviceIdRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "App")]
        public virtual GetInstalledAppsResponse GetInstalledApps(GetInstalledAppsRequest request)
        {
            throw new NotImplementedException();
        }

        [return: MessageParameter(Name = "Capabilities")]
        public virtual GetServiceCapabilitiesResponse GetServiceCapabilities(GetServiceCapabilitiesRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual InstallLicenseResponse InstallLicense(InstallLicenseRequest request)
        {
            throw new NotImplementedException();
        }

        public virtual UninstallResponse Uninstall(UninstallRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
