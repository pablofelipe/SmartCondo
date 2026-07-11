namespace SmartCondoApi.Exceptions
{
    public class UserExpiredException : Exception
    {
        public UserExpiredException(string message) : base(message) { }
    }
}
