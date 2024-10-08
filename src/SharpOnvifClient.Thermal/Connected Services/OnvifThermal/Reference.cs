﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SharpOnvifClient.Thermal
{
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ServiceModel.ServiceContractAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl", ConfigurationName="SharpOnvifClient.Thermal.ThermalPort")]
    public interface ThermalPort
    {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetServiceCapabilities", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="Capabilities")]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.Capabilities> GetServiceCapabilitiesAsync();
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetConfigurationOptions", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="ConfigurationOptions")]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.ConfigurationOptions> GetConfigurationOptionsAsync(string VideoSourceToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetConfiguration", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="Configuration")]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.Configuration> GetConfigurationAsync(string VideoSourceToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetConfigurations", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.GetConfigurationsResponse> GetConfigurationsAsync(SharpOnvifClient.Thermal.GetConfigurationsRequest request);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/SetConfiguration", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task SetConfigurationAsync(string VideoSourceToken, SharpOnvifClient.Thermal.Configuration Configuration);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetRadiometryConfigurationOptions", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="ConfigurationOptions")]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.RadiometryConfigurationOptions> GetRadiometryConfigurationOptionsAsync(string VideoSourceToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/GetRadiometryConfiguration", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        [return: System.ServiceModel.MessageParameterAttribute(Name="Configuration")]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.RadiometryConfiguration> GetRadiometryConfigurationAsync(string VideoSourceToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://www.onvif.org/ver10/thermal/wsdl/SetRadiometryConfiguration", ReplyAction="*")]
        [System.ServiceModel.XmlSerializerFormatAttribute(SupportFaults=true)]
        System.Threading.Tasks.Task SetRadiometryConfigurationAsync(string VideoSourceToken, SharpOnvifClient.Thermal.RadiometryConfiguration Configuration);
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class Capabilities
    {
        
        private System.Xml.XmlElement[] anyField;
        
        private bool radiometryField;
        
        private bool radiometryFieldSpecified;
        
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
        public bool Radiometry
        {
            get
            {
                return this.radiometryField;
            }
            set
            {
                this.radiometryField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool RadiometrySpecified
        {
            get
            {
                return this.radiometryFieldSpecified;
            }
            set
            {
                this.radiometryFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class RadiometryGlobalParameters
    {
        
        private float reflectedAmbientTemperatureField;
        
        private float emissivityField;
        
        private float distanceToObjectField;
        
        private float relativeHumidityField;
        
        private bool relativeHumidityFieldSpecified;
        
        private float atmosphericTemperatureField;
        
        private bool atmosphericTemperatureFieldSpecified;
        
        private float atmosphericTransmittanceField;
        
        private bool atmosphericTransmittanceFieldSpecified;
        
        private float extOpticsTemperatureField;
        
        private bool extOpticsTemperatureFieldSpecified;
        
        private float extOpticsTransmittanceField;
        
        private bool extOpticsTransmittanceFieldSpecified;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public float ReflectedAmbientTemperature
        {
            get
            {
                return this.reflectedAmbientTemperatureField;
            }
            set
            {
                this.reflectedAmbientTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public float Emissivity
        {
            get
            {
                return this.emissivityField;
            }
            set
            {
                this.emissivityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public float DistanceToObject
        {
            get
            {
                return this.distanceToObjectField;
            }
            set
            {
                this.distanceToObjectField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public float RelativeHumidity
        {
            get
            {
                return this.relativeHumidityField;
            }
            set
            {
                this.relativeHumidityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool RelativeHumiditySpecified
        {
            get
            {
                return this.relativeHumidityFieldSpecified;
            }
            set
            {
                this.relativeHumidityFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public float AtmosphericTemperature
        {
            get
            {
                return this.atmosphericTemperatureField;
            }
            set
            {
                this.atmosphericTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AtmosphericTemperatureSpecified
        {
            get
            {
                return this.atmosphericTemperatureFieldSpecified;
            }
            set
            {
                this.atmosphericTemperatureFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public float AtmosphericTransmittance
        {
            get
            {
                return this.atmosphericTransmittanceField;
            }
            set
            {
                this.atmosphericTransmittanceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool AtmosphericTransmittanceSpecified
        {
            get
            {
                return this.atmosphericTransmittanceFieldSpecified;
            }
            set
            {
                this.atmosphericTransmittanceFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public float ExtOpticsTemperature
        {
            get
            {
                return this.extOpticsTemperatureField;
            }
            set
            {
                this.extOpticsTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExtOpticsTemperatureSpecified
        {
            get
            {
                return this.extOpticsTemperatureFieldSpecified;
            }
            set
            {
                this.extOpticsTemperatureFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public float ExtOpticsTransmittance
        {
            get
            {
                return this.extOpticsTransmittanceField;
            }
            set
            {
                this.extOpticsTransmittanceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool ExtOpticsTransmittanceSpecified
        {
            get
            {
                return this.extOpticsTransmittanceFieldSpecified;
            }
            set
            {
                this.extOpticsTransmittanceFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=8)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class RadiometryConfiguration
    {
        
        private RadiometryGlobalParameters radiometryGlobalParametersField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public RadiometryGlobalParameters RadiometryGlobalParameters
        {
            get
            {
                return this.radiometryGlobalParametersField;
            }
            set
            {
                this.radiometryGlobalParametersField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/schema")]
    public partial class FloatRange
    {
        
        private float minField;
        
        private float maxField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public float Min
        {
            get
            {
                return this.minField;
            }
            set
            {
                this.minField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public float Max
        {
            get
            {
                return this.maxField;
            }
            set
            {
                this.maxField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class RadiometryGlobalParameterOptions
    {
        
        private FloatRange reflectedAmbientTemperatureField;
        
        private FloatRange emissivityField;
        
        private FloatRange distanceToObjectField;
        
        private FloatRange relativeHumidityField;
        
        private FloatRange atmosphericTemperatureField;
        
        private FloatRange atmosphericTransmittanceField;
        
        private FloatRange extOpticsTemperatureField;
        
        private FloatRange extOpticsTransmittanceField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public FloatRange ReflectedAmbientTemperature
        {
            get
            {
                return this.reflectedAmbientTemperatureField;
            }
            set
            {
                this.reflectedAmbientTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public FloatRange Emissivity
        {
            get
            {
                return this.emissivityField;
            }
            set
            {
                this.emissivityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public FloatRange DistanceToObject
        {
            get
            {
                return this.distanceToObjectField;
            }
            set
            {
                this.distanceToObjectField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public FloatRange RelativeHumidity
        {
            get
            {
                return this.relativeHumidityField;
            }
            set
            {
                this.relativeHumidityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=4)]
        public FloatRange AtmosphericTemperature
        {
            get
            {
                return this.atmosphericTemperatureField;
            }
            set
            {
                this.atmosphericTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=5)]
        public FloatRange AtmosphericTransmittance
        {
            get
            {
                return this.atmosphericTransmittanceField;
            }
            set
            {
                this.atmosphericTransmittanceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=6)]
        public FloatRange ExtOpticsTemperature
        {
            get
            {
                return this.extOpticsTemperatureField;
            }
            set
            {
                this.extOpticsTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=7)]
        public FloatRange ExtOpticsTransmittance
        {
            get
            {
                return this.extOpticsTransmittanceField;
            }
            set
            {
                this.extOpticsTransmittanceField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=8)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class RadiometryConfigurationOptions
    {
        
        private RadiometryGlobalParameterOptions radiometryGlobalParameterOptionsField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public RadiometryGlobalParameterOptions RadiometryGlobalParameterOptions
        {
            get
            {
                return this.radiometryGlobalParameterOptionsField;
            }
            set
            {
                this.radiometryGlobalParameterOptionsField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class Configurations
    {
        
        private Configuration configurationField;
        
        private System.Xml.XmlElement[] anyField;
        
        private string tokenField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public Configuration Configuration
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
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
        public string token
        {
            get
            {
                return this.tokenField;
            }
            set
            {
                this.tokenField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class Configuration
    {
        
        private ColorPalette colorPaletteField;
        
        private Polarity polarityField;
        
        private NUCTable nUCTableField;
        
        private Cooler coolerField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public ColorPalette ColorPalette
        {
            get
            {
                return this.colorPaletteField;
            }
            set
            {
                this.colorPaletteField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public Polarity Polarity
        {
            get
            {
                return this.polarityField;
            }
            set
            {
                this.polarityField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public NUCTable NUCTable
        {
            get
            {
                return this.nUCTableField;
            }
            set
            {
                this.nUCTableField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=3)]
        public Cooler Cooler
        {
            get
            {
                return this.coolerField;
            }
            set
            {
                this.coolerField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=4)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class ColorPalette
    {
        
        private string nameField;
        
        private System.Xml.XmlElement[] anyField;
        
        private string tokenField;
        
        private string typeField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
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
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
        public string token
        {
            get
            {
                return this.tokenField;
            }
            set
            {
                this.tokenField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public enum Polarity
    {
        
        /// <remarks/>
        WhiteHot,
        
        /// <remarks/>
        BlackHot,
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class NUCTable
    {
        
        private string nameField;
        
        private System.Xml.XmlElement[] anyField;
        
        private string tokenField;
        
        private float lowTemperatureField;
        
        private bool lowTemperatureFieldSpecified;
        
        private float highTemperatureField;
        
        private bool highTemperatureFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
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
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
        public string token
        {
            get
            {
                return this.tokenField;
            }
            set
            {
                this.tokenField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public float LowTemperature
        {
            get
            {
                return this.lowTemperatureField;
            }
            set
            {
                this.lowTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool LowTemperatureSpecified
        {
            get
            {
                return this.lowTemperatureFieldSpecified;
            }
            set
            {
                this.lowTemperatureFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public float HighTemperature
        {
            get
            {
                return this.highTemperatureField;
            }
            set
            {
                this.highTemperatureField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool HighTemperatureSpecified
        {
            get
            {
                return this.highTemperatureFieldSpecified;
            }
            set
            {
                this.highTemperatureFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class Cooler
    {
        
        private bool enabledField;
        
        private float runTimeField;
        
        private bool runTimeFieldSpecified;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public bool Enabled
        {
            get
            {
                return this.enabledField;
            }
            set
            {
                this.enabledField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=1)]
        public float RunTime
        {
            get
            {
                return this.runTimeField;
            }
            set
            {
                this.runTimeField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool RunTimeSpecified
        {
            get
            {
                return this.runTimeFieldSpecified;
            }
            set
            {
                this.runTimeFieldSpecified = value;
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class CoolerOptions
    {
        
        private bool enabledField;
        
        private bool enabledFieldSpecified;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=0)]
        public bool Enabled
        {
            get
            {
                return this.enabledField;
            }
            set
            {
                this.enabledField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool EnabledSpecified
        {
            get
            {
                return this.enabledFieldSpecified;
            }
            set
            {
                this.enabledFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute(Order=1)]
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
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl")]
    public partial class ConfigurationOptions
    {
        
        private ColorPalette[] colorPaletteField;
        
        private NUCTable[] nUCTableField;
        
        private CoolerOptions coolerOptionsField;
        
        private System.Xml.XmlElement[] anyField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ColorPalette", Order=0)]
        public ColorPalette[] ColorPalette
        {
            get
            {
                return this.colorPaletteField;
            }
            set
            {
                this.colorPaletteField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("NUCTable", Order=1)]
        public NUCTable[] NUCTable
        {
            get
            {
                return this.nUCTableField;
            }
            set
            {
                this.nUCTableField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Order=2)]
        public CoolerOptions CoolerOptions
        {
            get
            {
                return this.coolerOptionsField;
            }
            set
            {
                this.coolerOptionsField = value;
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
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetConfigurations", WrapperNamespace="http://www.onvif.org/ver10/thermal/wsdl", IsWrapped=true)]
    public partial class GetConfigurationsRequest
    {
        
        public GetConfigurationsRequest()
        {
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
    [System.ServiceModel.MessageContractAttribute(WrapperName="GetConfigurationsResponse", WrapperNamespace="http://www.onvif.org/ver10/thermal/wsdl", IsWrapped=true)]
    public partial class GetConfigurationsResponse
    {
        
        [System.ServiceModel.MessageBodyMemberAttribute(Namespace="http://www.onvif.org/ver10/thermal/wsdl", Order=0)]
        [System.Xml.Serialization.XmlElementAttribute("Configurations")]
        public SharpOnvifClient.Thermal.Configurations[] Configurations;
        
        public GetConfigurationsResponse()
        {
        }
        
        public GetConfigurationsResponse(SharpOnvifClient.Thermal.Configurations[] Configurations)
        {
            this.Configurations = Configurations;
        }
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    public interface ThermalPortChannel : SharpOnvifClient.Thermal.ThermalPort, System.ServiceModel.IClientChannel
    {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.Tools.ServiceModel.Svcutil", "2.2.0-preview1.23462.5")]
    public partial class ThermalPortClient : System.ServiceModel.ClientBase<SharpOnvifClient.Thermal.ThermalPort>, SharpOnvifClient.Thermal.ThermalPort
    {
        
        public ThermalPortClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.Capabilities> GetServiceCapabilitiesAsync()
        {
            return base.Channel.GetServiceCapabilitiesAsync();
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.ConfigurationOptions> GetConfigurationOptionsAsync(string VideoSourceToken)
        {
            return base.Channel.GetConfigurationOptionsAsync(VideoSourceToken);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.Configuration> GetConfigurationAsync(string VideoSourceToken)
        {
            return base.Channel.GetConfigurationAsync(VideoSourceToken);
        }
        
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        System.Threading.Tasks.Task<SharpOnvifClient.Thermal.GetConfigurationsResponse> SharpOnvifClient.Thermal.ThermalPort.GetConfigurationsAsync(SharpOnvifClient.Thermal.GetConfigurationsRequest request)
        {
            return base.Channel.GetConfigurationsAsync(request);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.GetConfigurationsResponse> GetConfigurationsAsync()
        {
            SharpOnvifClient.Thermal.GetConfigurationsRequest inValue = new SharpOnvifClient.Thermal.GetConfigurationsRequest();
            return ((SharpOnvifClient.Thermal.ThermalPort)(this)).GetConfigurationsAsync(inValue);
        }
        
        public System.Threading.Tasks.Task SetConfigurationAsync(string VideoSourceToken, SharpOnvifClient.Thermal.Configuration Configuration)
        {
            return base.Channel.SetConfigurationAsync(VideoSourceToken, Configuration);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.RadiometryConfigurationOptions> GetRadiometryConfigurationOptionsAsync(string VideoSourceToken)
        {
            return base.Channel.GetRadiometryConfigurationOptionsAsync(VideoSourceToken);
        }
        
        public System.Threading.Tasks.Task<SharpOnvifClient.Thermal.RadiometryConfiguration> GetRadiometryConfigurationAsync(string VideoSourceToken)
        {
            return base.Channel.GetRadiometryConfigurationAsync(VideoSourceToken);
        }
        
        public System.Threading.Tasks.Task SetRadiometryConfigurationAsync(string VideoSourceToken, SharpOnvifClient.Thermal.RadiometryConfiguration Configuration)
        {
            return base.Channel.SetRadiometryConfigurationAsync(VideoSourceToken, Configuration);
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
