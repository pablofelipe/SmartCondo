using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Crypto;

namespace SmartCondoApi.Services.Auth
{
    public interface IAuthDependencies
    {
        SmartCondoContext Context { get; }
        IConfiguration Configuration { get; }
        UserManager<Models.User> UserManager { get; }
        ICryptoService CryptoService { get; }
    }
}
