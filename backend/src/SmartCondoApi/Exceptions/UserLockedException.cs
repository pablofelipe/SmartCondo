namespace SmartCondoApi.Exceptions
{
    public class UserLockedException : Exception
    {
        public UserLockedException(string message) : base(message) { }
    }
}
