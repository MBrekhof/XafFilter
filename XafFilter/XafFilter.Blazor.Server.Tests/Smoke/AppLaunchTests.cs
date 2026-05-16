using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Smoke;

[Collection("App")]
public class AppLaunchTests
{
    private readonly AppFixture _app;
    public AppLaunchTests(AppFixture app) => _app = app;

    [Fact]
    public async Task LoginPage_RespondsWith200()
    {
        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        var r = await http.GetAsync($"{AppFixture.BaseUrl}/LoginPage");
        Assert.Equal(200, (int)r.StatusCode);
    }

    [Fact]
    public async Task AdminCanLogIn_AndReachTicketsList()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(30_000);

        await page.GotoAsync($"{AppFixture.BaseUrl}/Ticket_ListView", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Generate Demo Data", new() { Timeout = 60_000 });
    }
}
