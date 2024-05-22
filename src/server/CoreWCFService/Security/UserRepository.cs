using System.Threading.Tasks;

namespace CoreWCFService.Security
{
    public interface IUserRepository
    {
        public Task<bool> Authenticate(string username, string password, string nonce, string created);
    }

    public class UserRepository : IUserRepository
    {
        public string UserName { get; set; } = "admin";
        public string Password { get; set; } = "password";

        public Task<bool> Authenticate(string username, string password, string nonce, string created)
        {
            string hashedPassword = DigestHelpers.CreateHashedPassword(nonce, created, Password);
            System.DateTime createdDateTime;
            if (System.DateTime.TryParse(created, out createdDateTime))
            {
                if (System.DateTime.UtcNow.Subtract(createdDateTime).TotalMinutes < 5)
                {
                    if (hashedPassword.CompareTo(password) == 0)
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }
    }
}
