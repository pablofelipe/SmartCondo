using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Crypto;

namespace SmartCondoApi.Services.User
{
    public class UserProfileServiceDependencies(SmartCondoContext context, UserManager<Models.User> userManager, ICryptoService cryptoService) : IUserProfileServiceDependencies
    {
        public SmartCondoContext Context { get; } = context;
        public UserManager<Models.User> UserManager { get; } = userManager;

        public ICryptoService CryptoService { get; } = cryptoService;
    }
}
