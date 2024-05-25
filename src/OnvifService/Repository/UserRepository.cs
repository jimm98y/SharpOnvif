using SharpOnvifServer;
using System.Threading.Tasks;

namespace OnvifService.Repository
{
    public class UserRepository : IUserRepository
    {
        public string UserName { get; set; } = "admin";
        public string Password { get; set; } = "password";

        public Task<UserInfo> GetUser(string userName)
        {
            if (string.Compare(userName, UserName, true) == 0)
            {
                return Task.FromResult(new UserInfo() { UserName = userName, Password = Password });
            }

            return Task.FromResult((UserInfo)null);
        }
    }
}
