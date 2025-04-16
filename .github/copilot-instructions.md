# GitHub Copilot Instructions - Loose Notes Web Application

## Project Overview

The Loose Notes Web Application is a multi-user note-taking platform built with ASP.NET Core 8. This document provides guidance for working with GitHub Copilot in this specific project, highlighting best practices for ASP.NET 8 development and security remediation.

## ASP.NET 8 Best Practices

### 1. Configuration and Startup

- Use the `WebApplicationBuilder` pattern from Program.cs instead of Startup.cs
- Implement configuration with the options pattern for strongly-typed settings
- Use environment-specific configuration (appsettings.{Environment}.json)
- Secure sensitive configuration data with User Secrets during development

```csharp
// Example builder configuration pattern
var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
```

### 2. Dependency Injection

- Register services with appropriate lifetimes (Singleton, Scoped, Transient)
- Use constructor injection over property or method injection
- Register interfaces for services to enable easier testing
- Avoid service locator pattern; use DI constructor injection

```csharp
// Example service registration
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<IUserService, UserService>();
```

### 3. Entity Framework Core

- Use DbContext with the repository pattern where appropriate
- Implement data migrations properly with version control
- Use async/await patterns for database operations
- Properly parameterize queries to prevent SQL injection
- Consider using specification pattern for complex queries

```csharp
// Example of proper EF Core usage
await _context.Notes
    .Where(n => n.UserId == userId)
    .ToListAsync();
```

### 4. Security Best Practices

- Replace deprecated `System.Data.SqlClient` with `Microsoft.Data.SqlClient`
- Use ASP.NET Core Identity for authentication and authorization
- Implement proper password hashing (with Identity or BCrypt)
- Use Data Protection API for sensitive data
- Implement HTTPS with proper certificate handling
- Add CSRF protection via anti-forgery tokens
- Sanitize user input to prevent XSS attacks
- Use proper authorization attributes and policies
- Use specific data types and allow list values for input strings

```csharp
// Example of proper authorization
[Authorize(Policy = "NoteOwnerPolicy")]
public async Task<IActionResult> Edit(int id)
{
    // Logic for authorized users only
}
```

### 5. Minimal API Enhancements (When Appropriate)

- Consider using minimal APIs for simpler endpoints
- Implement proper route grouping
- Use TypedResults for improved responses
- Implement proper parameter binding and validation

```csharp
// Example minimal API pattern
app.MapGet("/api/notes/{id}", async (int id, INoteService noteService) =>
    await noteService.GetNoteByIdAsync(id) is Note note
        ? Results.Ok(note)
        : Results.NotFound());
```

### 6. Performance Best Practices

- Implement response caching where appropriate
- Use async/await patterns consistently
- Consider output caching for heavy operations
- Implement proper model binding
- Use pagination for large result sets
- Consider using compiled queries for frequently executed EF Core operations

```csharp
// Example of response caching
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
public IActionResult Index()
{
    // Method implementation
}
```

## Project-Specific Security Remediations

Based on the PRD document, when using Copilot to assist with this project, focus on fixing these security vulnerabilities:

1. Replace plaintext password storage with proper hashing (SEC-001)
2. Parameterize all database queries to prevent SQL injection (SEC-002)
3. Implement proper content sanitization to prevent XSS (SEC-003)
4. Add CSRF protection to all state-changing operations (SEC-004)
5. Implement proper authorization checks for resource access (SEC-005)
6. Validate and sanitize file paths to prevent path traversal (SEC-006)
7. Remove or properly secure command execution functionality (SEC-007)
8. Implement secure file upload validation and handling (SEC-008)
9. Improve session security mechanisms (SEC-009)
10. Implement proper error handling to prevent information disclosure (SEC-010)

## GitHub Copilot Usage Tips

1. **Comment-Driven Development**: When working with Copilot, write clear comments describing what you want to achieve before letting Copilot generate code.

```csharp
// Implement a secure password hashing method using BCrypt
```

2. **Contextual Prompting**: Provide context in your comments to get better suggestions.

```csharp
// Create a validator for Note entities to prevent XSS attacks in content
```

3. **Iterate and Refine**: Don't accept the first suggestion - review, revise, and ask Copilot to improve.

```csharp
// Refactor this method to use async/await pattern with proper error handling
```

4. **Implementing Patterns**: Ask Copilot to implement specific patterns.

```csharp
// Implement repository pattern for NoteService with proper EF Core usage
```

5. **Fix Security Issues**: Specifically target security improvements.

```csharp
// Refactor this method to prevent SQL injection by using parameterized queries
```

## Testing Considerations

- Write unit tests for all service methods
- Implement integration tests for controllers
- Add security-focused tests to validate vulnerability fixes
- Use mock objects for external dependencies
- Follow AAA pattern (Arrange-Act-Assert) in test methods

## Modernization Recommendations

When working with legacy code, ask Copilot to help modernize patterns:

1. Replace older HttpContext session usage with ASP.NET Core Identity
2. Upgrade from direct SQL connections to Entity Framework patterns
3. Refactor controllers to use async/await patterns
4. Implement proper model binding and validation
5. Add API endpoints with OpenAPI documentation where appropriate

## Conclusion

This document serves as a guide for using GitHub Copilot effectively with the Loose Notes Web Application project. Following these ASP.NET 8 best practices and security remediation guidelines will help ensure the application is developed according to modern standards and security requirements.