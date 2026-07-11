using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SmartCondoApi.Models
{
    public class UserProfile
    {
        public UserProfile()
        {
            Vehicles = [];
            SentMessages = [];
            UserMessages = [];
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; } // Chave primária
        public string Name { get; set; }
        public string Address { get; set; }

        public string Phone1 { get; set; }

        public string? Phone2 { get; set; }

        // Chave estrangeira para UserType
        public int UserTypeId { get; set; }

        // Propriedade de navegação para UserType
        public UserType UserType { get; set; }
        //CPF/CNPJ
        public string RegistrationNumber { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        public int? CondominiumId { get; set; }
        [JsonIgnore]
        public Condominium? Condominium { get; set; }

        public int? TowerId { get; set; }

        [JsonIgnore]
        public Tower? Tower { get; set; }

        public int? FloorNumber { get; set; }
        public int? Apartment { get; set; }
        public int? ParkingSpaceNumber { get; set; }

        [JsonIgnore]
        public ICollection<Vehicle> Vehicles { get; set; }

        // Relacionamento com Message (Sender)
        [InverseProperty("Sender")]
        [JsonIgnore]
        public ICollection<Message> SentMessages { get; set; }

        // Relacionamento com Message (Recipient)
        [JsonIgnore]
        public virtual ICollection<UserMessage> UserMessages { get; set; }
    }
}