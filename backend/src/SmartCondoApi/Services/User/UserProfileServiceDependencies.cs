using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.User
{
    public class UserProfileServiceDependencies(SmartCondoContext context, UserManager<Models.User> userManager) : IUserProfileServiceDependencies
    {
        public SmartCondoContext Context { get; } = context;
        public UserManager<Models.User> UserManager { get; } = userManager;
    }
}
