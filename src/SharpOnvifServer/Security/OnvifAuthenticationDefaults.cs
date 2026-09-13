namespace SharpOnvifServer.Security
{
    /// <summary>Names the authentication scheme Onvif digest authentication registers under.</summary>
    public static class OnvifAuthenticationDefaults
    {
        /// <summary>
        /// The scheme <see cref="OnvifExtensions.AddOnvifDigestAuthentication(Microsoft.Extensions.DependencyInjection.IServiceCollection, DigestAuthenticationSchemeOptions)"/>
        /// registers. Onvif endpoints challenge against it when it is present.
        /// </summary>
        public const string AuthenticationScheme = "Digest";
    }
}
