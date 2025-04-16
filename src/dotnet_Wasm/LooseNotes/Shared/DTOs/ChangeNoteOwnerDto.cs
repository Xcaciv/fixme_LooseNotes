using System.ComponentModel.DataAnnotations;

namespace Xcaciv.LooseNotes.Wasm.Shared.DTOs;

public class ChangeNoteOwnerDto
{
    [Required(ErrorMessage = "New owner ID is required")]
    public string NewOwnerId { get; set; } = string.Empty;
}