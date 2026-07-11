using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Models.Permissions;
using System.Security.Claims;

namespace SmartCondoApi.Models
{
    public class SmartCondoContext : IdentityDbContext<User, IdentityRole<long>, long>
    {
        public SmartCondoContext()
        {

        }

        public SmartCondoContext(DbContextOptions<SmartCondoContext> options)
            : base(options)
        {
        }

        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

        public DbSet<UserType> UserTypes { get; set; }

        public DbSet<UserProfile> UserProfiles { get; set; }

        public DbSet<Condominium> Condominiums { get; set; }

        public DbSet<Tower> Towers { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

        public DbSet<Message> Messages { get; set; }

        public DbSet<UserMessage> UserMessages { get; set; }

        public DbSet<WebSocketConnection> WebSocketConnections { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserProfile>()
                .HasOne(u => u.User)
                .WithOne(l => l.UserProfile)
                .HasForeignKey<User>(l => l.Id);

            modelBuilder.Entity<UserProfile>()
                .HasIndex(c => c.RegistrationNumber)
                .IsUnique();

            modelBuilder.Entity<UserProfile>()
                .HasOne(u => u.UserType)
                .WithMany()
                .HasForeignKey(u => u.UserTypeId)
                .IsRequired()
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserProfile>()
                .HasOne(e => e.Condominium)
                .WithMany(e => e.Users)
                .IsRequired(false);

            modelBuilder.Entity<UserProfile>()
                .HasOne(e => e.Tower)
                .WithMany(e => e.Users)
                .IsRequired(false);

            modelBuilder.Entity<UserProfile>()
                .HasMany(u => u.Vehicles)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .IsRequired();

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                      .UseIdentityAlwaysColumn();
            });

            // Configuração do relacionamento Sender (Remetente)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)          // Cada Message tem um Sender
                .WithMany(u => u.SentMessages)  // Cada UserProfile tem muitas SentMessages
                .HasForeignKey(m => m.SenderId) // Chave estrangeira em Message
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserMessage>()
                .HasKey(um => um.Id);

            modelBuilder.Entity<UserMessage>()
                .HasOne(um => um.Message)
                .WithMany(m => m.UserMessages)
                .HasForeignKey(um => um.MessageId);

            modelBuilder.Entity<UserMessage>()
                .HasOne(um => um.UserProfile)
                .WithMany()
                .HasForeignKey(um => um.UserProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany(u => u.SentMessages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
                .HasIndex(c => c.Email)
                .IsUnique();

            modelBuilder.Entity<Condominium>()
                .HasMany(c => c.Users)
                .WithOne(u => u.Condominium)
                .HasForeignKey(u => u.CondominiumId);


            modelBuilder.Entity<Vehicle>()
                .Property(u => u.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Vehicle>()
                .HasIndex(c => c.LicensePlate)
                .IsUnique();

            modelBuilder.Entity<Vehicle>()
                .HasOne(u => u.User)
                .WithMany(v => v.Vehicles)
                .HasForeignKey(f => f.UserId)
                .IsRequired();

            modelBuilder.Entity<UserType>().HasData(
                new UserType { Id = 1, Name = "SystemAdministrator", Description = "Administrador do sistema" },
                new UserType { Id = 2, Name = "CondominiumAdministrator", Description = "Síndico" },
                new UserType { Id = 3, Name = "Resident", Description = "Condômino" },
                new UserType { Id = 4, Name = "Janitor", Description = "Zelador" },
                new UserType { Id = 5, Name = "Doorman", Description = "Porteiro" },
                new UserType { Id = 6, Name = "Cleaner", Description = "Funcionário de Limpeza" },
                new UserType { Id = 7, Name = "Security", Description = "Segurança" },
                new UserType { Id = 8, Name = "ServiceProvider", Description = "Prestador de Serviços" },
                new UserType { Id = 9, Name = "ExternalProvider", Description = "Prestador Externo" },
                new UserType { Id = 10, Name = "DeliveryPerson", Description = "Funcionário de Entrega" },
                new UserType { Id = 11, Name = "Visitor", Description = "Visitante" },
                new UserType { Id = 12, Name = "CleaningManager", Description = "Administração de Limpeza" },
                new UserType { Id = 13, Name = "ResidentCommitteeMember", Description = "Conselheiro" },
                new UserType { Id = 14, Name = "AdministrativeAssistant", Description = "Funcionário da Administração" }
            );

            modelBuilder.Entity<WebSocketConnection>(entity =>
            {
                entity.HasKey(e => e.ConnectionId);
                entity.Property(e => e.ConnectionId).IsRequired();

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            base.OnModelCreating(modelBuilder);
        }

        public static async Task SeedPermissionsAsync(RoleManager<IdentityRole<long>> roleManager)
        {
            var rolePermissions = RolePermissions.GetPermissions();

            foreach (var rolePermission in rolePermissions)
            {
                await CreateOrUpdateRoleAsync(roleManager, rolePermission.Key, rolePermission.Value);
            }
        }

        private static async Task CreateOrUpdateRoleAsync(RoleManager<IdentityRole<long>> roleManager,
            string roleName, UserPermissionsDTO permission)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                role = new IdentityRole<long>(roleName);
                await roleManager.CreateAsync(role);
            }

            var existingClaims = await roleManager.GetClaimsAsync(role);
            foreach (var claim in existingClaims)
            {
                await roleManager.RemoveClaimAsync(role, claim);
            }

            foreach (var property in permission.GetType().GetProperties())
            {
                var value = property.GetValue(permission);

                if (value != null && bool.TryParse(value.ToString(), out bool bValue) && bValue)
                    await roleManager.AddClaimAsync(role, new Claim("Permission", property.Name));
            }

            if (null != permission.AllowedRecipientTypes)
            {
                foreach (var allowedType in permission.AllowedRecipientTypes)
                {
                    await roleManager.AddClaimAsync(role, new Claim("AllowedRecipient", allowedType));
                }
            }
        }
    }
}
