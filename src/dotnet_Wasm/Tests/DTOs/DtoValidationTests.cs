using System.ComponentModel.DataAnnotations;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Tests.DTOs;

public class DtoValidationTests
{
    [Fact]
    public void RegisterUserDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            UserName = "testuser",
            Email = "test@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void RegisterUserDto_InvalidData_FailsValidation()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            UserName = "t", // Too short
            Email = "invalid-email", // Invalid format
            Password = "weak", // Too weak
            ConfirmPassword = "notmatching" // Doesn't match password
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("UserName"));
        Assert.Contains(results, r => r.MemberNames.Contains("Email"));
        Assert.Contains(results, r => r.MemberNames.Contains("Password"));
    }

    [Fact]
    public void CreateUpdateNoteDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new CreateUpdateNoteDto
        {
            Title = "Valid Title",
            Content = "This is valid content."
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void CreateUpdateNoteDto_InvalidData_FailsValidation()
    {
        // Arrange
        var dto = new CreateUpdateNoteDto
        {
            Title = "", // Empty title
            Content = "" // Empty content
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Title"));
        Assert.Contains(results, r => r.MemberNames.Contains("Content"));
    }

    [Fact]
    public void NoteRatingDto_ValidData_PassesValidation()
    {
        // Arrange
        var dto = new NoteRatingDto
        {
            NoteId = 1,
            Rating = 4,
            Comment = "This is a good note."
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(results);
    }

    [Fact]
    public void NoteRatingDto_InvalidRating_FailsValidation()
    {
        // Arrange - Rating out of range (1-5)
        var dto = new NoteRatingDto
        {
            NoteId = 1,
            Rating = 6, // Invalid rating (must be 1-5)
            Comment = "This is a good note."
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Rating"));
    }
}