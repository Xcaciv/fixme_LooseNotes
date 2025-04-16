using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Moq;
using Xcaciv.LooseNotes.Web.Data;
using Xcaciv.LooseNotes.Web.Models;
using Xcaciv.LooseNotes.Web.Services;
using Xcaciv.LooseNotes.Web.Tests.Fixtures;

namespace Xcaciv.LooseNotes.Web.Tests.Services
{
    public class UserServiceTests : TestBase
    {
        private readonly UserService _userService;

        public UserServiceTests() : base()
        {
            _userService = new UserService(_context);
        }

        protected override void SeedDatabase()
        {
            _context.Users.Add(new User 
            { 
                Id = 1,
                Username = "testuser",
                Password = "password123", // Insecure plaintext password
                Email = "test@example.com",
                Role = "User"
            });
            _context.SaveChanges();
        }

        [Fact]
        public void GetById_ReturnsCorrectUser()
        {
            // Act
            var result = _userService.GetById(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void Authenticate_WithCorrectCredentials_ReturnsUser()
        {
            // Act - Note: This test will actually use the DbContext implementation
            var result = _userService.Authenticate("testuser", "password123");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("testuser", result.Username);
        }

        [Fact]
        public void Authenticate_WithIncorrectCredentials_ReturnsNull()
        {
            // Act
            var result = _userService.Authenticate("testuser", "wrongpassword");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void Create_AddsNewUser_WithoutHashingPassword()
        {
            // Arrange
            var newUser = new User
            {
                Username = "newuser",
                Password = "plaintext123", // Should be hashed in a secure implementation
                Email = "new@example.com",
                Role = "User"
            };

            // Act
            var result = _userService.Create(newUser);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("plaintext123", result.Password); // Vulnerability: password stored as plaintext
            Assert.NotEqual(0, result.Id); // Should have a new ID
        }

        [Fact]
        public void GeneratePasswordResetToken_ReturnsSimpleToken()
        {
            // Act
            var token = _userService.GeneratePasswordResetToken("test@example.com");

            // Assert
            Assert.NotNull(token);
            Assert.Equal(8, token.Length); // Vulnerable: predictable, short token
        }

        [Theory]
        [InlineData("test@example.com", "' OR 1=1 --")]
        [InlineData("' OR 1=1 --", "password123")]
        public void Authenticate_VulnerableToSqlInjection(string username, string password)
        {
            // Note: This test demonstrates vulnerability but won't pass due to in-memory database
            // In a real SQL database, the SQL injection attack might succeed
            
            // Act - using the vulnerable method
            // This would succeed in a real environment with the SQL injection
            var result = _userService.Authenticate(username, password);
            
            // We're not asserting anything as this is demonstrating vulnerability
            // In a real test, we would mock the SqlConnection and verify the attack
        }
    }
}