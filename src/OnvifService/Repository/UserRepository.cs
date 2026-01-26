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

        public Task<UserInfo> GetUser(string userName)
        {
            if (string.Compare(userName, _configuration.GetValue<string>("UserRepository:UserName"), true) == 0)
            {
                string password = _configuration.GetValue<string>("UserRepository:Password");
                return Task.FromResult(new UserInfo() { UserName = userName, Password = password });
            }

            return Task.FromResult((UserInfo)null);
        }

        /// <summary>
        /// Get the user from the hashed user name.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="userName"></param>
        /// <param name="realm"></param>
        /// <returns></returns>
        public Task<UserInfo> GetUserByHash(string algorithm, string userName, string realm)
        {
            // TODO: store this in the users database and use it for lookups
            if (string.Compare(DigestAuthentication.CreateUserNameHashRFC7616(algorithm, _configuration.GetValue<string>("UserRepository:UserName"), realm), userName, true) == 0)
            {
                string user = _configuration.GetValue<string>("UserRepository:UserName");
                string password = _configuration.GetValue<string>("UserRepository:Password");
                return Task.FromResult(new UserInfo() { UserName = user, Password = password });
            }

            return Task.FromResult((UserInfo)null);
        }
    }
}
