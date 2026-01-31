using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;

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

        /// <summary>
        /// Hashing algorithm(s). MD5 is the default when empty. Accepted values are: "MD5", "SHA-256", "SHA-512-256".
        /// 
        /// Onvif specific: According to the RFC 7616 we should add the algorithms in the order of server preference, starting
        ///  with the most preferred one. When the client receives the first challenge, it should use the first one it supports. 
        ///  
        /// However, in Onvif core specification section 5.9.2.2 we can see the challenges are listed with MD5 first. The current 
        ///  behavior in ODM is that when we offer SHA-256 as the first one, ODM fails to connect. When we offer MD5 as the first one,
        ///  ODM connects using MD5 which seems to be the only one supported.
        ///  
        /// WWW-Authenticate challenges will be generated in the same order they are listed here.
        /// </summary>
        public List<string> HashingAlgorithms { get; set; } = new List<string>() { "MD5", "SHA-256", "SHA-512-256" };

        /// <summary>
        /// Allowed qop.
        /// </summary>
        public List<string> AllowedQop { get; set; } = new List<string>() { "auth", "auth-int" };
    }
}
