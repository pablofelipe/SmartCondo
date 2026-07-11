namespace SmartCondoApi.Exceptions
{
    public class CondominiumNotFoundException : Exception
    {
        public CondominiumNotFoundException(string message) : base(message) { }
    }
}