namespace SmartCondoApi.Services.LinkGenerator
{
    public interface ILinkGeneratorService
    {
        string GenerateConfirmationLink(string action, string controller, object values);
    }
}
