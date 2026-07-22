using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.User
{
    public interface IUserProfileServiceDependencies
    {
        SmartCondoContext Context { get; }
        UserManager<Models.User> UserManager { get; }
    }
}
