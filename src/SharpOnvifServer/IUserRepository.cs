using System.Threading.Tasks;

namespace SharpOnvifServer
{
    /// <summary>
    /// User repository.
    /// </summary>
    public interface IUserRepository
    {
        public Task<bool> Authenticate(string username, string password, string nonce, string created);
    }
}
