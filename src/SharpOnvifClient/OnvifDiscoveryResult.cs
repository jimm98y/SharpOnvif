namespace SharpOnvifClient
{
    /// <summary>
    /// Discovered device.
    /// </summary>
    public class OnvifDiscoveryResult
    {
        /// <summary>
        /// Raw SOAP message for advanced processing.
        /// </summary>
        public string Raw { get; set; }

        /// <summary>
        /// Onvif addresses.
        /// </summary>
        public string[] Addresses { get; set; }

        /// <summary>
        /// Onvif scopes.
        /// </summary>
        public string[] Scopes { get; set; }

        /// <summary>
        /// Location - city.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Location - country.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Hardware.
        /// </summary>
        public string Hardware { get; set; }

        /// <summary>
        /// MAC address.
        /// </summary>
        public string MAC { get; set; }

        /// <summary>
        /// Device manufacturer.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Device name.
        /// </summary>
        public string Name { get; set; }
    }
}
