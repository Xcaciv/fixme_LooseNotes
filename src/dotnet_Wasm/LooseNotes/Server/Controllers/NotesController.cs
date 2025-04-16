using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Xcaciv.LooseNotes.Wasm.Server.Services;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly INoteService _noteService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(INoteService noteService, ILogger<NotesController> logger)
    {
        _noteService = noteService;
        _logger = logger;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetMyNotes()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var notes = await _noteService.GetNotesAsync(userId);
        return Ok(notes);
    }

    [HttpGet("public")]
    public async Task<IActionResult> GetPublicNotes()
    {
        var notes = await _noteService.GetPublicNotesAsync();
        return Ok(notes);
    }

    [HttpGet("top-rated")]
    public async Task<IActionResult> GetTopRatedNotes([FromQuery] int count = 10)
    {
        var notes = await _noteService.GetTopRatedNotesAsync(count);
        return Ok(notes);
    }

    [HttpGet("shared/{token}")]
    public async Task<IActionResult> GetSharedNote(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest(new { message = "Invalid share token" });
        }

        var note = await _noteService.GetNoteByShareTokenAsync(token);
        
        if (note == null)
        {
            return NotFound(new { message = "Note not found" });
        }

        return Ok(note);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetNote(int id)
    {
        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var note = await _noteService.GetNoteByIdAsync(id, userId);
        
        if (note == null)
        {
            return NotFound(new { message = "Note not found or you don't have access to it" });
        }

        return Ok(note);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateUpdateNoteDto noteDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var createdNote = await _noteService.CreateNoteAsync(noteDto, userId);
            return CreatedAtAction(nameof(GetNote), new { id = createdNote.Id }, createdNote);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating note for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while creating the note" });
        }
    }

    [Authorize]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateNote(int id, [FromBody] CreateUpdateNoteDto noteDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != noteDto.Id)
        {
            return BadRequest(new { message = "ID mismatch" });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var updatedNote = await _noteService.UpdateNoteAsync(noteDto, userId);
        
        if (updatedNote == null)
        {
            return NotFound(new { message = "Note not found or you don't have permission to update it" });
        }

        return Ok(updatedNote);
    }

    [Authorize]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _noteService.DeleteNoteAsync(id, userId);
        
        if (!result)
        {
            return NotFound(new { message = "Note not found or you don't have permission to delete it" });
        }

        return Ok(new { message = "Note deleted successfully" });
    }

    [Authorize]
    [HttpPost("{id:int}/share")]
    public async Task<IActionResult> GenerateShareToken(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        try
        {
            var token = await _noteService.GenerateShareTokenAsync(id, userId);
            return Ok(new { shareToken = token, shareUrl = $"{Request.Scheme}://{Request.Host}/notes/shared/{token}" });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "Note not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating share token for note {NoteId}", id);
            return StatusCode(500, new { message = "An error occurred while generating share token" });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/change-owner")]
    public async Task<IActionResult> ChangeNoteOwner(int id, [FromBody] ChangeNoteOwnerDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var adminUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(adminUserId))
        {
            return Unauthorized();
        }

        var result = await _noteService.ChangeNoteOwnerAsync(id, model.NewOwnerId, adminUserId);
        
        if (!result)
        {
            return BadRequest(new { message = "Failed to change note ownership" });
        }

        return Ok(new { message = "Note ownership changed successfully" });
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchNotes([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "Search query is required" });
        }

        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var notes = await _noteService.SearchNotesAsync(query, userId);
        return Ok(notes);
    }

    [Authorize]
    [HttpPost("{noteId:int}/attachments")]
    public async Task<IActionResult> AddAttachment(int noteId, [FromForm] AttachmentUploadDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        if (model.FileContent == null || model.FileContent.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        try
        {
            using var stream = new MemoryStream(model.FileContent);
            var attachment = await _noteService.AddAttachmentAsync(
                noteId, 
                userId, 
                stream, 
                model.FileName ?? "unknown", 
                model.ContentType ?? "application/octet-stream");

            return Ok(attachment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException)
        {
            return NotFound(new { message = "Note not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading attachment for note {NoteId}", noteId);
            return StatusCode(500, new { message = "An error occurred while uploading the attachment" });
        }
    }

    [Authorize]
    [HttpDelete("{noteId:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> DeleteAttachment(int noteId, int attachmentId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _noteService.DeleteAttachmentAsync(attachmentId, userId);
        
        if (!result)
        {
            return NotFound(new { message = "Attachment not found or you don't have permission to delete it" });
        }

        return Ok(new { message = "Attachment deleted successfully" });
    }

    [HttpGet("{noteId:int}/attachments/{attachmentId:int}")]
    public async Task<IActionResult> DownloadAttachment(int noteId, int attachmentId)
    {
        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        try
        {
            var attachmentStream = await _noteService.GetAttachmentStreamAsync(attachmentId, userId);
            
            if (attachmentStream == null)
            {
                return NotFound(new { message = "Attachment not found or you don't have access to it" });
            }

            // Get file details from our database
            var note = await _noteService.GetNoteByIdAsync(noteId, userId);
            if (note == null)
            {
                return NotFound(new { message = "Note not found or you don't have access to it" });
            }

            var attachment = note.Attachments.FirstOrDefault(a => a.Id == attachmentId);
            if (attachment == null)
            {
                return NotFound(new { message = "Attachment not found" });
            }

            return File(attachmentStream, attachment.ContentType, attachment.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading attachment {AttachmentId} for note {NoteId}", attachmentId, noteId);
            return StatusCode(500, new { message = "An error occurred while downloading the attachment" });
        }
    }

    [Authorize]
    [HttpPost("{noteId:int}/rate")]
    public async Task<IActionResult> RateNote(int noteId, [FromBody] NoteRatingDto ratingDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var rating = await _noteService.RateNoteAsync(noteId, ratingDto, userId);
        
        if (rating == null)
        {
            return NotFound(new { message = "Note not found" });
        }

        return Ok(rating);
    }

    [HttpGet("{noteId:int}/ratings")]
    public async Task<IActionResult> GetRatings(int noteId)
    {
        string? userId = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        var ratings = await _noteService.GetRatingsForNoteAsync(noteId, userId);
        return Ok(ratings);
    }

    [Authorize]
    [HttpDelete("ratings/{ratingId:int}")]
    public async Task<IActionResult> DeleteRating(int ratingId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _noteService.DeleteRatingAsync(ratingId, userId);
        
        if (!result)
        {
            return NotFound(new { message = "Rating not found or you don't have permission to delete it" });
        }

        return Ok(new { message = "Rating deleted successfully" });
    }
}