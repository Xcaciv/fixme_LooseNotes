using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xcaciv.LooseNotes.Wasm.Client.Pages;
using Xcaciv.LooseNotes.Wasm.Client.Services;
using Xcaciv.LooseNotes.Wasm.Shared.DTOs;

namespace Xcaciv.LooseNotes.Wasm.Tests.Components;

public class LoginComponentTests : TestContext
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<NavigationManager> _navigationManagerMock;
    private readonly Mock<Microsoft.JSInterop.IJSRuntime> _jsRuntimeMock;

    public LoginComponentTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _navigationManagerMock = new Mock<NavigationManager>();
        _jsRuntimeMock = new Mock<Microsoft.JSInterop.IJSRuntime>();
        
        Services.AddSingleton(_authServiceMock.Object);
        Services.AddSingleton(_navigationManagerMock.Object);
        Services.AddSingleton(_jsRuntimeMock.Object);
    }

    [Fact]
    public void Login_RendersProperly()
    {
        // Act
        var cut = RenderComponent<Login>();
        
        // Assert
        cut.Find("h2").TextContent.Should().Be("Login");
        cut.Find("form").Should().NotBeNull();
        cut.Find("input#email").Should().NotBeNull(); // Email/Username field
        cut.Find("input#password").Should().NotBeNull(); // Password field
        cut.Find("button[type=submit]").TextContent.Should().Contain("Sign In");
    }

    [Fact]
    public void Login_ShowsValidationErrors_WhenFormSubmittedEmpty()
    {
        // Act
        var cut = RenderComponent<Login>();
        var form = cut.Find("form");
        form.Submit();
        
        // Assert
        cut.Markup.Should().Contain("Username or email is required");
        cut.Markup.Should().Contain("Password is required");
    }

    [Fact]
    public void Login_CallsLoginService_WhenValidFormSubmitted()
    {
        // Arrange
        _authServiceMock
            .Setup(service => service.LoginAsync(It.IsAny<LoginUserDto>()))
            .ReturnsAsync(new ServiceResponse<string>
            {
                Success = true,
                Data = "token123",
                Message = "Login successful"
            });

        // Act
        var cut = RenderComponent<Login>();
        var emailInput = cut.Find("input#email");
        var passwordInput = cut.Find("input#password");
        
        emailInput.Change("testuser");
        passwordInput.Change("password123");
        
        var form = cut.Find("form");
        form.Submit();
        
        // Assert
        _authServiceMock.Verify(service => service.LoginAsync(It.Is<LoginUserDto>(dto => 
            dto.UserNameOrEmail == "testuser" && dto.Password == "password123")), Times.Once);
    }

    [Fact]
    public void Login_DisplaysErrorMessage_WhenLoginFails()
    {
        // Arrange
        _authServiceMock
            .Setup(service => service.LoginAsync(It.IsAny<LoginUserDto>()))
            .ReturnsAsync(new ServiceResponse<string>
            {
                Success = false,
                Message = "Invalid username or password"
            });

        // Act
        var cut = RenderComponent<Login>();
        var emailInput = cut.Find("input#email");
        var passwordInput = cut.Find("input#password");
        
        emailInput.Change("testuser");
        passwordInput.Change("wrongpassword");
        
        var form = cut.Find("form");
        form.Submit();
        
        // Assert
        cut.Markup.Should().Contain("Invalid username or password");
        cut.Find(".alert-danger").Should().NotBeNull();
    }
}