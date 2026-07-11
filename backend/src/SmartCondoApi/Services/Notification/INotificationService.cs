namespace SmartCondoApi.Services.Notification
{
    public interface INotificationService
    {
        Task NotifyNewMessageAsync(Models.Message message);
    }
}
