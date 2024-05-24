using System.Threading.Tasks;
using SharpOnvifCommon.Security;

namespace SharpOnvifServer
{
    /// <summary>
    /// Basic implementation of the <see cref="IUserRepository"/> with Digest authentication.
    /// </summary>
    public abstract class UserRepositoryBase : IUserRepository
    {
        /// <summary>
        /// Maximum allowed time difference in between the client and the server in seconds.
        /// </summary>
        protected int MaxValidTimeDeltaInSeconds { get; set; } = 300;

        public Task<bool> Authenticate(string username, string password, string nonce, string created)
        {
            if (UserExists(username))
            {
                string hashedPassword = DigestHelpers.CreateHashedPassword(nonce, created, GetPassword(username));
                System.DateTime createdDateTime;
                if (System.DateTime.TryParse(created, out createdDateTime))
                {
                    if (MaxValidTimeDeltaInSeconds < 0 || System.DateTime.UtcNow.Subtract(createdDateTime).TotalSeconds < MaxValidTimeDeltaInSeconds)
                    {
                        if (hashedPassword.CompareTo(password) == 0)
                        {
                            return Task.FromResult(true);
                        }
                    }
                }
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Returns a password for the given user.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <returns>Password.</returns>
        protected abstract string GetPassword(string username);

        /// <summary>
        /// Returns true if the user exists, false otherwise.
        /// </summary>
        /// <param name="username">User name.</param>
        /// <returns>true if the user exists, false otherwise.</returns>
        protected abstract bool UserExists(string username);
    }
}
