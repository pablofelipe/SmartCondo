using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Auth
{
    public interface IAuthDependencies
    {
        SmartCondoContext Context { get; }
        IConfiguration Configuration { get; }
        UserManager<Models.User> UserManager { get; }
    }
}
