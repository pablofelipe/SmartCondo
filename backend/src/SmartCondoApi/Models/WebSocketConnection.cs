namespace SmartCondoApi.Models
{
    public class WebSocketConnection
    {
        public string ConnectionId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public DateTime ConnectedAt { get; set; }
        public bool IsActive { get; set; }

        // Navegação
        public virtual UserProfile User { get; set; } = null!;
    }
}
