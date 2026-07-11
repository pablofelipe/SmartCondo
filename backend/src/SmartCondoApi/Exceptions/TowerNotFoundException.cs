namespace SmartCondoApi.Exceptions
{
    public class TowerNotFoundException : Exception
    {
        public TowerNotFoundException(string message) : base(message) { }
    }
}