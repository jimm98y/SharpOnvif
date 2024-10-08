﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharpOnvifClient.AppMgmt
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", ConfigurationName="SharpOnvifClient.AppMgmt.AppManagement")]
    public interface AppManagement
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Uninstall", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.UninstallResponse> UninstallAsync(SharpOnvifClient.AppMgmt.UninstallRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetInstalledApps", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetInstalledAppsResponse> GetInstalledAppsAsync(SharpOnvifClient.AppMgmt.GetInstalledAppsRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetAppsInfo", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetAppsInfoResponse> GetAppsInfoAsync(SharpOnvifClient.AppMgmt.GetAppsInfoRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Activate", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.ActivateResponse> ActivateAsync(SharpOnvifClient.AppMgmt.ActivateRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Deactivate", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.DeactivateResponse> DeactivateAsync(SharpOnvifClient.AppMgmt.DeactivateRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetServiceCapabilities", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetServiceCapabilitiesResponse> GetServiceCapabilitiesAsync(SharpOnvifClient.AppMgmt.GetServiceCapabilitiesRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/InstallLicense", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.InstallLicenseResponse> InstallLicenseAsync(SharpOnvifClient.AppMgmt.InstallLicenseRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetDeviceId", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetDeviceIdResponse> GetDeviceIdAsync(SharpOnvifClient.AppMgmt.GetDeviceIdRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="Uninstall", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class UninstallRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        public UninstallRequest()
        {
        }
        
        public UninstallRequest(string AppID)
        {
            this.AppID = AppID;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="UninstallResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class UninstallResponse
    {
        
        public UninstallResponse()
        {
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class GetInstalledAppsResponseApp
    {
        
        private string nameField;
        
        private string appIDField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string AppID
        {
            get
            {
                return this.appIDField;
            }
            set
            {
                this.appIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=2)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class Capabilities
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private string[] formatsSupportedField;
        
        private bool licensingField;
        
        private bool licensingFieldSpecified;
        
        private string uploadPathField;
        
        private string eventTopicPrefixField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=0)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string[] FormatsSupported
        {
            get
            {
                return this.formatsSupportedField;
            }
            set
            {
                this.formatsSupportedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool Licensing
        {
            get
            {
                return this.licensingField;
            }
            set
            {
                this.licensingField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LicensingSpecified
        {
            get
            {
                return this.licensingFieldSpecified;
            }
            set
            {
                this.licensingFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(DataType="anyURI")]
        public string UploadPath
        {
            get
            {
                return this.uploadPathField;
            }
            set
            {
                this.uploadPathField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string EventTopicPrefix
        {
            get
            {
                return this.eventTopicPrefixField;
            }
            set
            {
                this.eventTopicPrefixField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class LicenseInfo
    {
        
        private string nameField;
        
        private System.DateTime validFromField;
        
        private bool validFromFieldSpecified;
        
        private System.DateTime validUntilField;
        
        private bool validUntilFieldSpecified;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public System.DateTime ValidFrom
        {
            get
            {
                return this.validFromField;
            }
            set
            {
                this.validFromField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ValidFromSpecified
        {
            get
            {
                return this.validFromFieldSpecified;
            }
            set
            {
                this.validFromFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public System.DateTime ValidUntil
        {
            get
            {
                return this.validUntilField;
            }
            set
            {
                this.validUntilField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ValidUntilSpecified
        {
            get
            {
                return this.validUntilFieldSpecified;
            }
            set
            {
                this.validUntilFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=3)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class AppInfo
    {
        
        private string appIDField;
        
        private string nameField;
        
        private string versionField;
        
        private LicenseInfo[] licensesField;
        
        private string[] privilegesField;
        
        private System.DateTime installationDateField;
        
        private System.DateTime lastUpdateField;
        
        private AppState stateField;
        
        private string statusField;
        
        private bool autostartField;
        
        private string websiteField;
        
        private string openSourceField;
        
        private string configurationField;
        
        private string[] interfaceDescriptionField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=0)]
        public string AppID
        {
            get
            {
                return this.appIDField;
            }
            set
            {
                this.appIDField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=1)]
        public string Name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=2)]
        public string Version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Licenses", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=3)]
        public LicenseInfo[] Licenses
        {
            get
            {
                return this.licensesField;
            }
            set
            {
                this.licensesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("Privileges", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=4)]
        public string[] Privileges
        {
            get
            {
                return this.privilegesField;
            }
            set
            {
                this.privilegesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=5)]
        public System.DateTime InstallationDate
        {
            get
            {
                return this.installationDateField;
            }
            set
            {
                this.installationDateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=6)]
        public System.DateTime LastUpdate
        {
            get
            {
                return this.lastUpdateField;
            }
            set
            {
                this.lastUpdateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=7)]
        public AppState State
        {
            get
            {
                return this.stateField;
            }
            set
            {
                this.stateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=8)]
        public string Status
        {
            get
            {
                return this.statusField;
            }
            set
            {
                this.statusField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, Order=9)]
        public bool Autostart
        {
            get
            {
                return this.autostartField;
            }
            set
            {
                this.autostartField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="anyURI", Order=10)]
        public string Website
        {
            get
            {
                return this.websiteField;
            }
            set
            {
                this.websiteField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="anyURI", Order=11)]
        public string OpenSource
        {
            get
            {
                return this.openSourceField;
            }
            set
            {
                this.openSourceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="anyURI", Order=12)]
        public string Configuration
        {
            get
            {
                return this.configurationField;
            }
            set
            {
                this.configurationField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("InterfaceDescription", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, DataType="anyURI", Order=13)]
        public string[] InterfaceDescription
        {
            get
            {
                return this.interfaceDescriptionField;
            }
            set
            {
                this.interfaceDescriptionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=14)]
        public System.Xml.XmlElement[] Any
        {
            get
            {
                return this.anyField;
            }
            set
            {
                this.anyField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public enum AppState
    {
        
        /// <remarks/>
        Active,
        
        /// <remarks/>
        Inactive,
        
        /// <remarks/>
        Installing,
        
        /// <remarks/>
        Uninstalling,
        
        /// <remarks/>
        Removed,
        
        /// <remarks/>
        InstallationFailed,
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetInstalledApps", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetInstalledAppsRequest
    {
        
        public GetInstalledAppsRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetInstalledAppsResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetInstalledAppsResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("App", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SharpOnvifClient.AppMgmt.GetInstalledAppsResponseApp[] App;
        
        public GetInstalledAppsResponse()
        {
        }
        
        public GetInstalledAppsResponse(SharpOnvifClient.AppMgmt.GetInstalledAppsResponseApp[] App)
        {
            this.App = App;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetAppsInfo", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetAppsInfoRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        public GetAppsInfoRequest()
        {
        }
        
        public GetAppsInfoRequest(string AppID)
        {
            this.AppID = AppID;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetAppsInfoResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetAppsInfoResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("Info", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SharpOnvifClient.AppMgmt.AppInfo[] Info;
        
        public GetAppsInfoResponse()
        {
        }
        
        public GetAppsInfoResponse(SharpOnvifClient.AppMgmt.AppInfo[] Info)
        {
            this.Info = Info;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="Activate", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class ActivateRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        public ActivateRequest()
        {
        }
        
        public ActivateRequest(string AppID)
        {
            this.AppID = AppID;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="ActivateResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class ActivateResponse
    {
        
        public ActivateResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="Deactivate", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class DeactivateRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        public DeactivateRequest()
        {
        }
        
        public DeactivateRequest(string AppID)
        {
            this.AppID = AppID;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="DeactivateResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class DeactivateResponse
    {
        
        public DeactivateResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetServiceCapabilities", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetServiceCapabilitiesRequest
    {
        
        public GetServiceCapabilitiesRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetServiceCapabilitiesResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetServiceCapabilitiesResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public SharpOnvifClient.AppMgmt.Capabilities Capabilities;
        
        public GetServiceCapabilitiesResponse()
        {
        }
        
        public GetServiceCapabilitiesResponse(SharpOnvifClient.AppMgmt.Capabilities Capabilities)
        {
            this.Capabilities = Capabilities;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="InstallLicense", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class InstallLicenseRequest
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=1)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string License;
        
        public InstallLicenseRequest()
        {
        }
        
        public InstallLicenseRequest(string AppID, string License)
        {
            this.AppID = AppID;
            this.License = License;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="InstallLicenseResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class InstallLicenseResponse
    {
        
        public InstallLicenseResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetDeviceId", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetDeviceIdRequest
    {
        
        public GetDeviceIdRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetDeviceIdResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetDeviceIdResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string DeviceId;
        
        public GetDeviceIdResponse()
        {
        }
        
        public GetDeviceIdResponse(string DeviceId)
        {
            this.DeviceId = DeviceId;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    public interface AppManagementChannel : SharpOnvifClient.AppMgmt.AppManagement, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    public partial class AppManagementClient : System.ServiceModel.ClientBase<SharpOnvifClient.AppMgmt.AppManagement>, SharpOnvifClient.AppMgmt.AppManagement
    {
        
        public AppManagementClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.UninstallResponse> SharpOnvifClient.AppMgmt.AppManagement.UninstallAsync(SharpOnvifClient.AppMgmt.UninstallRequest request)
        {
            return base.Channel.UninstallAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.UninstallResponse> UninstallAsync(string AppID)
        {
            SharpOnvifClient.AppMgmt.UninstallRequest inValue = new SharpOnvifClient.AppMgmt.UninstallRequest();
            inValue.AppID = AppID;
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).UninstallAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetInstalledAppsResponse> SharpOnvifClient.AppMgmt.AppManagement.GetInstalledAppsAsync(SharpOnvifClient.AppMgmt.GetInstalledAppsRequest request)
        {
            return base.Channel.GetInstalledAppsAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetInstalledAppsResponse> GetInstalledAppsAsync()
        {
            SharpOnvifClient.AppMgmt.GetInstalledAppsRequest inValue = new SharpOnvifClient.AppMgmt.GetInstalledAppsRequest();
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).GetInstalledAppsAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetAppsInfoResponse> SharpOnvifClient.AppMgmt.AppManagement.GetAppsInfoAsync(SharpOnvifClient.AppMgmt.GetAppsInfoRequest request)
        {
            return base.Channel.GetAppsInfoAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetAppsInfoResponse> GetAppsInfoAsync(string AppID)
        {
            SharpOnvifClient.AppMgmt.GetAppsInfoRequest inValue = new SharpOnvifClient.AppMgmt.GetAppsInfoRequest();
            inValue.AppID = AppID;
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).GetAppsInfoAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.ActivateResponse> SharpOnvifClient.AppMgmt.AppManagement.ActivateAsync(SharpOnvifClient.AppMgmt.ActivateRequest request)
        {
            return base.Channel.ActivateAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.ActivateResponse> ActivateAsync(string AppID)
        {
            SharpOnvifClient.AppMgmt.ActivateRequest inValue = new SharpOnvifClient.AppMgmt.ActivateRequest();
            inValue.AppID = AppID;
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).ActivateAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.DeactivateResponse> SharpOnvifClient.AppMgmt.AppManagement.DeactivateAsync(SharpOnvifClient.AppMgmt.DeactivateRequest request)
        {
            return base.Channel.DeactivateAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.DeactivateResponse> DeactivateAsync(string AppID)
        {
            SharpOnvifClient.AppMgmt.DeactivateRequest inValue = new SharpOnvifClient.AppMgmt.DeactivateRequest();
            inValue.AppID = AppID;
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).DeactivateAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetServiceCapabilitiesResponse> SharpOnvifClient.AppMgmt.AppManagement.GetServiceCapabilitiesAsync(SharpOnvifClient.AppMgmt.GetServiceCapabilitiesRequest request)
        {
            return base.Channel.GetServiceCapabilitiesAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetServiceCapabilitiesResponse> GetServiceCapabilitiesAsync()
        {
            SharpOnvifClient.AppMgmt.GetServiceCapabilitiesRequest inValue = new SharpOnvifClient.AppMgmt.GetServiceCapabilitiesRequest();
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).GetServiceCapabilitiesAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.InstallLicenseResponse> SharpOnvifClient.AppMgmt.AppManagement.InstallLicenseAsync(SharpOnvifClient.AppMgmt.InstallLicenseRequest request)
        {
            return base.Channel.InstallLicenseAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.InstallLicenseResponse> InstallLicenseAsync(string AppID, string License)
        {
            SharpOnvifClient.AppMgmt.InstallLicenseRequest inValue = new SharpOnvifClient.AppMgmt.InstallLicenseRequest();
            inValue.AppID = AppID;
            inValue.License = License;
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).InstallLicenseAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetDeviceIdResponse> SharpOnvifClient.AppMgmt.AppManagement.GetDeviceIdAsync(SharpOnvifClient.AppMgmt.GetDeviceIdRequest request)
        {
            return base.Channel.GetDeviceIdAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.AppMgmt.GetDeviceIdResponse> GetDeviceIdAsync()
        {
            SharpOnvifClient.AppMgmt.GetDeviceIdRequest inValue = new SharpOnvifClient.AppMgmt.GetDeviceIdRequest();
            return ((SharpOnvifClient.AppMgmt.AppManagement)(this)).GetDeviceIdAsync(inValue);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }

#if !NETCOREAPP
        public virtual System.Threading.Tasks.Task CloseAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
#endif
    }
}
