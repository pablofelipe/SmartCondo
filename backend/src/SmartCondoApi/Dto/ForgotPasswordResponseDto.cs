using SmartCondoApi.Models;

namespace SmartCondoApi.Dto
{
    public class ForgotPasswordResponseDto
    {
        public string Message { get; set; }
        public PasswordResetToken PasswordReset { get; internal set; }
    }
}
