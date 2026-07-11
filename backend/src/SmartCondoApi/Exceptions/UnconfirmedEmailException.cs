namespace SmartCondoApi.Exceptions
{
    public class UnconfirmedEmailException : Exception
    {
        public UnconfirmedEmailException(string message) : base(message) { }
    }
}
