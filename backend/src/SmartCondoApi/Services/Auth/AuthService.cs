using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Auth
{
    public class AuthService(IAuthDependencies _dependencies) : IAuthService
    {
        public async Task<LoginResponseDTO> Login([FromBody] Dictionary<string, string> body)
        {
            if (!body.TryGetValue("user", out string userText) || string.IsNullOrEmpty(userText))
            {
                throw new InvalidCredentialsException("Email é obrigatório.");
            }

            if (!body.TryGetValue("secret", out string secretText) || string.IsNullOrEmpty(secretText))
            {
                throw new InvalidCredentialsException("Senha é obrigatória.");
            }

            var user = await _dependencies.UserManager.FindByEmailAsync(userText);

            if (null == user)
            {
                throw new UserNotFoundException("Usuário não encontrado.");
            }

            if (user.EmailConfirmed == false)
            {
                throw new UnconfirmedEmailException("O email não foi validado.");
            }

            if (user.Enabled == false)
            {
                throw new UserDisabledException("Usuário desabilitado.");
            }

            if (await _dependencies.UserManager.IsLockedOutAsync(user))
            {
                throw new UserLockedException("Conta bloqueada. Tente novamente mais tarde.");
            }

            //throw new UserExpiredException("Usuário expirado.");

            if (!await _dependencies.UserManager.CheckPasswordAsync(user, secretText))
            {
                throw new IncorrectPasswordException("Senha incorreta.");
            }

            var userProfile = await _dependencies.Context.UserProfiles
                .Include(u => u.Condominium)
                .FirstOrDefaultAsync(x => x.Id == user.Id);

            if (null == userProfile)
            {
                throw new UserNotFoundException("Perfil de usuário não encontrado.");
            }

            if (userProfile.CondominiumId.HasValue && userProfile.Condominium?.Enabled == false)
            {
                throw new CondominiumDisabledException("O condomínio deste usuário está desabilitado.");
            }

            var dbUserType = await _dependencies.Context.UserTypes.FirstOrDefaultAsync(us => us.Id == userProfile.UserTypeId);

            if (null == dbUserType)
            {
                throw new UserTypeNotFoundException("Tipo de Usuário não encontrado.");
            }

            string role = dbUserType.Name;

            return new LoginResponseDTO
            {
                Token = GenerateToken(user, dbUserType.Name),
                User = new UserProfileDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = userProfile.Name,
                    Role = role,
                    CondominiumId = userProfile.CondominiumId ?? 0,
                    TowerId = userProfile.TowerId ?? 0,
                    FloorId = userProfile.FloorNumber ?? 0,
                    Apartment = userProfile.Apartment ?? 0,
                    Permissions = GetPermissionsByRole(role)
                },
            };
        }

        private static UserPermissionsDTO GetPermissionsByRole(string role)
        {
            var rolePermissions = RolePermissions.GetPermissions();

            if (rolePermissions.TryGetValue(role, out var permissions))
            {
                return permissions;
            }

            return new UserPermissionsDTO();
        }

        private string GenerateToken(Models.User user, string userType)
        {
            return new TokenHandler(_dependencies.Configuration).Generate(user.Id.ToString(), user.Email, userType, DateTime.Now.AddDays(1));
        }

        public AuthKeyDTO GetPublicKey()
        {
            return null;
        }
    }
}
