namespace SmartCondoApi.Exceptions
{
    public class UserDisabledException : Exception
    {
        public UserDisabledException(string message) : base(message) { }
    }
}
