namespace SmartCondoApi.Dto
{
    public class MessageSendDTO
    {
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public string Content { get; set; }
    }
}
