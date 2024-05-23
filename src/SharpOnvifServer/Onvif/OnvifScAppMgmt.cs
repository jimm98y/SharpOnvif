﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OnvifScAppMgmt
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [CoreWCF.ServiceContractAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", ConfigurationName="OnvifScAppMgmt.AppManagement")]
    public interface AppManagement
    {
        
        // CODEGEN: Parameter 'AppID' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Uninstall")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        OnvifScAppMgmt.UninstallResponse Uninstall(OnvifScAppMgmt.UninstallRequest request);
        
        // CODEGEN: Parameter 'App' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetInstalledApps")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        [return: CoreWCF.MessageParameterAttribute(Name="App")]
        OnvifScAppMgmt.GetInstalledAppsResponse GetInstalledApps(OnvifScAppMgmt.GetInstalledAppsRequest request);
        
        // CODEGEN: Parameter 'Info' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetAppsInfo")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        [return: CoreWCF.MessageParameterAttribute(Name="Info")]
        OnvifScAppMgmt.GetAppsInfoResponse GetAppsInfo(OnvifScAppMgmt.GetAppsInfoRequest request);
        
        // CODEGEN: Parameter 'AppID' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Activate")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        OnvifScAppMgmt.ActivateResponse Activate(OnvifScAppMgmt.ActivateRequest request);
        
        // CODEGEN: Parameter 'AppID' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/Deactivate")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        OnvifScAppMgmt.DeactivateResponse Deactivate(OnvifScAppMgmt.DeactivateRequest request);
        
        // CODEGEN: Parameter 'Capabilities' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetServiceCapabilities")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        [return: CoreWCF.MessageParameterAttribute(Name="Capabilities")]
        OnvifScAppMgmt.GetServiceCapabilitiesResponse GetServiceCapabilities(OnvifScAppMgmt.GetServiceCapabilitiesRequest request);
        
        // CODEGEN: Parameter 'AppID' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/InstallLicense")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        OnvifScAppMgmt.InstallLicenseResponse InstallLicense(OnvifScAppMgmt.InstallLicenseRequest request);
        
        // CODEGEN: Parameter 'DeviceId' requires additional schema information that cannot be captured using the parameter mode. The specific attribute is 'System.Xml.Serialization.XmlElementAttribute'.
        [CoreWCF.OperationContractAttribute(Action="http://www.onvif.org/ver10/appmgmt/wsdl/GetDeviceId")]
        [CoreWCF.XmlSerializerFormatAttribute()]
        [return: CoreWCF.MessageParameterAttribute(Name="DeviceId")]
        OnvifScAppMgmt.GetDeviceIdResponse GetDeviceId(OnvifScAppMgmt.GetDeviceIdRequest request);
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="Uninstall", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class UninstallRequest
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="UninstallResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class UninstallResponse
    {
        
        public UninstallResponse()
        {
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class GetInstalledAppsResponseApp
    {
        
        private string nameField;
        
        private string appIDField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
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
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class Capabilities
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private string[] formatsSupportedField;
        
        private bool licensingField;
        
        private bool licensingFieldSpecified;
        
        private string uploadPathField;
        
        private string eventTopicPrefixField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
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
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl")]
    public partial class LicenseInfo
    {
        
        private string nameField;
        
        private System.DateTime validFromField;
        
        private bool validFromFieldSpecified;
        
        private System.DateTime validUntilField;
        
        private bool validUntilFieldSpecified;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
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
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.8.3928.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
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
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
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
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr
        {
            get
            {
                return this.anyAttrField;
            }
            set
            {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("svcutil", "4.8.3928.0")]
    [System.SerializableAttribute()]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetInstalledApps", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetInstalledAppsRequest
    {
        
        public GetInstalledAppsRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetInstalledAppsResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetInstalledAppsResponse
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("App", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public OnvifScAppMgmt.GetInstalledAppsResponseApp[] App;
        
        public GetInstalledAppsResponse()
        {
        }
        
        public GetInstalledAppsResponse(OnvifScAppMgmt.GetInstalledAppsResponseApp[] App)
        {
            this.App = App;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetAppsInfo", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetAppsInfoRequest
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetAppsInfoResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetAppsInfoResponse
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("Info", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public OnvifScAppMgmt.AppInfo[] Info;
        
        public GetAppsInfoResponse()
        {
        }
        
        public GetAppsInfoResponse(OnvifScAppMgmt.AppInfo[] Info)
        {
            this.Info = Info;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="Activate", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class ActivateRequest
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="ActivateResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class ActivateResponse
    {
        
        public ActivateResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="Deactivate", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class DeactivateRequest
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="DeactivateResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class DeactivateResponse
    {
        
        public DeactivateResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetServiceCapabilities", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetServiceCapabilitiesRequest
    {
        
        public GetServiceCapabilitiesRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetServiceCapabilitiesResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetServiceCapabilitiesResponse
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public OnvifScAppMgmt.Capabilities Capabilities;
        
        public GetServiceCapabilitiesResponse()
        {
        }
        
        public GetServiceCapabilitiesResponse(OnvifScAppMgmt.Capabilities Capabilities)
        {
            this.Capabilities = Capabilities;
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="InstallLicense", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class InstallLicenseRequest
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string AppID;
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=1)]
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
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="InstallLicenseResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class InstallLicenseResponse
    {
        
        public InstallLicenseResponse()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetDeviceId", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetDeviceIdRequest
    {
        
        public GetDeviceIdRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("CoreWCF", "4.0.0.0")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [CoreWCF.MessageContractAttribute(WrapperName="GetDeviceIdResponse", WrapperNamespace="http://www.onvif.org/ver10/appmgmt/wsdl", IsWrapped=true)]
    public partial class GetDeviceIdResponse
    {
        
        [CoreWCF.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/appmgmt/wsdl", Order=0)]
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
}