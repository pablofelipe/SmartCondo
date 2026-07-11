namespace SmartCondoApi.Dto
{
    public class ResetPasswordRequestDto
    {
        public string UserId { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
    }
}
