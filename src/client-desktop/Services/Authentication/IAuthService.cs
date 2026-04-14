using Layla.Desktop.Models.Authentication;
using System.Threading.Tasks;

namespace Layla.Desktop.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(LoginRequest request);
        Task<AuthResult> RegisterAsync(RegisterRequest request);
    }
}
