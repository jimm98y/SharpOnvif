// SharpOnvif
// Copyright (C) 2026 Lukas Volf
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE 
// SOFTWARE.

using Microsoft.Extensions.Configuration;
using SharpOnvifCommon.Security;
using SharpOnvifServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnvifService.Repository
{
    /// <summary>
    /// Users backed by the "Users" section of appsettings.json:
    /// <code>
    /// "Users": [ { "UserName": "admin", "Password": "password" } ]
    /// </code>
    /// </summary>
    public class UserRepository : IUserRepository
    {
        /// <summary>
        /// Configuration section holding the users.
        /// </summary>
        public const string USERS_SECTION = "Users";

        private readonly IConfiguration _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public UserInfo GetUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            foreach (UserInfo user in GetUsers())
            {
                // Onvif user names are case sensitive
                if (string.Equals(user.UserName, userName, StringComparison.Ordinal))
                    return user;
            }

            return null;
        }

        public Task<UserInfo> GetUserAsync(string userName)
        {
            return Task.FromResult(GetUser(userName));
        }

        /// <summary>
        /// Get the user from the hashed user name.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="userName"></param>
        /// <param name="realm"></param>
        /// <returns></returns>
        public UserInfo GetUserByHash(string algorithm, string userName, string realm)
        {
            if (string.IsNullOrEmpty(userName))
                return null;

            // TODO: store the hashes in the users database and use them for lookups
            foreach (UserInfo user in GetUsers())
            {
                string hash = HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, user.UserName, realm);
                if (string.Equals(hash, userName, StringComparison.OrdinalIgnoreCase))
                    return user;
            }

            return null;
        }

        public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm)
        {
            return Task.FromResult(GetUserByHash(algorithm, userName, realm));
        }

        /// <summary>
        /// All configured users. Read on every lookup so that editing appsettings.json takes effect
        /// without a restart.
        /// </summary>
        /// <returns>Configured users, never null.</returns>
        public IReadOnlyList<UserInfo> GetUsers()
        {
            return _configuration.GetSection(USERS_SECTION).Get<UserInfo[]>() ?? Array.Empty<UserInfo>();
        }
    }
}
