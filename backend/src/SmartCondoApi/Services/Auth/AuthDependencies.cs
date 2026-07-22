using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Auth
{
    public class AuthDependencies(SmartCondoContext context, UserManager<Models.User> userManager, IConfiguration configuration) : IAuthDependencies
    {
        public SmartCondoContext Context { get; } = context;
        public UserManager<Models.User> UserManager { get; } = userManager;
        public IConfiguration Configuration { get; } = configuration;
    }
}
