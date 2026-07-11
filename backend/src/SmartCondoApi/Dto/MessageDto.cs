using SmartCondoApi.Models;

namespace SmartCondoApi.Dto
{
    public class MessageDto
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public DateTime SentDate { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadDate { get; set; }
        public MessageScope Scope { get; set; }

        // Informações do remetente
        public long SenderId { get; set; }
        public string SenderName { get; set; }
        public string SenderType { get; set; }

        // Informações do destinatário (para mensagens individuais)
        public long? RecipientUserId { get; set; }
        public string RecipientName { get; set; }

        // Informações de localização
        public int CondominiumId { get; set; }
        public string CondominiumName { get; set; }
        public int? TowerId { get; set; }
        public string TowerName { get; set; }
        public int? FloorId { get; set; }
    }
}
