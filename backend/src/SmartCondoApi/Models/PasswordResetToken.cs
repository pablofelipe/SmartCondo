namespace SmartCondoApi.Models
{
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public DateTime Expires { get; set; }
        public long UserId { get; set; }
        public User User { get; set; }
    }
}
