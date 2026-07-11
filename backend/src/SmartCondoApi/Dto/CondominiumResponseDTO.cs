namespace SmartCondoApi.Dto
{
    public class CondominiumResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int TowerCount { get; set; }
        public int MaxUsers { get; set; }
        public bool Enabled { get; set; }
        public List<TowerResponseDTO> Towers { get; set; } = new List<TowerResponseDTO>();
    }
}