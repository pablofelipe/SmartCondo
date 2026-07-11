using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Crypto;

namespace SmartCondoApi.Services.User
{
    public interface IUserProfileServiceDependencies
    {
        SmartCondoContext Context { get; }
        UserManager<Models.User> UserManager { get; }
        ICryptoService CryptoService { get; }
    }
}
