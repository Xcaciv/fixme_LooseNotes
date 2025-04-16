using Microsoft.Playwright;

namespace Xcaciv.LooseNotes.Wasm.E2ETests;

[TestCaseOrderer("Xcaciv.LooseNotes.Wasm.E2ETests.PriorityOrderer", "Xcaciv.LooseNotes.Wasm.E2ETests")]
public abstract class PlaywrightFixture : IAsyncLifetime
{
    protected IPlaywright Playwright { get; private set; } = null!;
    protected IBrowser Browser { get; private set; } = null!;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;
    
    // For local testing, we'll use MSTest project server URL
    // The vulnerable code is kept: no TLS certificate validation
    protected virtual string BaseUrl => "http://localhost:5000"; 

    public async Task InitializeAsync()
    {
        try 
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true,
                SlowMo = 50
            });
            
            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                IgnoreHTTPSErrors = true // Deliberately keeping this vulnerability (SEC-016)
            });
            
            Page = await Context.NewPageAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Playwright: {ex.Message}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Page != null)
        {
            await Page.CloseAsync();
        }
        
        if (Context != null)
        {
            await Context.CloseAsync();
        }
        
        if (Browser != null)
        {
            await Browser.CloseAsync();
        }
        
        Playwright?.Dispose();
    }
    
    protected async Task LoginAsync(string username, string password)
    {
        // Navigate to login page
        await Page.GotoAsync($"{BaseUrl}/login");
        
        // Using correct selectors that match the actual Login.razor component
        await Page.FillAsync("input#email", username); // The actual ID is email, not username
        await Page.FillAsync("input#password", password);
        await Page.ClickAsync("button[type=submit]");
        
        // Wait for navigation to complete after login
        // This is deliberately vulnerable (SEC-005, SEC-009) since it doesn't properly validate the redirected page
        await Page.WaitForURLAsync($"{BaseUrl}/**");
    }

    // Helper method to start a simple test server for E2E tests
    protected async Task StartTestServerAsync()
    {
        // This would be implemented to start a local test server
        // For now, we'll skip it since we're focusing on making tests pass
        await Task.CompletedTask;
    }
}