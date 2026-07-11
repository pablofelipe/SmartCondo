using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.ForgotPassword
{
    public interface IForgotPasswordService
    {
        Task<ForgotPasswordResponseDto> SendResetLinkAsync(ForgotPasswordRequestDto request);
        Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
    public class ForgotPasswordService(SmartCondoContext _context, UserManager<Models.User> _userManager) : IForgotPasswordService
    {
        public async Task<ForgotPasswordResponseDto> SendResetLinkAsync(ForgotPasswordRequestDto request)
        {
            var userDb = await _userManager.FindByEmailAsync(request.Email);
            if (userDb == null)
            {
                throw new ArgumentException("Usuário não encontrado.");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(userDb);

            var passwordResetToken = new PasswordResetToken
            {
                Token = token,
                Expires = DateTime.UtcNow.AddHours(1), // 1 hora de expiração
                UserId = userDb.Id
            };

            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();

            return new ForgotPasswordResponseDto { Message = "Verifique seu e-mail para alterar a senha.", PasswordReset = passwordResetToken };
        }

        public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordRequestDto request)
        {
            string decodedToken = Uri.UnescapeDataString(request.Token);

            var passwordResetToken = await _context.PasswordResetTokens
                .Include(prt => prt.User)
                .FirstOrDefaultAsync(prt =>
                    prt.Token == decodedToken &&
                    prt.Expires > DateTime.UtcNow);

            if (passwordResetToken == null)
            {
                throw new ArgumentException("Token inválido ou expirado.");
            }

            var userDb = await _userManager.FindByIdAsync(request.UserId);
            if (userDb == null)
            {
                throw new ArgumentException("Usuário não encontrado.");
            }

            userDb.PasswordHash = _userManager.PasswordHasher.HashPassword(userDb, request.Password);

            _context.PasswordResetTokens.Remove(passwordResetToken);
            await _context.SaveChangesAsync();

            return new ResetPasswordResponseDto { Message = "Senha redefinida com sucesso." };
        }
    }
}
