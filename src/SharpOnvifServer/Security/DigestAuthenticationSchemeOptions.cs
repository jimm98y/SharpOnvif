using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;

namespace SharpOnvifServer.Security
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
        /// Hashing algorithm(s). MD5 is the default when empty. Accepted values are: "MD5", "MD5-sess", "SHA-256", "SHA-256-sess", "SHA-512-256", "SHA-512-256-sess".
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
        public List<string> HashingAlgorithms { get; set; } = new List<string>() 
        { 
            "MD5",
            "MD5-sess",
            "SHA-256",
            "SHA-256-sess",
            "SHA-512-256",
            "SHA-512-256-sess",
        };

        /// <summary>
        /// Offered quality of protection levels. Valid values are "auth" and "auth-int".
        /// </summary>
        public List<string> AllowedQop { get; set; } = new List<string>() 
        {
            "auth",
            "auth-int",
        };

        /// <summary>
        /// Indicates whether the server should offer User hashing.
        /// </summary>
        public bool IsUserHashSupported { get; set; } = true;

        /// <summary>
        /// When true, forces the client to use the nonce from the last response. 
        /// This breaks pipelining of the client requests, but it lets the server enforce single-use nonces.
        /// </summary>
        //public bool IsUsingNextNonce { get; set; } = false;
    }
}
