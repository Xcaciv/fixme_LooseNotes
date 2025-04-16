using Xcaciv.LooseNotes.Wasm.Shared.Models;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

/// <summary>
/// Interface for the anti-CSRF token service to protect against Cross-Site Request Forgery
/// </summary>
public interface IAntiForgeryService
{
    /// <summary>
    /// Gets the current anti-forgery token, generating a new one if needed
    /// </summary>
    Task<string> GetTokenAsync();
    
    /// <summary>
    /// Gets the complete anti-forgery token model for HTTP requests
    /// </summary>
    Task<AntiForgeryTokenModel> GetTokenModelAsync();
    
    /// <summary>
    /// Regenerates a new anti-forgery token, invalidating the old one
    /// </summary>
    Task RegenerateTokenAsync();
}