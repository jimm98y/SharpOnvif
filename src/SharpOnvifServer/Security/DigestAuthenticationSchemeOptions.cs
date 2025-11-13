using Microsoft.AspNetCore.Authentication;

namespace SharpOnvifServer
{
    public class DigestAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        /// <summary>
        /// Maximum allowed time difference in between the client and the server in seconds. Negative value will disable the timestamp verification.
        /// </summary>
        public double MaxValidTimeDeltaInSeconds { get; set; } = 300;

        /// <summary>
        /// Realm.
        /// </summary>
        public string Realm { get; set; } = "IP Camera";
    }
}
