using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Xcaciv.LooseNotes.Wasm.Server.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<FileStorageService> _logger;
    private readonly string _uploadsFolder;

    public FileStorageService(IWebHostEnvironment environment, ILogger<FileStorageService> logger)
    {
        _environment = environment;
        _logger = logger;
        _uploadsFolder = Path.Combine(_environment.ContentRootPath, "uploads");
        
        // Ensure uploads folder exists
        if (!Directory.Exists(_uploadsFolder))
        {
            Directory.CreateDirectory(_uploadsFolder);
        }
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType)
    {
        try
        {
            // Create a safe file name
            string safeFileName = GetSafeFileName(fileName);
            
            // Get the full path where the file will be saved
            string filePath = GetFilePath(safeFileName, contentType);
            
            // Create directory if it doesn't exist
            string directoryPath = Path.GetDirectoryName(filePath) ?? _uploadsFolder;
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            
            // Save the file
            using (var fileStored = new FileStream(filePath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fileStored);
            }
            
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file {FileName}", fileName);
            throw new IOException($"Error saving file {fileName}", ex);
        }
    }

    public async Task<(Stream FileStream, string ContentType)?> GetFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return null;
            }
            
            // Validate path is within uploads folder to prevent path traversal
            var fullPath = Path.GetFullPath(filePath);
            var normalizedUploadFolder = Path.GetFullPath(_uploadsFolder);
            
            if (!fullPath.StartsWith(normalizedUploadFolder, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted path traversal: {FilePath}", filePath);
                return null;
            }
            
            // Get content type based on file extension
            string contentType = GetContentType(Path.GetExtension(filePath));
            
            // Open file stream for reading
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return (fileStream, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file {FilePath}", filePath);
            return null;
        }
    }

    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                // Validate path is within uploads folder to prevent path traversal
                var fullPath = Path.GetFullPath(filePath);
                var normalizedUploadFolder = Path.GetFullPath(_uploadsFolder);
                
                if (!fullPath.StartsWith(normalizedUploadFolder, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Attempted path traversal in delete: {FilePath}", filePath);
                    return;
                }
                
                File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FilePath}", filePath);
        }
    }

    public string GetSafeFileName(string fileName)
    {
        // Replace invalid filename characters and limit length
        string safeFileName = Regex.Replace(fileName, @"[^\w\s\.-]", "-");
        
        // Generate a unique file name by appending a timestamp and random hash
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string randomHash = ComputeSha256Hash(fileName + timestamp).Substring(0, 8);
        
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(safeFileName);
        string extension = Path.GetExtension(safeFileName);
        
        if (fileNameWithoutExtension.Length > 50)
        {
            fileNameWithoutExtension = fileNameWithoutExtension.Substring(0, 50);
        }
        
        return $"{fileNameWithoutExtension}-{timestamp}-{randomHash}{extension}";
    }

    public string GetFilePath(string fileName, string contentType)
    {
        // Create a folder structure based on content type
        string folder = contentType.Split('/')[0];
        
        // Sanitize folder name
        folder = Regex.Replace(folder, @"[^\w]", "");
        
        return Path.Combine(_uploadsFolder, folder, fileName);
    }

    private string GetContentType(string fileExtension)
    {
        // Default content type
        string contentType = "application/octet-stream";
        
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".txt", "text/plain" },
            { ".pdf", "application/pdf" },
            { ".doc", "application/msword" },
            { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
            { ".xls", "application/vnd.ms-excel" },
            { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
            { ".png", "image/png" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".gif", "image/gif" },
            { ".zip", "application/zip" }
        };
        
        if (mappings.ContainsKey(fileExtension))
        {
            contentType = mappings[fileExtension];
        }
        
        return contentType;
    }

    private string ComputeSha256Hash(string input)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            byte[] hash = sha256.ComputeHash(bytes);
            
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }
}