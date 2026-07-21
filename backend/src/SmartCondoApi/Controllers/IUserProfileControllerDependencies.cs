using SmartCondoApi.Infra;
using SmartCondoApi.Services.Email;
using SmartCondoApi.Services.LinkGenerator;
using SmartCondoApi.Services.User;

namespace SmartCondoApi.Controllers
{
    public interface IUserProfileControllerDependencies
    {
        IUserProfileService UserProfileService { get; }
        ILinkGeneratorService LinkGeneratorService { get; }
        IEmailService EmailService { get; }
        IEmailConfirmationService EmailConfirmationService { get; }
        ILogger<UserProfileController> Logger { get; }
        IAuthenticatedActorResolver ActorResolver { get; }
    }
}
