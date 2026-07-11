using Microsoft.AspNetCore.Identity;
using SmartCondoApi.Exceptions;

namespace SmartCondoApi.Services.Email
{
    public class EmailConfirmationService(UserManager<Models.User> _userManager) : IEmailConfirmationService
    {
        public async Task ConfirmEmail(string userId, string token)
        {
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                throw new UserNotFoundException("Usuário não encontrado.");
            }

            string decodedToken = Uri.UnescapeDataString(token);

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken);

            if (!result.Succeeded)
            {
                string descriptions = string.Join(", ", result.Errors
                    .Where(i => !string.IsNullOrEmpty(i.Description))
                    .Select(i => i.Description));

                throw new ConfirmEmailException(message: $"Erro ao confirmar o e-mail. Mensagem: {descriptions}");
            }

            user.Enabled = true;
            user.EmailConfirmed = true;

            await _userManager.UpdateAsync(user);
        }
    }
}
