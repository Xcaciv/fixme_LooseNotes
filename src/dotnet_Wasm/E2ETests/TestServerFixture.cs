using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Xcaciv.LooseNotes.Wasm.E2ETests;

// Using generic WebApplicationFactory instead of referencing the Program class directly
public class TestServerFixture : WebApplicationFactory<MvcTestingStartup>, IAsyncLifetime
{
    private IHost? _host;
    public string ServerBaseUrl { get; } = "http://localhost:5000";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create the host for the test server
        builder.ConfigureWebHost(webHostBuilder =>
        {
            webHostBuilder.UseUrls(ServerBaseUrl);
            
            // Configure the test services here
            webHostBuilder.ConfigureServices(services =>
            {
                // Replace services with test implementations if needed
                // For example, you could replace the authentication with a test version
            });
        });

        _host = builder.Build();
        _host.Start();
        return _host;
    }

    public async Task InitializeAsync()
    {
        // Start the server when the fixture is initialized
        // Keeping this simple for now since we're testing with skips
        await Task.CompletedTask;
    }

    public new async Task DisposeAsync()
    {
        // Stop the server when the fixture is disposed
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        
        base.Dispose();
    }
}

// Empty class just to satisfy the generic parameter of WebApplicationFactory
public class MvcTestingStartup
{
}