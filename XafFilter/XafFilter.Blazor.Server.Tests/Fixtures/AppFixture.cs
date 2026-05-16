using System.Diagnostics;
using Microsoft.Playwright;

namespace XafFilter.Blazor.Server.Tests.Fixtures;

public sealed class AppFixture : IAsyncLifetime
{
    // HTTP (not HTTPS) avoids dev-cert / SignalR-handshake issues in headless Chromium.
    public const string BaseUrl = "http://localhost:5000";

    private Process? _serverProcess;
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await StartServerAsync();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        Playwright?.Dispose();

        if (_serverProcess is { HasExited: false })
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                await _serverProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            }
            catch { /* best effort */ }
        }
    }

    public async Task<IBrowserContext> NewLoggedInContextAsync()
    {
        var ctx = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(30_000);
        page.PageError += (_, msg) => Console.Error.WriteLine($"[pageerror] {msg}");
        page.Console += (_, msg) => Console.Error.WriteLine($"[console.{msg.Type}] {msg.Text}");
        page.Response += (_, r) => { if (r.Status >= 400) Console.Error.WriteLine($"[http {r.Status}] {r.Url}"); };

        await page.GotoAsync($"{BaseUrl}/LoginPage", new() { WaitUntil = WaitUntilState.NetworkIdle });
        try
        {
            await page.WaitForSelectorAsync("button[data-action-name='Log In']", new() { Timeout = 60_000 });
        }
        catch
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "screenshots");
            Directory.CreateDirectory(dir);
            await page.ScreenshotAsync(new() { Path = Path.Combine(dir, "login-fail.png"), FullPage = true });
            var html = await page.ContentAsync();
            File.WriteAllText(Path.Combine(dir, "login-fail.html"), html);
            Console.Error.WriteLine($"[login-fail] URL={page.Url}, html length={html.Length}");
            throw;
        }
        await page.Locator("input[type='text']").First.FillAsync("Admin");
        await page.Locator("button[data-action-name='Log In']:not([virtual-id])").ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("LoginPage"), new() { Timeout = 15_000 });

        await page.CloseAsync();
        return ctx;
    }

    private async Task StartServerAsync()
    {
        var repoRoot = FindRepoRoot();
        var hostProj = Path.Combine(repoRoot, "XafFilter", "XafFilter.Blazor.Server");

        var psi = new ProcessStartInfo
        {
            FileName  = "dotnet",
            Arguments = "run -c EasyTest --no-launch-profile --urls=http://localhost:5000",
            WorkingDirectory = hostProj,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        // Without --launch-profile, ASPNETCORE_ENVIRONMENT defaults to Production,
        // which makes UseStaticWebAssets() skip the _content/* manifest → blazor.server.js 404 → no circuit.
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        _serverProcess = Process.Start(psi);

        if (_serverProcess is null) throw new InvalidOperationException("Failed to start dotnet run");

        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync($"{BaseUrl}/LoginPage");
                if ((int)r.StatusCode == 200) return;
            }
            catch { /* not ready */ }
            await Task.Delay(500);
        }
        throw new TimeoutException("XafFilter.Blazor.Server did not become ready within 90s.");
    }

    private static string FindRepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "XafFilter.slnx"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("XafFilter.slnx not found above test bin");
    }
}

[CollectionDefinition("App")]
public class AppCollection : ICollectionFixture<AppFixture> { }
