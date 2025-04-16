namespace Xcaciv.LooseNotes.Client.Services;

/// <summary>
/// Interface for HTML sanitization services that prevent XSS attacks
/// </summary>
public interface IHtmlSanitizerService
{
    /// <summary>
    /// Sanitizes HTML content to prevent XSS attacks
    /// </summary>
    /// <param name="html">The HTML content to sanitize</param>
    /// <returns>Sanitized HTML content safe for rendering</returns>
    string Sanitize(string html);
}