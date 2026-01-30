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
}
