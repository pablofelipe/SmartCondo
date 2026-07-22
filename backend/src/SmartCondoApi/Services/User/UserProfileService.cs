using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.User
{
    public class UserProfileService(IUserProfileServiceDependencies _dependencies, ILogger<UserProfileService> _logger) : IUserProfileService
    {
        public async Task<UserProfileResponseDTO> Add(UserProfileCreateDTO userCreateDTO, AuthenticatedActor actor)
        {
            // Validate the CPF/CNPJ
            if (!ValidateRegistrationNumber(userCreateDTO.RegistrationNumber))
            {
                throw new InvalidRegistrationNumberIDException("Invalid CPF/CNPJ");
            }

            if (null == userCreateDTO.User)
            {
                throw new InvalidCredentialsException("No login was provided");
            }

            var userDb = await _dependencies.UserManager.FindByEmailAsync(userCreateDTO.User.Email);

            if (null != userDb)
            {
                throw new LoginAlreadyExistsException($"Login {userCreateDTO.User.Email} is already registered");
            }

            var context = _dependencies.Context;

            if (context.UserProfiles.Any(u => u.RegistrationNumber == userCreateDTO.RegistrationNumber))
            {
                throw new UserAlreadyExistsException($"CPF {userCreateDTO.RegistrationNumber} is already registered");
            }

            var userTypes = await _dependencies.Context.UserTypes.FirstOrDefaultAsync(ut => ut.Id == userCreateDTO.UserTypeId);

            if (null == userTypes)
            {
                throw new ArgumentException($"User type {userCreateDTO.UserTypeId} not found");
            }

            EnsureCallerCanRegister(actor.Role, userTypes.Name);

            var condo = await context.Condominiums.FirstOrDefaultAsync(c => c.Id == userCreateDTO.CondominiumId);

            if (null == condo)
            {
                if (IsSystemAdmin(userTypes.Name) == false)
                    throw new ArgumentException($"Condominium {userCreateDTO.CondominiumId} not found");

                SystemAdministrationValidations(userCreateDTO, userTypes, condo);
            }
            else
            {
                if (!condo.Enabled)
                {
                    throw new CondominiumDisabledException($"Condominium {condo.Name} is disabled. Contact the administrator for more information.");
                }

                var actorTenantId = actor.CondominiumId;
                if (!ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, condo.Id, p => p.CanRegisterUsers))
                {
                    throw new UnauthorizedAccessException("You are not authorized to register users in this condominium");
                }

                // A slot is occupied at registration, not at e-mail confirmation - see the authorization
                // evolution notes for Step 5. TryOccupyUserSlot reflects whatever was last read into `condo`;
                // the actual guarantee against a concurrent registration racing past MaxUsers comes from
                // OccupiedUserSlots being a concurrency token (see SmartCondoContext.OnModelCreating) - the
                // save below fails with DbUpdateConcurrencyException if another registration committed first,
                // which is translated back into the same exception this early check throws.
                condo.TryOccupyUserSlot();

                await ResidentValidations(userCreateDTO, userTypes, condo);

                NonResidentValidations(userCreateDTO, userTypes);
            }

            var user = new Models.User
            {
                UserName = userCreateDTO.User.Email,
                Email = userCreateDTO.User.Email,
                EmailConfirmed = false,
                LockoutEnabled = true,
                TwoFactorEnabled = true
            };

            user.PasswordHash = _dependencies.UserManager.PasswordHasher.HashPassword(user, userCreateDTO.User.Password);

            var userProfile = new UserProfile
            {
                Name = userCreateDTO.Name,
                Address = userCreateDTO.Address,
                Phone1 = userCreateDTO.Phone1,
                Phone2 = userCreateDTO.Phone2,
                UserTypeId = userCreateDTO.UserTypeId,
                RegistrationNumber = userCreateDTO.RegistrationNumber,
                CondominiumId = userCreateDTO.CondominiumId,
                TowerId = userCreateDTO.TowerId,
                FloorNumber = userCreateDTO.FloorId,
                Apartment = userCreateDTO.Apartment,
                ParkingSpaceNumber = userCreateDTO.ParkingSpaceNumber,
                User = user
            };


            await context.UserProfiles.AddAsync(userProfile);
            await context.Users.AddAsync(user);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new UsersExceedException("The maximum number of users allowed for this condominium has been reached. Contact the administrator for more information.");
            }

            await _dependencies.UserManager.UpdateSecurityStampAsync(user);
            await _dependencies.UserManager.UpdateNormalizedEmailAsync(user);
            await _dependencies.UserManager.UpdateNormalizedUserNameAsync(user);
            var token = await _dependencies.UserManager.GenerateEmailConfirmationTokenAsync(user);

            return new UserProfileResponseDTO()
            {
                Id = user.Id,
                Name = userCreateDTO.Name,
                Address = userCreateDTO.Address,
                UserTypeId = userCreateDTO.UserTypeId,
                RegistrationNumber = userCreateDTO.RegistrationNumber,
                CondominiumId = userCreateDTO.CondominiumId,
                TowerId = userCreateDTO.TowerId,
                FloorId = userCreateDTO.FloorId,
                Apartment = userCreateDTO.Apartment,
                ParkingSpaceNumber = userCreateDTO.ParkingSpaceNumber,
                Message = "User registered. Check your e-mail to confirm the registration.",
                Token = token
            };
        }

        private static void NonResidentValidations(UserProfileCreateDTO userCreateDTO, UserType currentUserType)
        {

            if (IsResident(currentUserType.Name))
                return;

            userCreateDTO.FloorId = null;
            userCreateDTO.Apartment = null;
            userCreateDTO.ParkingSpaceNumber = null;
            userCreateDTO.TowerId = null;
        }

        private static void SystemAdministrationValidations(UserProfileCreateDTO userCreateDTO, UserType currentUserType, Models.Condominium condo)
        {

            if (!IsSystemAdmin(currentUserType.Name))
                return;

            userCreateDTO.FloorId = null;
            userCreateDTO.Apartment = null;
            userCreateDTO.ParkingSpaceNumber = null;
            userCreateDTO.TowerId = null;
        }

        private static bool IsSystemAdmin(string userTypeName)
        {
            return string.Compare(userTypeName, "SystemAdministrator", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static void EnsureCallerCanRegister(string? callerRole, string targetRoleName)
        {
            if (string.IsNullOrEmpty(callerRole)
                || !RolePermissions.GetPermissions().TryGetValue(callerRole, out var callerPermissions))
            {
                throw new UnauthorizedAccessException($"Your role is not authorized to register a {targetRoleName}");
            }

            if (callerPermissions.BlockedUserTypes != null && callerPermissions.BlockedUserTypes.Contains(targetRoleName))
            {
                throw new UnauthorizedAccessException($"Your role is not authorized to register a {targetRoleName}");
            }

            if (callerPermissions.CanRegisterAnyUserType)
            {
                return;
            }

            if (callerPermissions.RegisterableUserTypes != null && callerPermissions.RegisterableUserTypes.Contains(targetRoleName))
            {
                return;
            }

            throw new UnauthorizedAccessException($"Your role is not authorized to register a {targetRoleName}");
        }

        private static bool IsResident(string userTypeName)
        {
            return string.Compare(userTypeName, "Resident", StringComparison.OrdinalIgnoreCase) == 0;
        }

        private async Task ResidentValidations(UserProfileCreateDTO userCreateDTO, UserType currentUserType, Models.Condominium condo)
        {

            if (!IsResident(currentUserType.Name))
                return;

            if (userCreateDTO.Apartment <= 0)
            {
                throw new InconsistentDataException($"Invalid apartment number.");
            }

            var context = _dependencies.Context;

            var tower = await context.Towers.FirstOrDefaultAsync(t => t.Id == userCreateDTO.TowerId && t.CondominiumId == condo.Id);

            if (null == tower)
            {
                throw new ArgumentException($"Tower {userCreateDTO.TowerId} not found");
            }

            if (userCreateDTO.FloorId > tower.FloorCount)
            {
                throw new InconsistentDataException($"Invalid floor number. Tower {tower.Name} has {tower.FloorCount} floor(s)");
            }

            // Rule: one apartment per parking space.
            var usersParkingSpaceNumber = (from profiles in context.UserProfiles
                                           join users in context.Users on profiles.Id equals users.Id
                                           where users.Enabled == true
                                           && profiles.CondominiumId == userCreateDTO.CondominiumId
                                           && profiles.ParkingSpaceNumber == userCreateDTO.ParkingSpaceNumber
                                           && profiles.Apartment != userCreateDTO.Apartment
                                           select new
                                           {
                                               User = profiles
                                           }).ToList();

            if (null != usersParkingSpaceNumber && usersParkingSpaceNumber.Count > 0)
            {
                _logger.LogDebug($"ParkingSpaceNumber no available trying to add new resident. Apartment: {userCreateDTO.Apartment}, ParkingSpaceNumber: {userCreateDTO.ParkingSpaceNumber}");
                foreach (var item in usersParkingSpaceNumber)
                {
                    _logger.LogDebug($"Apartment: {item.User.Apartment} of {item.User.Name} using the parkingspacenumber");
                }

                throw new ParkingSpaceNumberException($"The specified parking space number is already in use by another apartment. Contact the administrator for more information.");
            }
        }

        private static bool ValidateRegistrationNumber(string registrationNumber)
        {
            return new RegistrationNumberValidator().Verify(registrationNumber);
        }

        public async Task<UserProfileResponseDTO> Update(long userId, UserProfileUpdateDTO userUpdateDTO, AuthenticatedActor actor)
        {
            if (null == userUpdateDTO)
            {
                throw new InvalidCredentialsException("User data is required.");
            }

            var context = _dependencies.Context;

            // Look up the existing user in the database
            var userProfile = await context.UserProfiles
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (null == userProfile)
                throw new UserNotFoundException("User not found.");

            var isSelf = actor.Id == userProfile.Id;
            var actorTenantId = actor.CondominiumId;
            var hasAdminAuthority = ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, userProfile.CondominiumId, p => p.CanEditUsers);

            if (!isSelf && !hasAdminAuthority)
            {
                throw new UnauthorizedAccessException("You are not authorized to edit this profile");
            }

            var changesHousingAssignment = userUpdateDTO.CondominiumId.HasValue
                || userUpdateDTO.TowerId.HasValue
                || userUpdateDTO.FloorId.HasValue
                || userUpdateDTO.Apartment.HasValue;

            if (changesHousingAssignment && !hasAdminAuthority)
            {
                throw new UnauthorizedAccessException("Only an administrator can change housing assignment");
            }

            var originCondominiumId = userProfile.CondominiumId;
            var isCrossTenantMove = userUpdateDTO.CondominiumId.HasValue && userUpdateDTO.CondominiumId.Value != originCondominiumId;

            if (isCrossTenantMove)
            {
                // Moving a resident is itself an administrative act on the destination tenant, not just the
                // origin one - without this, an administrator of Condominium A could move someone into any
                // other condominium in the system, regardless of having any authority over it.
                var hasAdminAuthorityOverDestination = ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, userUpdateDTO.CondominiumId, p => p.CanEditUsers);

                if (!hasAdminAuthorityOverDestination)
                {
                    throw new UnauthorizedAccessException("You are not authorized to move users into this condominium");
                }
            }

            if (userUpdateDTO.Name != null) userProfile.Name = userUpdateDTO.Name;
            if (userUpdateDTO.Address != null) userProfile.Address = userUpdateDTO.Address;

            if (userUpdateDTO.CondominiumId.HasValue) userProfile.CondominiumId = userUpdateDTO.CondominiumId.Value;
            if (userUpdateDTO.TowerId.HasValue) userProfile.TowerId = userUpdateDTO.TowerId.Value;
            if (userUpdateDTO.FloorId.HasValue) userProfile.FloorNumber = userUpdateDTO.FloorId.Value;
            if (userUpdateDTO.Apartment.HasValue) userProfile.Apartment = userUpdateDTO.Apartment.Value;

            // Update the login data, if provided
            if (userUpdateDTO.User != null)
            {
                // never changes the email!
                //if (userUpdateDTO.User.Email != null) user.Email = userUpdateDTO.User.Email;
                // not the path to disable the account
                //if (userUpdateDTO.User.Enabled.HasValue) user.Enabled = userUpdateDTO.User.Enabled.Value;

                if (!string.IsNullOrEmpty(userUpdateDTO.User.Password))
                {
                    var user = await _dependencies.UserManager.FindByIdAsync(userId.ToString());

                    if (null != user)
                    {
                        user.PasswordHash = _dependencies.UserManager.PasswordHasher.HashPassword(user, userUpdateDTO.User.Password);
                    }
                }
            }

            if (isCrossTenantMove)
            {
                var destinationCondo = await context.Condominiums.FindAsync(userUpdateDTO.CondominiumId!.Value);

                if (destinationCondo == null)
                {
                    throw new ArgumentException($"Condominium {userUpdateDTO.CondominiumId} not found");
                }

                try
                {
                    destinationCondo.TryOccupyUserSlot();
                }
                catch (UsersExceedException)
                {
                    throw new UsersExceedException("The maximum number of users allowed for the destination condominium has been reached.");
                }

                if (originCondominiumId.HasValue)
                {
                    var originCondo = await context.Condominiums.FindAsync(originCondominiumId.Value);

                    originCondo?.ReleaseUserSlot();
                }
            }

            // Both condominium counters (when a cross-tenant move touches them) and the profile itself save
            // together - OccupiedUserSlots being a concurrency token means a race with another registration or
            // move on either condominium surfaces here as DbUpdateConcurrencyException, not as a silently
            // wrong count.
            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new UsersExceedException("The maximum number of users allowed for the destination condominium has been reached.");
            }

            return new UserProfileResponseDTO()
            {
                Name = userProfile.Name,
                Address = userProfile.Address,
                RegistrationNumber = userProfile.RegistrationNumber,
                CondominiumId = userProfile.CondominiumId,
                TowerId = userProfile.TowerId,
                FloorId = userProfile.FloorNumber,
                Apartment = userProfile.Apartment,
                Message = "User updated.",
            };
        }

        public async Task<UserProfileEditDTO> Get(long id, AuthenticatedActor actor)
        {
            var userProfile = await _dependencies.Context.UserProfiles.FindAsync(id);
            if (userProfile == null)
            {
                throw new UserNotFoundException("User profile not found.");
            }

            if (actor.Id != userProfile.Id)
            {
                var actorTenantId = actor.CondominiumId;
                if (!ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, userProfile.CondominiumId, p => p.CanViewUsers))
                {
                    throw new UnauthorizedAccessException("You are not authorized to view this profile");
                }
            }

            var user = await _dependencies.UserManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                throw new UserNotFoundException("User not found.");
            }

            var dto = new UserProfileEditDTO
            {
                Id = userProfile.Id,
                Name = userProfile.Name,
                Address = userProfile.Address,
                Phone1 = userProfile.Phone1,
                Phone2 = userProfile.Phone2,
                UserTypeId = userProfile.UserTypeId,
                RegistrationNumber = userProfile.RegistrationNumber,
                CondominiumId = userProfile.CondominiumId,
                TowerId = userProfile.TowerId,
                FloorId = userProfile.FloorNumber,
                Apartment = userProfile.Apartment,
                ParkingSpaceNumber = userProfile.ParkingSpaceNumber,
                Email = user.Email ?? string.Empty,
                Enabled = user.Enabled,
                PasswordLength = 8 // Real length is unknown; this is only for display
            };

            return dto;
        }

        public async Task Delete(long id, AuthenticatedActor actor)
        {
            if (id < 1)
            {
                throw new InconsistentDataException($"Invalid id number {id}.");
            }

            var userProfile = await _dependencies.Context.UserProfiles.FindAsync(id);
            if (userProfile == null)
            {
                throw new UserNotFoundException("User not found.");
            }

            var actorTenantId = actor.CondominiumId;
            if (!ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, userProfile.CondominiumId, p => p.CanEditUsers))
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this profile");
            }

            if (null != userProfile.User)
            {
                userProfile.User.Enabled = false;
            }
            else
            {
                _dependencies.Context.UserProfiles.Remove(userProfile);
            }

            await _dependencies.Context.SaveChangesAsync();

            // A slot is released on removal, independent of Enabled (which now only tracks e-mail
            // confirmation, never occupancy - see the Add/Update reservations). The floor guard keeps the
            // counter from going negative if this profile's slot was already released by an earlier call; it
            // does not by itself make repeated calls fully idempotent (see Step 5 notes). Kept as its own
            // save, separate from the one above: if a concurrent change to the same condominium makes this
            // release race (DbUpdateConcurrencyException), the profile is already disabled/removed either
            // way, and leaving the slot count slightly high until the next successful write touches it is the
            // safe direction - so that failure is logged, not rethrown.
            if (userProfile.CondominiumId.HasValue)
            {
                var condo = await _dependencies.Context.Condominiums.FindAsync(userProfile.CondominiumId.Value);

                if (condo != null)
                {
                    condo.ReleaseUserSlot();

                    try
                    {
                        await _dependencies.Context.SaveChangesAsync();
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        _logger.LogWarning("Could not release a user slot for condominium {CondominiumId} due to a concurrent update", userProfile.CondominiumId);
                    }
                }
            }
        }
    }
}
