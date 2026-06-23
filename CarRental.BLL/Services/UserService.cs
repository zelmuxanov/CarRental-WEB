using AutoMapper;
using CarRental.BLL.Interfaces.Services;
using CarRental.BLL.DTOs.User;
using CarRental.Domain.Entities;
using CarRental.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarRental.BLL.Services;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IEmailService _emailService;

    public UserService(
        UserManager<User> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        IMapper mapper,
        ILogger<UserService> logger,
        IEmailService emailService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
    }

    #region Public Methods (for users)
    
    public async Task<IdentityResult> CreateUserAsync(RegisterDto registerDto)
    {
        try
        {
            _logger.LogInformation("Creating user with email: {Email}", registerDto.Email);

            if (!registerDto.BirthDate.HasValue || !registerDto.DrivingExperience.HasValue)
                return IdentityResult.Failed(new IdentityError { 
                    Description = "Дата рождения и стаж вождения обязательны" 
                });

            // возраст 23+, стаж 2+
            var age = DateTime.Now.Year - registerDto.BirthDate.Value.Year;
            if (registerDto.BirthDate.Value > DateTime.Now.AddYears(-age)) age--;
            
            if (age < 23)
                return IdentityResult.Failed(new IdentityError { 
                    Description = "Минимальный возраст для регистрации - 23 года" 
                });

            if (registerDto.DrivingExperience < 2)
                return IdentityResult.Failed(new IdentityError { 
                    Description = "Минимальный стаж вождения - 2 года" 
                });
 
            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                MiddleName = registerDto.MiddleName,
                UserName = registerDto.Email,
                Email = registerDto.Email,
                PhoneNumber = registerDto.PhoneNumber,
                BirthDate = registerDto.BirthDate.Value,
                DrivingExperience = registerDto.DrivingExperience.Value,
                Status = UserStatus.Pending, 
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            
            if (result.Succeeded)
            {
                // Автоматически назначаем роль "User"
                await _userManager.AddToRoleAsync(user, "User");
                await _emailService.SendNewUserNotificationAsync(user);
                _logger.LogInformation("User created successfully: {UserId}", user.Id);
            }
            else
            {
                _logger.LogWarning("User creation failed: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user with email: {Email}", registerDto.Email);
            throw;
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<bool> UpdateUserProfileAsync(Guid id, UserProfileDto userProfileDto)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        user.FirstName = userProfileDto.FirstName;
        user.LastName = userProfileDto.LastName;
        user.MiddleName = userProfileDto.MiddleName;
        user.PhoneNumber = userProfileDto.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ValidateUserAgeAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        var age = DateTime.Now.Year - user.BirthDate.Year;
        if (user.BirthDate > DateTime.Now.AddYears(-age)) age--;
        
        return age >= 23;
    }

    public async Task<bool> ValidateDrivingExperienceAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.DrivingExperience >= 2;
    }

    public async Task<IdentityResult> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return IdentityResult.Failed(new IdentityError { Description = "Пользователь не найден" });

        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<UserVerificationInfoDto> GetUserVerificationInfoAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) 
            return new UserVerificationInfoDto { Status = "Не найден" };

        return new UserVerificationInfoDto
        {
            IsVerified = user.Status == UserStatus.Active,
            Status = user.Status.ToString(),
            VerificationDate = user.UpdatedAt
        };
    }

    public async Task<UserDocumentsDto> GetUserDocumentsAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return new UserDocumentsDto();

        return new UserDocumentsDto
        {
            DriverLicenseNumber = user.DriverLicenseNumber,
            DriverLicenseExpiry = user.DriverLicenseExpiry,
            HasDriverLicense = !string.IsNullOrEmpty(user.DriverLicenseNumber),
            DocumentsVerifiedAt = user.UpdatedAt,
            VerificationStatus = user.Status.ToString()
        };
    }

    public async Task<bool> UpdateUserDocumentsAsync(Guid userId, UserDocumentsDto documentsDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        user.DriverLicenseNumber = documentsDto.DriverLicenseNumber;
        user.DriverLicenseExpiry = documentsDto.DriverLicenseExpiry;
        
        // при загрузке новых документов аккаунт переходит в статус "На проверке"
        if (!string.IsNullOrEmpty(documentsDto.DriverLicenseNumber) && user.Status == UserStatus.Active)
        {
            user.Status = UserStatus.Pending;
        }
        
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }
    public async Task<User?> GetUserEntityByIdAsync(Guid id)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<string?> GetUserEmailAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        return user?.Email;
    }

    #endregion

    #region Admin Methods

    public async Task<(IEnumerable<UserDto> Users, int TotalCount)> GetUsersWithPaginationAsync(
        int page, int pageSize, UserStatus? status = null)
    {
        var query = _userManager.Users.AsQueryable();
        
        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }
        
        var totalCount = await query.CountAsync();
        
        var users = await query
            .OrderByDescending(u => u.RegistrationDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (_mapper.Map<IEnumerable<UserDto>>(users), totalCount);
    }

    public async Task<bool> ActivateUserAsync(Guid userId)
    {
        try
        {
            _logger.LogInformation("Activating user: {UserId}", userId);
            
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            if (user.Status != UserStatus.Pending)
            {
                _logger.LogWarning("Cannot activate user {UserId} from status {Status}", 
                    userId, user.Status);
                return false;
            }

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User activated successfully: {UserId}", userId);
                
                // TODO: Отправить email пользователю об активации
                // await SendActivationEmailAsync(user.Email);
            }
            else
            {
                _logger.LogError("Failed to activate user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> BlockUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.Status == UserStatus.Blocked)
            return false;

        // Можно блокировать только активных пользователей
        if (user.Status != UserStatus.Active)
            return false;

        user.Status = UserStatus.Blocked;
        user.UpdatedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UnblockUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.Status != UserStatus.Blocked)
            return false;

        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> UpdateUserStatusAsync(Guid userId, UserStatus status)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return false;

        // Валидация переходов статусов
        switch (status)
        {
            case UserStatus.Active:
                if (user.Status != UserStatus.Pending) return false;
                break;
            case UserStatus.Blocked:
                if (user.Status != UserStatus.Active) return false;
                break;
            case UserStatus.Rejected:
                if (user.Status != UserStatus.Pending) return false;
                break;
        }

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;
        
        var result = await _userManager.UpdateAsync(user);
        return result.Succeeded;
    }


    #endregion
}