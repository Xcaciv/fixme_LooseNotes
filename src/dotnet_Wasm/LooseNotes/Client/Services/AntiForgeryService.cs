using Blazored.LocalStorage;
using System.Security.Cryptography;
using System.Text;
using Xcaciv.LooseNotes.Wasm.Shared.Models;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

/// <summary>
/// Service for protecting against CSRF attacks in Blazor WebAssembly
/// </summary>
public class AntiForgeryService : IAntiForgeryService
{
    private readonly ILocalStorageService _localStorage;
    private const string TokenKey = "anti-forgery-token";
    private const int TokenExpirationMinutes = 60;

    public AntiForgeryService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    /// <summary>
    /// Gets the current anti-forgery token, generating a new one if needed
    /// </summary>
    public async Task<string> GetTokenAsync()
    {
        var tokenModel = await GetOrCreateTokenModelAsync();
        return tokenModel.Token;
    }

    /// <summary>
    /// Gets the complete anti-forgery token model for HTTP requests
    /// </summary>
    public async Task<AntiForgeryTokenModel> GetTokenModelAsync()
    {
        return await GetOrCreateTokenModelAsync();
    }

    /// <summary>
    /// Regenerates a new anti-forgery token, invalidating the old one
    /// </summary>
    public async Task RegenerateTokenAsync()
    {
        var tokenModel = new AntiForgeryTokenModel
        {
            Token = GenerateRandomToken(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _localStorage.SetItemAsync(TokenKey, tokenModel);
    }

    private async Task<AntiForgeryTokenModel> GetOrCreateTokenModelAsync()
    {
        var tokenModel = await _localStorage.GetItemAsync<AntiForgeryTokenModel>(TokenKey);

        // If token doesn't exist or has expired, create a new one
        if (tokenModel == null || 
            DateTimeOffset.UtcNow.Subtract(tokenModel.CreatedAt).TotalMinutes >= TokenExpirationMinutes)
        {
            await RegenerateTokenAsync();
            tokenModel = await _localStorage.GetItemAsync<AntiForgeryTokenModel>(TokenKey);
        }

        return tokenModel!;
    }

    private string GenerateRandomToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}