using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Crypto;

namespace SmartCondoApi.Services.Auth
{
    public class AuthDependencies(SmartCondoContext context, UserManager<Models.User> userManager, IConfiguration configuration, ICryptoService cryptoService) : IAuthDependencies
    {
        public SmartCondoContext Context { get; } = context;
        public UserManager<Models.User> UserManager { get; } = userManager;
        public IConfiguration Configuration { get; } = configuration;
        public ICryptoService CryptoService { get; } = cryptoService;
    }
}
