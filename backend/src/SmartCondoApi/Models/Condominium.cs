using System.Text.Json.Serialization;
using SmartCondoApi.Exceptions;

namespace SmartCondoApi.Models
{
    public class Condominium
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int TowerCount { get; set; }
        public bool Enabled { get; set; }
        public int MaxUsers { get; set; }
        public int OccupiedUserSlots { get; private set; }

        [JsonIgnore]
        public ICollection<Tower> Towers { get; set; }

        [JsonIgnore]
        public ICollection<UserProfile> Users { get; set; }

        [JsonIgnore]
        public ICollection<Message> Messages { get; set; }

        public void TryOccupyUserSlot()
        {
            if (OccupiedUserSlots >= MaxUsers)
            {
                throw new UsersExceedException("O número máximo de usuários permitidos para este condomínio foi atingido. Entre em contato com o administrador para mais informações.");
            }

            OccupiedUserSlots += 1;
        }

        public void ReleaseUserSlot()
        {
            if (OccupiedUserSlots > 0)
            {
                OccupiedUserSlots -= 1;
            }
        }
    }
}
