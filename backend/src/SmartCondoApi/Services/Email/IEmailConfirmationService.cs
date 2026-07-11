namespace SmartCondoApi.Services.Email
{
    public interface IEmailConfirmationService
    {
        Task ConfirmEmail(string userId, string token);
    }
}
