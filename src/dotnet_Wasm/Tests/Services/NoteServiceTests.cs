using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xcaciv.LooseNotes.Wasm.Client.Services;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Tests.Services;

public class NoteServiceTests
{
    private readonly Mock<ILogger<NoteService>> _loggerMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<IAntiForgeryService> _antiForgeryServiceMock;
    private readonly HttpClient _httpClient;
    private readonly NoteService _noteService;

    public NoteServiceTests()
    {
        _loggerMock = new Mock<ILogger<NoteService>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _antiForgeryServiceMock = new Mock<IAntiForgeryService>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("https://example.com/")
        };
        _noteService = new NoteService(_httpClient, _loggerMock.Object, _antiForgeryServiceMock.Object);
    }

    [Fact]
    public async Task GetMyNotesAsync_ReturnsNotes_WhenApiCallSucceeds()
    {
        // Arrange
        var expectedNotes = new List<NoteDto>
        {
            new() { Id = 1, Title = "Test Note 1", Content = "Content 1" },
            new() { Id = 2, Title = "Test Note 2", Content = "Content 2" }
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(expectedNotes)
            });

        // Act
        var result = await _noteService.GetMyNotesAsync();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Test Note 1", result.Data[0].Title);
        Assert.Equal("Test Note 2", result.Data[1].Title);
    }

    [Fact]
    public async Task GetMyNotesAsync_ReturnsError_WhenApiCallFails()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = JsonContent.Create(new { message = "Error fetching notes" })
            });

        // Act
        var result = await _noteService.GetMyNotesAsync();

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Error fetching notes", result.Message);
    }

    [Fact]
    public async Task CreateNoteAsync_ReturnsCreatedNote_WhenApiCallSucceeds()
    {
        // Arrange
        var noteDto = new CreateUpdateNoteDto
        {
            Title = "New Note",
            Content = "Note Content",
            IsPublic = true
        };

        var createdNote = new NoteDto
        {
            Id = 1,
            Title = "New Note",
            Content = "Note Content",
            IsPublic = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Created,
                Content = JsonContent.Create(createdNote)
            });

        // Act
        var result = await _noteService.CreateNoteAsync(noteDto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Note created successfully", result.Message);
        Assert.Equal(1, result.Data.Id);
        Assert.Equal("New Note", result.Data.Title);
    }

    [Fact]
    public async Task DeleteNoteAsync_ReturnsSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        int noteIdToDelete = 1;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = JsonContent.Create(new { message = "Note deleted successfully" })
            });

        // Act
        var result = await _noteService.DeleteNoteAsync(noteIdToDelete);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Note deleted successfully", result.Message);
        Assert.True(result.Data);
    }
}