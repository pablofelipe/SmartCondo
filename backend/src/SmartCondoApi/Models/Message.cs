using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartCondoApi.Models
{
    public enum MessageScope
    {
        Individual,
        Condominium,
        Tower,
        Floor
    }

    public partial class Message
    {
        public long Id { get; set; }
        public string Content { get; set; }
        public DateTime SentDate { get; set; }

        public MessageScope Scope { get; set; }
        //Relacionamento com Users(quem enviou).
        public long SenderId { get; set; }

        [ForeignKey("SenderId")]
        [JsonIgnore]
        public UserProfile Sender { get; set; }

        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public int? TowerId { get; set; }
        public Tower Tower { get; set; }

        public int? FloorId { get; set; }

        //Mensagem para outro usuário do sistema
        public long? RecipientUserId { get; set; }

        [JsonIgnore]
        public UserProfile RecipientUser { get; set; }

        [JsonIgnore]
        public virtual ICollection<UserMessage> UserMessages { get; set; }
    }
}
