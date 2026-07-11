namespace SmartCondoApi.Dto
{
    public class TowerResponseDTO
    {
        public int Id { get; set; }
        public int Number { get; set; }
        public string Name { get; set; }
        public int CondominiumId { get; set; }
        public int FloorCount { get; set; }
    }
}