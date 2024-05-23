using System.Threading.Tasks;

namespace SharpOnvifServer.Security
{
    public abstract class UserRepositoryBase : IUserRepository
    {
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

        protected abstract string GetPassword(string username);
        protected abstract bool UserExists(string username);
    }
}
