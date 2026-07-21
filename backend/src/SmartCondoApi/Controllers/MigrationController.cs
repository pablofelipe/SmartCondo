using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class MigrationController : ControllerBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MigrationController> _logger;

        public MigrationController(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<MigrationController> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("migrate")]
        [AllowAnonymous]
        public async Task<ActionResult> Migrate()
        {
            _logger.LogInformation("Requisição de MIGRATE recebida");

            // Verificação de segurança
            var authKey = _configuration["MIGRATION_AUTH_KEY"];

            if (string.IsNullOrEmpty(authKey))
            {
                _logger.LogError("Chave de migração não configurada");
                return Unauthorized("Migration auth key not configured");
            }

            if (Request.Headers["X-Migration-Auth"] != authKey)
            {
                _logger.LogWarning("Tentativa de acesso não autorizado");
                return Unauthorized();
            }

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();

                await SeedDatabase(context, scope);

                _logger.LogInformation("Migração concluída com sucesso");
                return Ok("Database migrated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na migração");
                return StatusCode(500, $"Migration failed: {ex.Message}");
            }
        }

        private async Task SeedDatabase(
            SmartCondoContext context,
            IServiceScope scope)
        {
            _logger.LogInformation("Iniciando migração do banco de dados...");

            // Aplica as migrations
            await context.Database.MigrateAsync();
            _logger.LogInformation("Migrations aplicadas com sucesso");

            // Seed do admin
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var adminEmail = _configuration["ADMIN_EMAIL"] ?? throw new ArgumentNullException("ADMIN_EMAIL not configured");
            var adminPassword = _configuration["ADMIN_PASSWORD"] ?? throw new ArgumentNullException("ADMIN_PASSWORD not configured");

            if (!await context.Users.AnyAsync(u => u.Email == adminEmail))
            {
                _logger.LogInformation("Criando usuário admin...");

                var adminProfile = new UserProfile
                {
                    Name = "System Administrator",
                    UserTypeId = 1, // SystemAdministrator
                    Address = "Not informed",
                    Phone1 = "0000000000",
                    RegistrationNumber = "ADM001"
                };

                context.UserProfiles.Add(adminProfile);
                await context.SaveChangesAsync();

                var adminUser = new User
                {
                    Id = adminProfile.Id,
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Enabled = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Usuário admin criado com sucesso");
                }
                else
                {
                    // Rollback em caso de erro
                    context.UserProfiles.Remove(adminProfile);
                    await context.SaveChangesAsync();

                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Falha ao criar usuário admin: {errors}");
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }
            else
            {
                _logger.LogInformation("Usuário admin já existe");
            }
        }
    }
}