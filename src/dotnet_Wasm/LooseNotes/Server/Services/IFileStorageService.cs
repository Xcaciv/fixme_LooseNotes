namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType);
    Task<(Stream FileStream, string ContentType)?> GetFileAsync(string filePath);
    Task DeleteFileAsync(string filePath);
    string GetSafeFileName(string fileName);
    string GetFilePath(string fileName, string contentType);
}