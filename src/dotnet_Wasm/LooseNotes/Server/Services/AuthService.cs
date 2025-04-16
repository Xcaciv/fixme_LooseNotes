using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Xcaciv.LooseNotes.Wasm.Server.Data;
using Xcaciv.LooseNotes.Wasm.Server.Models;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        ApplicationDbContext dbContext,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, string? Token)> RegisterUserAsync(RegisterUserDto registerDto)
    {
        try
        {
            // Check if user with the same username already exists
            var userExists = await _userManager.FindByNameAsync(registerDto.UserName);
            if (userExists != null)
            {
                return (false, "Username already exists", null);
            }

            // Check if user with the same email already exists
            var emailExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (emailExists != null)
            {
                return (false, "Email already in use", null);
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                EmailConfirmed = true, // Auto-confirm for simplicity
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Add user to database
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("User registration failed: {Errors}", errors);
                return (false, errors, null);
            }

            // Ensure User role exists
            if (!await _roleManager.RoleExistsAsync("User"))
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            // Assign User role to the new user
            await _userManager.AddToRoleAsync(user, "User");

            // Generate JWT token
            var userRoles = await _userManager.GetRolesAsync(user);
            var (token, _) = await GenerateJwtTokenAsync(user.Id, userRoles);

            return (true, "User registered successfully", token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user registration");
            return (false, "An error occurred during registration", null);
        }
    }

    public async Task<(bool Success, string Message, string? Token)> LoginAsync(LoginUserDto loginDto)
    {
        try
        {
            // Find user by username or email
            var user = await _userManager.FindByNameAsync(loginDto.UserNameOrEmail) ??
                      await _userManager.FindByEmailAsync(loginDto.UserNameOrEmail);

            if (user == null)
            {
                return (false, "Invalid credentials", null);
            }

            // Verify password
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid)
            {
                return (false, "Invalid credentials", null);
            }

            // Update last login timestamp
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Generate JWT token
            var (token, _) = await GenerateJwtTokenAsync(user.Id, userRoles);

            return (true, "Login successful", token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return (false, "An error occurred during login", null);
        }
    }

    public async Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> RequestPasswordResetAsync(string email)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Return true even if user doesn't exist to prevent user enumeration
                return true;
            }

            // Generate password reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // Here you would typically send an email with the reset token/link
            // This is just a placeholder - you would implement email sending logic in a real application
            _logger.LogInformation("Password reset token generated for user {UserId}: {Token}", user.Id, token);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset for email {Email}", email);
            return false;
        }
    }

    public async Task<bool> ResetPasswordAsync(string userId, string token, string newPassword)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> PromoteToAdminAsync(string userId, string adminUserId)
    {
        try
        {
            // Verify the requesting user is an admin
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                return false;
            }

            // Find the user to promote
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Ensure Admin role exists
            if (!await _roleManager.RoleExistsAsync("Admin"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Add user to Admin role
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            
            if (result.Succeeded)
            {
                // Update IsAdmin flag
                user.IsAdmin = true;
                await _userManager.UpdateAsync(user);
            }

            return result.Succeeded;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user {UserId} to admin", userId);
            return false;
        }
    }

    public async Task<bool> UpdateUserProfileAsync(string userId, string? userName, string? email)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return false;
            }

            bool changes = false;

            // Update username if provided
            if (!string.IsNullOrEmpty(userName) && userName != user.UserName)
            {
                // Check if the new username is already taken
                var existingUser = await _userManager.FindByNameAsync(userName);
                if (existingUser != null && existingUser.Id != userId)
                {
                    return false;
                }

                user.UserName = userName;
                changes = true;
            }

            // Update email if provided
            if (!string.IsNullOrEmpty(email) && email != user.Email)
            {
                // Check if the new email is already taken
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null && existingUser.Id != userId)
                {
                    return false;
                }

                user.Email = email;
                user.EmailConfirmed = false; // Require confirmation for new email
                changes = true;
            }

            if (changes)
            {
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
            return false;
        }
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync(string adminUserId)
    {
        try
        {
            // Verify the requesting user is an admin
            var adminUser = await _userManager.FindByIdAsync(adminUserId);
            if (adminUser == null || !await _userManager.IsInRoleAsync(adminUser, "Admin"))
            {
                return Enumerable.Empty<UserDto>();
            }

            // Get all users with note and rating counts
            var users = await _dbContext.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    IsAdmin = u.IsAdmin,
                    NoteCount = u.Notes.Count,
                    RatingCount = u.Ratings.Count
                })
                .ToListAsync();

            return users;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return Enumerable.Empty<UserDto>();
        }
    }

    public async Task<UserDto?> GetUserByIdAsync(string userId)
    {
        try
        {
            return await _dbContext.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName ?? string.Empty,
                    Email = u.Email ?? string.Empty,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    IsAdmin = u.IsAdmin,
                    NoteCount = u.Notes.Count,
                    RatingCount = u.Ratings.Count
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID {UserId}", userId);
            return null;
        }
    }

    public async Task<(string Token, DateTime Expiry)> GenerateJwtTokenAsync(
        string userId, 
        IList<string> roles, 
        IList<Claim>? customClaims = null)
    {
        try
        {
            var secretKey = _configuration["JwtSettings:SecretKey"] ?? 
                throw new InvalidOperationException("JWT Secret Key is not configured");
            
            var issuer = _configuration["JwtSettings:Issuer"];
            var audience = _configuration["JwtSettings:Audience"];
            var tokenExpiryMinutes = int.Parse(_configuration["JwtSettings:ExpiryInMinutes"] ?? "60");

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Add custom claims if provided
            if (customClaims != null)
            {
                claims.AddRange(customClaims);
            }

            // Create token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return (tokenString, expires);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWT token for user {UserId}", userId);
            throw new ApplicationException("Error generating authentication token", ex);
        }
    }
}