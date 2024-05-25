using System.Threading.Tasks;

namespace SharpOnvifServer
{
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
        public Task<UserInfo> GetUser(string userName);
    }
}
