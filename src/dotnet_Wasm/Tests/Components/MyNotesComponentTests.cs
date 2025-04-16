using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xcaciv.LooseNotes.Wasm.Client.Pages;
using Xcaciv.LooseNotes.Wasm.Client.Services;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Tests.Components;

public class MyNotesComponentTests : TestContext
{
    private readonly Mock<INoteService> _noteServiceMock;
    private readonly Mock<NavigationManager> _navigationManagerMock;

    public MyNotesComponentTests()
    {
        _noteServiceMock = new Mock<INoteService>();
        _navigationManagerMock = new Mock<NavigationManager>();
        
        Services.AddSingleton(_noteServiceMock.Object);
        Services.AddSingleton(_navigationManagerMock.Object);
    }

    [Fact]
    public void MyNotes_DisplaysLoadingState_Initially()
    {
        // Arrange
        _noteServiceMock
            .Setup(service => service.GetMyNotesAsync())
            .Returns(Task.Delay(1000).ContinueWith(_ => new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = new List<NoteDto>()
            }));

        // Act
        var cut = RenderComponent<MyNotes>();

        // Assert
        cut.Find(".spinner-border");
    }

    [Fact]
    public void MyNotes_DisplaysEmptyMessage_WhenNoNotes()
    {
        // Arrange
        _noteServiceMock
            .Setup(service => service.GetMyNotesAsync())
            .ReturnsAsync(new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = new List<NoteDto>()
            });

        // Act
        var cut = RenderComponent<MyNotes>();

        // Assert
        cut.Find(".alert.alert-info");
        cut.FindAll(".card").Count.Should().Be(0);
    }

    [Fact]
    public void MyNotes_DisplaysNotes_WhenNotesExist()
    {
        // Arrange
        var notes = new List<NoteDto>
        {
            new() { Id = 1, Title = "Test Note 1", Content = "Content 1", IsPublic = false },
            new() { Id = 2, Title = "Test Note 2", Content = "Content 2", IsPublic = true }
        };

        _noteServiceMock
            .Setup(service => service.GetMyNotesAsync())
            .ReturnsAsync(new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = notes
            });

        // Act
        var cut = RenderComponent<MyNotes>();

        // Assert
        var cards = cut.FindAll(".card");
        cards.Count.Should().Be(2);
        
        // First note should have its title displayed
        cards[0].InnerHtml.Should().Contain("Test Note 1");
        
        // Second note should have the public badge
        cards[1].InnerHtml.Should().Contain("Public");
    }

    [Fact]
    public void MyNotes_PerformsSearch_WhenSearchButtonClicked()
    {
        // Arrange
        _noteServiceMock
            .Setup(service => service.GetMyNotesAsync())
            .ReturnsAsync(new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = new List<NoteDto>()
            });

        _noteServiceMock
            .Setup(service => service.SearchNotesAsync(It.IsAny<string>()))
            .ReturnsAsync(new ServiceResponse<List<NoteDto>>
            {
                Success = true,
                Data = new List<NoteDto>
                {
                    new() { Id = 1, Title = "Test Note 1", Content = "Content 1" }
                }
            });

        // Act
        var cut = RenderComponent<MyNotes>();
        
        // Find the input and set its value directly
        var searchInput = cut.Find("input.form-control");
        searchInput.Input("test search"); // Use Input method instead of Change
        
        // Now click the search button
        var searchButton = cut.Find("button.btn-outline-secondary");
        searchButton.Click();

        // Assert
        _noteServiceMock.Verify(service => service.SearchNotesAsync("test search"), Times.Once);
    }
}