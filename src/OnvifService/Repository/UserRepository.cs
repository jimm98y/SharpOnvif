using SharpOnvifServer.Security;

namespace OnvifService.Repository
{
    public class UserRepository : UserRepositoryBase
    {
        public string UserName { get; set; } = "admin";
        public string Password { get; set; } = "password";

        protected override bool UserExists(string username)
        {
            return username == UserName;
        }

        protected override string GetPassword(string username)
        {
            if (username == UserName)
                return Password;

            throw new System.Exception("Unauthorized");
        }
    }
}
