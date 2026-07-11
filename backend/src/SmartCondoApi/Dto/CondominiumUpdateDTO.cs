namespace SmartCondoApi.Dto
{
    public class CondominiumUpdateDTO
    {
        public string? Name { get; set; }
        public string? Address { get; set; }
        public int? TowerCount { get; set; }
        public int? MaxUsers { get; set; }
        public bool? Enabled { get; set; }

        public List<TowerUpdateDTO>? Towers { get; set; }
    }
}