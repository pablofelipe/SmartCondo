using System.Text.Json.Serialization;

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

        [JsonIgnore]
        public ICollection<Tower> Towers { get; set; }

        [JsonIgnore]
        public ICollection<UserProfile> Users { get; set; }

        [JsonIgnore]
        public ICollection<Message> Messages { get; set; }

    }
}
