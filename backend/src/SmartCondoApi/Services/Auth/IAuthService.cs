using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> Login([FromBody] Dictionary<string, string> body);
        AuthKeyDTO GetPublicKey();
    }
}
