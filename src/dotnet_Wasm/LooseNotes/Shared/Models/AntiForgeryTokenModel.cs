namespace Xcaciv.LooseNotes.Wasm.Shared.Models;

/// <summary>
/// Model for carrying anti-CSRF tokens between client and server
/// </summary>
public class AntiForgeryTokenModel
{
    /// <summary>
    /// The anti-forgery token value
    /// </summary>
    public string Token { get; set; } = string.Empty;
    
    /// <summary>
    /// Token creation timestamp for expiration validation
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}