using Xcaciv.LooseNotes.Wasm.Shared.Models;

namespace Xcaciv.LooseNotes.Wasm.Tests.Models;

public class NoteTests
{
    [Fact]
    public void Note_PropertiesInitializeCorrectly()
    {
        // Arrange
        var note = new Note
        {
            Id = 1,
            Title = "Test Note",
            Content = "This is a test note content.",
            IsPublic = true,
            ShareToken = "test-token-123",
            UserId = "user-123",
        };

        // Act & Assert
        Assert.Equal(1, note.Id);
        Assert.Equal("Test Note", note.Title);
        Assert.Equal("This is a test note content.", note.Content);
        Assert.True(note.IsPublic);
        Assert.Equal("test-token-123", note.ShareToken);
        Assert.Equal("user-123", note.UserId);
    }

    [Fact]
    public void Note_CreatedAtInitializedToCurrentTime()
    {
        // Arrange
        var beforeCreation = DateTimeOffset.UtcNow.AddSeconds(-1);
        
        // Act
        var note = new Note();
        var afterCreation = DateTimeOffset.UtcNow.AddSeconds(1);
        
        // Assert
        Assert.True(note.CreatedAt >= beforeCreation);
        Assert.True(note.CreatedAt <= afterCreation);
    }

    [Fact]
    public void Note_AverageRatingCalculatedCorrectly()
    {
        // Arrange
        var note = new Note
        {
            Id = 1,
            Title = "Test Note",
            Ratings = new List<NoteRating>
            {
                new NoteRating { Rating = 3 },
                new NoteRating { Rating = 4 },
                new NoteRating { Rating = 5 }
            }
        };

        // Act
        double averageRating = note.CalculateAverageRating();

        // Assert
        Assert.Equal(4.0, averageRating);
    }

    [Fact]
    public void Note_AverageRatingIsZeroWhenNoRatings()
    {
        // Arrange
        var note = new Note
        {
            Id = 1,
            Title = "Test Note",
            Ratings = new List<NoteRating>()
        };

        // Act
        double averageRating = note.CalculateAverageRating();

        // Assert
        Assert.Equal(0, averageRating);
    }
}