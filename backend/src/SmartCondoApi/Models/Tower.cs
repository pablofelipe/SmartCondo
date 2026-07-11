namespace SmartCondoApi.Models
{
    public partial class Tower
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public int CondominiumId { get; set; }
        public Condominium Condominium { get; set; }

        public int FloorCount { get; set; }

        public ICollection<UserProfile> Users { get; set; }

        public ICollection<Message> Messages { get; set; }
    }
}
