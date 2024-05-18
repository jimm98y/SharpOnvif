﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OnvifAppMgmt
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", ConfigurationName="OnvifAppMgmt.AppManagement")]
    public interface AppManagement
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Uninstall", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.UninstallResponse> UninstallAsync(OnvifAppMgmt.UninstallRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetInstalledApps", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetInstalledAppsResponse> GetInstalledAppsAsync(OnvifAppMgmt.GetInstalledAppsRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetAppsInfo", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetAppsInfoResponse> GetAppsInfoAsync(OnvifAppMgmt.GetAppsInfoRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Activate", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.ActivateResponse> ActivateAsync(OnvifAppMgmt.ActivateRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Deactivate", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.DeactivateResponse> DeactivateAsync(OnvifAppMgmt.DeactivateRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetServiceCapabilities", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetServiceCapabilitiesResponse> GetServiceCapabilitiesAsync(OnvifAppMgmt.GetServiceCapabilitiesRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/InstallLicense", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.InstallLicenseResponse> InstallLicenseAsync(OnvifAppMgmt.InstallLicenseRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetDeviceId", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetDeviceIdResponse> GetDeviceIdAsync(OnvifAppMgmt.GetDeviceIdRequest request);
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
        public OnvifAppMgmt.GetInstalledAppsResponseApp[] App;
        
        public GetInstalledAppsResponse()
        {
        }
        
        public GetInstalledAppsResponse(OnvifAppMgmt.GetInstalledAppsResponseApp[] App)
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
        public OnvifAppMgmt.AppInfo[] Info;
        
        public GetAppsInfoResponse()
        {
        }
        
        public GetAppsInfoResponse(OnvifAppMgmt.AppInfo[] Info)
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
        public OnvifAppMgmt.Capabilities Capabilities;
        
        public GetServiceCapabilitiesResponse()
        {
        }
        
        public GetServiceCapabilitiesResponse(OnvifAppMgmt.Capabilities Capabilities)
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
    public interface AppManagementChannel : OnvifAppMgmt.AppManagement, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    public partial class AppManagementClient : System.ServiceModel.ClientBase<OnvifAppMgmt.AppManagement>, OnvifAppMgmt.AppManagement
    {
        
        public AppManagementClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.UninstallResponse> OnvifAppMgmt.AppManagement.UninstallAsync(OnvifAppMgmt.UninstallRequest request)
        {
            return base.Channel.UninstallAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.UninstallResponse> UninstallAsync(string AppID)
        {
            OnvifAppMgmt.UninstallRequest inValue = new OnvifAppMgmt.UninstallRequest();
            inValue.AppID = AppID;
            return ((OnvifAppMgmt.AppManagement)(this)).UninstallAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetInstalledAppsResponse> OnvifAppMgmt.AppManagement.GetInstalledAppsAsync(OnvifAppMgmt.GetInstalledAppsRequest request)
        {
            return base.Channel.GetInstalledAppsAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.GetInstalledAppsResponse> GetInstalledAppsAsync()
        {
            OnvifAppMgmt.GetInstalledAppsRequest inValue = new OnvifAppMgmt.GetInstalledAppsRequest();
            return ((OnvifAppMgmt.AppManagement)(this)).GetInstalledAppsAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetAppsInfoResponse> OnvifAppMgmt.AppManagement.GetAppsInfoAsync(OnvifAppMgmt.GetAppsInfoRequest request)
        {
            return base.Channel.GetAppsInfoAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.GetAppsInfoResponse> GetAppsInfoAsync(string AppID)
        {
            OnvifAppMgmt.GetAppsInfoRequest inValue = new OnvifAppMgmt.GetAppsInfoRequest();
            inValue.AppID = AppID;
            return ((OnvifAppMgmt.AppManagement)(this)).GetAppsInfoAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.ActivateResponse> OnvifAppMgmt.AppManagement.ActivateAsync(OnvifAppMgmt.ActivateRequest request)
        {
            return base.Channel.ActivateAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.ActivateResponse> ActivateAsync(string AppID)
        {
            OnvifAppMgmt.ActivateRequest inValue = new OnvifAppMgmt.ActivateRequest();
            inValue.AppID = AppID;
            return ((OnvifAppMgmt.AppManagement)(this)).ActivateAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.DeactivateResponse> OnvifAppMgmt.AppManagement.DeactivateAsync(OnvifAppMgmt.DeactivateRequest request)
        {
            return base.Channel.DeactivateAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.DeactivateResponse> DeactivateAsync(string AppID)
        {
            OnvifAppMgmt.DeactivateRequest inValue = new OnvifAppMgmt.DeactivateRequest();
            inValue.AppID = AppID;
            return ((OnvifAppMgmt.AppManagement)(this)).DeactivateAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetServiceCapabilitiesResponse> OnvifAppMgmt.AppManagement.GetServiceCapabilitiesAsync(OnvifAppMgmt.GetServiceCapabilitiesRequest request)
        {
            return base.Channel.GetServiceCapabilitiesAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.GetServiceCapabilitiesResponse> GetServiceCapabilitiesAsync()
        {
            OnvifAppMgmt.GetServiceCapabilitiesRequest inValue = new OnvifAppMgmt.GetServiceCapabilitiesRequest();
            return ((OnvifAppMgmt.AppManagement)(this)).GetServiceCapabilitiesAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.InstallLicenseResponse> OnvifAppMgmt.AppManagement.InstallLicenseAsync(OnvifAppMgmt.InstallLicenseRequest request)
        {
            return base.Channel.InstallLicenseAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.InstallLicenseResponse> InstallLicenseAsync(string AppID, string License)
        {
            OnvifAppMgmt.InstallLicenseRequest inValue = new OnvifAppMgmt.InstallLicenseRequest();
            inValue.AppID = AppID;
            inValue.License = License;
            return ((OnvifAppMgmt.AppManagement)(this)).InstallLicenseAsync(inValue);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<OnvifAppMgmt.GetDeviceIdResponse> OnvifAppMgmt.AppManagement.GetDeviceIdAsync(OnvifAppMgmt.GetDeviceIdRequest request)
        {
            return base.Channel.GetDeviceIdAsync(request);
        }
        
        public System.Threading.Tasks.Task<OnvifAppMgmt.GetDeviceIdResponse> GetDeviceIdAsync()
        {
            OnvifAppMgmt.GetDeviceIdRequest inValue = new OnvifAppMgmt.GetDeviceIdRequest();
            return ((OnvifAppMgmt.AppManagement)(this)).GetDeviceIdAsync(inValue);
        }
        
        public virtual System.Threading.Tasks.Task OpenAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginOpen(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndOpen));
        }
        
        public virtual System.Threading.Tasks.Task CloseAsync()
        {
            return System.Threading.Tasks.Task.Factory.FromAsync(((System.ServiceModel.ICommunicationObject)(this)).BeginClose(null, null), new System.Action<System.IAsyncResult>(((System.ServiceModel.ICommunicationObject)(this)).EndClose));
        }
    }
}