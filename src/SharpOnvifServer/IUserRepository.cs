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

using SharpOnvifServer.Security;
using System.Threading.Tasks;

namespace SharpOnvifServer
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        /// <summary>
        /// Set to true when we are storing HA1 in the user database instead of plaintext password. Otherwise defaults to false.
        /// </summary>
        public bool IsPasswordAlreadyHashed { get; set; }

        public UserInfo()
        { }

        public UserInfo(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }
    }

    /// <summary>
    /// User repository.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Get user from the user name. Returns null if the user does not exist.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns><see cref="UserInfo"/> if the user exists, null otherwise.</returns>
        public UserInfo GetUser(string userName);

        /// <summary>
        /// Get user from the user name. Returns null if the user does not exist.
        /// </summary>
        /// <param name="userName"></param>
        /// <returns><see cref="UserInfo"/> if the user exists, null otherwise.</returns>
        public Task<UserInfo> GetUserAsync(string userName);

        /// <summary>
        /// Get the user from the hashed user name.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="userName"></param>
        /// <param name="realm"></param>
        /// <returns></returns>
        public UserInfo GetUserByHash(string algorithm, string userName, string realm);

        /// <summary>
        /// Get the user from the hashed user name.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="userName"></param>
        /// <param name="realm"></param>
        /// <returns></returns>
        public Task<UserInfo> GetUserByHashAsync(string algorithm, string userName, string realm);
    }

    public static class IUserRepositoryExtensions
    {
        /// <summary>
        /// Resolves the user based upon the HTTP Digest Authentication header. Supports user name as well as user hash.
        /// </summary>
        /// <param name="userRepository">User repository that implements <see cref="IUserRepository"/>.</param>
        /// <param name="webToken">HTTP Digest web token <see cref="WebDigestAuth"/>.</param>
        /// <returns><see cref="UserInfo"/>.</returns>
        public static UserInfo GetUser(this IUserRepository userRepository, WebDigestAuth webToken)
        {
            UserInfo user = null;

            if (!string.IsNullOrEmpty(webToken.UserHash) && webToken.UserHash.ToUpperInvariant() == "TRUE")
            {
                user = userRepository.GetUserByHash(webToken.Algorithm, webToken.UserName, webToken.Realm);
            }
            else
            {
                user = userRepository.GetUser(webToken.UserName);
            }

            return user;
        }

        /// <summary>
        /// Resolves the user based upon the HTTP Digest Authentication header. Supports user name as well as user hash.
        /// </summary>
        /// <param name="userRepository">User repository that implements <see cref="IUserRepository"/>.</param>
        /// <param name="webToken">HTTP Digest web token <see cref="WebDigestAuth"/>.</param>
        /// <returns>Awaitable <see cref="Task{UserInfo}"/>.</returns>
        public static async Task<UserInfo> GetUserAsync(this IUserRepository userRepository, WebDigestAuth webToken)
        {
            UserInfo user = null;

            if (!string.IsNullOrEmpty(webToken.UserHash) && webToken.UserHash.ToUpperInvariant() == "TRUE")
            {
                user = await userRepository.GetUserByHashAsync(webToken.Algorithm, webToken.UserName, webToken.Realm).ConfigureAwait(false);
            }
            else
            {
                user = await userRepository.GetUserAsync(webToken.UserName).ConfigureAwait(false);
            }

            return user;
        }
    }
}
