namespace SharpOnvifServer.Security
{
    public class SoapDigestAuth
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Nonce { get; set; }
        public string Created { get; set; }

        public SoapDigestAuth(string username, string password, string nonce, string created)
        {
            UserName = username;
            Password = password;
            Nonce = nonce;
            Created = created;
        }
    }
}
