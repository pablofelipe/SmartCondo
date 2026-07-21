using SmartCondoApi.Dto;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Message
{
    public interface IMessageService
    {
        Task<Models.Message> SendMessageAsync(MessageCreateDto messageDto, AuthenticatedActor actor);
        Task<IEnumerable<MessageDto>> GetReceivedMessagesAsync(AuthenticatedActor actor);
        Task<IEnumerable<MessageDto>> GetSentMessagesAsync(AuthenticatedActor actor);
        Task<MessageDto> GetMessageAsync(long messageId, AuthenticatedActor actor);
        Task MarkAsReadAsync(long messageId, AuthenticatedActor actor);
    }
}
