using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Message
{
    public interface IMessageService
    {
        Task<Models.Message> SendMessageAsync(MessageCreateDto messageDto, long senderId);
        Task<IEnumerable<MessageDto>> GetReceivedMessagesAsync(long userId);
        Task<IEnumerable<MessageDto>> GetSentMessagesAsync(long userId);
        Task<MessageDto> GetMessageAsync(long messageId, long userId);
        Task MarkAsReadAsync(long messageId, long userId);
    }
}
