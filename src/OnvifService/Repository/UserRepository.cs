using Microsoft.Extensions.Configuration;
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

        public Task<UserInfo> GetUser(string userName)
        {
            if (string.Compare(userName, _configuration.GetValue<string>("UserRepository:UserName"), true) == 0)
            {
                string password = _configuration.GetValue<string>("UserRepository:Password");
                return Task.FromResult(new UserInfo() { UserName = userName, Password = password });
            }

            return Task.FromResult((UserInfo)null);
        }
    }
}
