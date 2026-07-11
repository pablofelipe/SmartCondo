namespace SmartCondoApi.Models
{
    public class UserMessage
    {
        public long Id { get; set; }
        public long MessageId { get; set; }
        public Message Message { get; set; }
        public long UserProfileId { get; set; }
        public UserProfile UserProfile { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
    }
}
