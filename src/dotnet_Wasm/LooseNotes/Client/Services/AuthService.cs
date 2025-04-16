using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Text.Json;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        HttpClient httpClient,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _logger = logger;
    }

    public bool IsAuthenticated { get; private set; }

    public async Task<ServiceResponse<string>> RegisterAsync(RegisterUserDto registerDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<string>(response);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = result.GetProperty("token").GetString();

            if (string.IsNullOrEmpty(token))
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Registration was successful, but the authentication token was not received"
                };
            }

            await _localStorage.SetItemAsStringAsync("authToken", token);
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(token);
            IsAuthenticated = true;

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Registration successful",
                Data = token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return new ServiceResponse<string>
            {
                Success = false,
                Message = "An unexpected error occurred during registration"
            };
        }
    }

    public async Task<ServiceResponse<string>> LoginAsync(LoginUserDto loginDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", loginDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<string>(response);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var token = result.GetProperty("token").GetString();

            if (string.IsNullOrEmpty(token))
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Login was successful, but the authentication token was not received"
                };
            }

            await _localStorage.SetItemAsStringAsync("authToken", token);
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(token);
            IsAuthenticated = true;

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Login successful",
                Data = token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return new ServiceResponse<string>
            {
                Success = false,
                Message = "An unexpected error occurred during login"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ChangePasswordAsync(ChangePasswordDto changePasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", changePasswordDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password changed successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while changing the password"
            };
        }
    }

    public async Task<ServiceResponse<bool>> RequestPasswordResetAsync(ResetPasswordRequestDto resetRequestDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password-request", resetRequestDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset request submitted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting password reset");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while requesting a password reset"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", resetPasswordDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Password reset successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while resetting the password"
            };
        }
    }

    public async Task<ServiceResponse<UserDto>> GetProfileAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/profile");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<UserDto>(response);
            }

            var user = await response.Content.ReadFromJsonAsync<UserDto>();
            
            return new ServiceResponse<UserDto>
            {
                Success = true,
                Data = user
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user profile");
            return new ServiceResponse<UserDto>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving user profile"
            };
        }
    }

    public async Task<ServiceResponse<bool>> UpdateProfileAsync(UpdateProfileDto profileDto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync("api/auth/profile", profileDto);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Profile updated successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while updating the profile"
            };
        }
    }

    public async Task<ServiceResponse<List<UserDto>>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/auth/users");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<UserDto>>(response);
            }

            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
            
            return new ServiceResponse<List<UserDto>>
            {
                Success = true,
                Data = users ?? new List<UserDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new ServiceResponse<List<UserDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving users"
            };
        }
    }

    public async Task<ServiceResponse<bool>> PromoteToAdminAsync(string userId)
    {
        try
        {
            var model = new PromoteToAdminDto { UserId = userId };
            var response = await _httpClient.PostAsJsonAsync("api/auth/promote-to-admin", model);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "User promoted to admin successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error promoting user to admin");
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while promoting user to admin"
            };
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        IsAuthenticated = false;
        _httpClient.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<bool> CheckAuthenticationStateAsync()
    {
        var authToken = await _localStorage.GetItemAsStringAsync("authToken");
        if (string.IsNullOrEmpty(authToken))
        {
            IsAuthenticated = false;
            return false;
        }

        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            IsAuthenticated = authState.User.Identity?.IsAuthenticated ?? false;

            // Verify the token is still valid
            if (IsAuthenticated)
            {
                var response = await _httpClient.GetAsync("api/auth/profile");
                if (!response.IsSuccessStatusCode)
                {
                    await LogoutAsync();
                    IsAuthenticated = false;
                    return false;
                }
            }

            return IsAuthenticated;
        }
        catch
        {
            await LogoutAsync();
            IsAuthenticated = false;
            return false;
        }
    }

    public async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        return await _authStateProvider.GetAuthenticationStateAsync();
    }

    private async Task<ServiceResponse<T>> HandleErrorResponse<T>(HttpResponseMessage response)
    {
        var errorMessage = "An error occurred";
        
        try
        {
            var content = await response.Content.ReadAsStringAsync();
            var errorDetails = JsonSerializer.Deserialize<JsonElement>(content);
            
            if (errorDetails.TryGetProperty("message", out var messageElement))
            {
                errorMessage = messageElement.GetString() ?? errorMessage;
            }
        }
        catch
        {
            // If we can't parse the error message, use the default
        }

        return new ServiceResponse<T>
        {
            Success = false,
            Message = errorMessage
        };
    }
}