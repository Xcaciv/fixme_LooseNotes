using Xcaciv.LooseNotes.Wasm.Shared.DTOs;
using Microsoft.AspNetCore.Components.Authorization;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

public interface IAuthService
{
    Task<ServiceResponse<string>> RegisterAsync(RegisterUserDto registerDto);
    Task<ServiceResponse<string>> LoginAsync(LoginUserDto loginDto);
    Task<ServiceResponse<bool>> ChangePasswordAsync(ChangePasswordDto changePasswordDto);
    Task<ServiceResponse<bool>> RequestPasswordResetAsync(ResetPasswordRequestDto resetRequestDto);
    Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto);
    Task<ServiceResponse<UserDto>> GetProfileAsync();
    Task<ServiceResponse<bool>> UpdateProfileAsync(UpdateProfileDto profileDto);
    Task<ServiceResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ServiceResponse<bool>> PromoteToAdminAsync(string userId);
    Task LogoutAsync();
    bool IsAuthenticated { get; }
    Task<bool> CheckAuthenticationStateAsync();
    Task<AuthenticationState> GetAuthenticationStateAsync();
}