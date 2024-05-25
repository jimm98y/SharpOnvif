namespace SharpOnvifServer
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Password { get; set; }

        public UserInfo()
        { }

        public UserInfo(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }
    }
}
