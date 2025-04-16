using System.Security.Claims;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public interface IAuthService
{
    Task<(bool Success, string Message, string? Token)> RegisterUserAsync(RegisterUserDto registerDto);
    Task<(bool Success, string Message, string? Token)> LoginAsync(LoginUserDto loginDto);
    Task<bool> ChangePasswordAsync(string userId, string currentPassword, string newPassword);
    Task<bool> RequestPasswordResetAsync(string email);
    Task<bool> ResetPasswordAsync(string userId, string token, string newPassword);
    Task<bool> PromoteToAdminAsync(string userId, string adminUserId);
    Task<bool> UpdateUserProfileAsync(string userId, string? userName, string? email);
    Task<IEnumerable<UserDto>> GetAllUsersAsync(string adminUserId);
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<(string Token, DateTime Expiry)> GenerateJwtTokenAsync(string userId, IList<string> roles, IList<Claim>? customClaims = null);
}