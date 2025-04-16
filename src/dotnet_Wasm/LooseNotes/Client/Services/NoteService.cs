using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace Xcaciv.LooseNotes.Wasm.Client.Services;

public class NoteService : INoteService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NoteService> _logger;
    private readonly IAntiForgeryService _antiForgeryService;

    private const string AntiForgeryHeader = "X-XSRF-TOKEN";

    public NoteService(HttpClient httpClient, ILogger<NoteService> logger, IAntiForgeryService antiForgeryService)
    {
        _httpClient = httpClient;
        _logger = logger;
        _antiForgeryService = antiForgeryService;
    }

    public async Task<ServiceResponse<List<NoteDto>>> GetMyNotesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/notes");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<NoteDto>>(response);
            }

            var notes = await response.Content.ReadFromJsonAsync<List<NoteDto>>();
            
            return new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = notes ?? new List<NoteDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user notes");
            return new ServiceResponse<List<NoteDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving your notes"
            };
        }
    }

    public async Task<ServiceResponse<List<NoteDto>>> GetPublicNotesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/notes/public");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<NoteDto>>(response);
            }

            var notes = await response.Content.ReadFromJsonAsync<List<NoteDto>>();
            
            return new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = notes ?? new List<NoteDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public notes");
            return new ServiceResponse<List<NoteDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving public notes"
            };
        }
    }

    public async Task<ServiceResponse<List<NoteDto>>> GetTopRatedNotesAsync(int count = 10)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/notes/top-rated?count={count}");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<NoteDto>>(response);
            }

            var notes = await response.Content.ReadFromJsonAsync<List<NoteDto>>();
            
            return new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = notes ?? new List<NoteDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top rated notes");
            return new ServiceResponse<List<NoteDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving top rated notes"
            };
        }
    }

    public async Task<ServiceResponse<NoteDto>> GetNoteByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/notes/{id}");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteDto>(response);
            }

            var note = await response.Content.ReadFromJsonAsync<NoteDto>();
            
            if (note == null)
            {
                return new ServiceResponse<NoteDto>
                {
                    Success = false,
                    Message = "Note not found"
                };
            }

            return new ServiceResponse<NoteDto>
            {
                Success = true,
                Data = note
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting note {NoteId}", id);
            return new ServiceResponse<NoteDto>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the note"
            };
        }
    }

    public async Task<ServiceResponse<NoteDto>> GetNoteByShareTokenAsync(string token)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/notes/shared/{token}");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteDto>(response);
            }

            var note = await response.Content.ReadFromJsonAsync<NoteDto>();
            
            if (note == null)
            {
                return new ServiceResponse<NoteDto>
                {
                    Success = false,
                    Message = "Shared note not found"
                };
            }

            return new ServiceResponse<NoteDto>
            {
                Success = true,
                Data = note
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shared note with token {Token}", token);
            return new ServiceResponse<NoteDto>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving the shared note"
            };
        }
    }

    public async Task<ServiceResponse<NoteDto>> CreateNoteAsync(CreateUpdateNoteDto noteDto)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, "api/notes");
            request.Headers.Add(AntiForgeryHeader, token);
            request.Content = JsonContent.Create(noteDto);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteDto>(response);
            }

            var createdNote = await response.Content.ReadFromJsonAsync<NoteDto>();
            
            if (createdNote == null)
            {
                return new ServiceResponse<NoteDto>
                {
                    Success = false,
                    Message = "Note was created but could not be retrieved"
                };
            }

            return new ServiceResponse<NoteDto>
            {
                Success = true,
                Message = "Note created successfully",
                Data = createdNote
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note");
            return new ServiceResponse<NoteDto>
            {
                Success = false,
                Message = "An unexpected error occurred while creating the note"
            };
        }
    }

    public async Task<ServiceResponse<NoteDto>> UpdateNoteAsync(int id, CreateUpdateNoteDto noteDto)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Put, $"api/notes/{id}");
            request.Headers.Add(AntiForgeryHeader, token);
            request.Content = JsonContent.Create(noteDto);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteDto>(response);
            }

            var updatedNote = await response.Content.ReadFromJsonAsync<NoteDto>();
            
            if (updatedNote == null)
            {
                return new ServiceResponse<NoteDto>
                {
                    Success = false,
                    Message = "Note was updated but could not be retrieved"
                };
            }

            return new ServiceResponse<NoteDto>
            {
                Success = true,
                Message = "Note updated successfully",
                Data = updatedNote
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating note {NoteId}", id);
            return new ServiceResponse<NoteDto>
            {
                Success = false,
                Message = "An unexpected error occurred while updating the note"
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteNoteAsync(int id)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/notes/{id}");
            request.Headers.Add(AntiForgeryHeader, token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Note deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting note {NoteId}", id);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the note"
            };
        }
    }

    public async Task<ServiceResponse<string>> GenerateShareTokenAsync(int id)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/notes/{id}/share");
            request.Headers.Add(AntiForgeryHeader, token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<string>(response);
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            var shareUrl = result.GetProperty("shareUrl").GetString();

            if (string.IsNullOrEmpty(shareUrl))
            {
                return new ServiceResponse<string>
                {
                    Success = false,
                    Message = "Share token was generated but the URL was not received"
                };
            }

            return new ServiceResponse<string>
            {
                Success = true,
                Message = "Share token generated successfully",
                Data = shareUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating share token for note {NoteId}", id);
            return new ServiceResponse<string>
            {
                Success = false,
                Message = "An unexpected error occurred while generating share token"
            };
        }
    }

    public async Task<ServiceResponse<bool>> ChangeNoteOwnerAsync(int id, string newOwnerId)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var model = new ChangeNoteOwnerDto { NewOwnerId = newOwnerId };
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/notes/{id}/change-owner");
            request.Headers.Add(AntiForgeryHeader, token);
            request.Content = JsonContent.Create(model);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Note ownership changed successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing ownership of note {NoteId}", id);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while changing note ownership"
            };
        }
    }

    public async Task<ServiceResponse<List<NoteDto>>> SearchNotesAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new ServiceResponse<List<NoteDto>>
                {
                    Success = false,
                    Message = "Search query cannot be empty",
                    Data = new List<NoteDto>()
                };
            }

            var response = await _httpClient.GetAsync($"api/notes/search?query={Uri.EscapeDataString(query)}");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<NoteDto>>(response);
            }

            var notes = await response.Content.ReadFromJsonAsync<List<NoteDto>>();
            
            return new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = notes ?? new List<NoteDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching notes with query: {Query}", query);
            return new ServiceResponse<List<NoteDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while searching notes",
                Data = new List<NoteDto>()
            };
        }
    }

    public async Task<ServiceResponse<NoteAttachmentDto>> AddAttachmentAsync(int noteId, MultipartFormDataContent content)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/notes/{noteId}/attachments");
            request.Headers.Add(AntiForgeryHeader, token);
            request.Content = content;
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteAttachmentDto>(response);
            }

            var attachment = await response.Content.ReadFromJsonAsync<NoteAttachmentDto>();
            
            if (attachment == null)
            {
                return new ServiceResponse<NoteAttachmentDto>
                {
                    Success = false,
                    Message = "Attachment was uploaded but details could not be retrieved"
                };
            }

            return new ServiceResponse<NoteAttachmentDto>
            {
                Success = true,
                Message = "Attachment uploaded successfully",
                Data = attachment
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for note {NoteId}", noteId);
            return new ServiceResponse<NoteAttachmentDto>
            {
                Success = false,
                Message = "An unexpected error occurred while uploading the attachment"
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteAttachmentAsync(int noteId, int attachmentId)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/notes/{noteId}/attachments/{attachmentId}");
            request.Headers.Add(AntiForgeryHeader, token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Attachment deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting attachment {AttachmentId} for note {NoteId}", attachmentId, noteId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the attachment"
            };
        }
    }

    public string GetAttachmentDownloadUrl(int noteId, int attachmentId)
    {
        return $"api/notes/{noteId}/attachments/{attachmentId}";
    }

    public async Task<ServiceResponse<NoteRatingDto>> RateNoteAsync(int noteId, NoteRatingDto ratingDto)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"api/notes/{noteId}/rate");
            request.Headers.Add(AntiForgeryHeader, token);
            request.Content = JsonContent.Create(ratingDto);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<NoteRatingDto>(response);
            }

            var rating = await response.Content.ReadFromJsonAsync<NoteRatingDto>();
            
            if (rating == null)
            {
                return new ServiceResponse<NoteRatingDto>
                {
                    Success = false,
                    Message = "Rating was submitted but details could not be retrieved"
                };
            }

            return new ServiceResponse<NoteRatingDto>
            {
                Success = true,
                Message = "Note rated successfully",
                Data = rating
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rating note {NoteId}", noteId);
            return new ServiceResponse<NoteRatingDto>
            {
                Success = false,
                Message = "An unexpected error occurred while rating the note"
            };
        }
    }

    public async Task<ServiceResponse<List<NoteRatingDto>>> GetRatingsForNoteAsync(int noteId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/notes/{noteId}/ratings");
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<List<NoteRatingDto>>(response);
            }

            var ratings = await response.Content.ReadFromJsonAsync<List<NoteRatingDto>>();
            
            return new ServiceResponse<List<NoteRatingDto>>
            {
                Success = true,
                Data = ratings ?? new List<NoteRatingDto>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ratings for note {NoteId}", noteId);
            return new ServiceResponse<List<NoteRatingDto>>
            {
                Success = false,
                Message = "An unexpected error occurred while retrieving ratings",
                Data = new List<NoteRatingDto>()
            };
        }
    }

    public async Task<ServiceResponse<bool>> DeleteRatingAsync(int ratingId)
    {
        try
        {
            var token = await _antiForgeryService.GetTokenAsync();
            
            var request = new HttpRequestMessage(HttpMethod.Delete, $"api/notes/ratings/{ratingId}");
            request.Headers.Add(AntiForgeryHeader, token);
            
            var response = await _httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                return await HandleErrorResponse<bool>(response);
            }

            return new ServiceResponse<bool>
            {
                Success = true,
                Message = "Rating deleted successfully",
                Data = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting rating {RatingId}", ratingId);
            return new ServiceResponse<bool>
            {
                Success = false,
                Message = "An unexpected error occurred while deleting the rating"
            };
        }
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