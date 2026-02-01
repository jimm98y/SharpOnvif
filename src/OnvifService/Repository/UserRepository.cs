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
using System.Threading.Tasks;

namespace OnvifService.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public UserInfo GetUser(string userName)
        {
            if (string.Compare(userName, _configuration.GetValue<string>("UserRepository:UserName"), false) == 0)
            {
                string password = _configuration.GetValue<string>("UserRepository:Password");
                return new UserInfo() { UserName = userName, Password = password };
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
            // TODO: store this in the users database and use it for lookups
            if (string.Compare(HttpDigestAuthentication.CreateUserNameHashRFC7616(algorithm, _configuration.GetValue<string>("UserRepository:UserName"), realm), userName, true) == 0)
            {
                string user = _configuration.GetValue<string>("UserRepository:UserName");
                string password = _configuration.GetValue<string>("UserRepository:Password");
                return new UserInfo() { UserName = user, Password = password };
            }

            return null;
        }

        public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm)
        {
            return Task.FromResult(GetUserByHash(algorithm, userName, realm));
        }
    }
}
