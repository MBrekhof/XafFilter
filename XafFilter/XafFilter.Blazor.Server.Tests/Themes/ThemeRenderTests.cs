using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Themes;

[Collection("App")]
public class ThemeRenderTests
{
    private readonly AppFixture _app;
    public ThemeRenderTests(AppFixture app) => _app = app;

    private static readonly string ScreenshotDir =
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "screenshots");

    private static async Task<IPage> NavigateToTicketsAsync(IBrowserContext ctx)
    {
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(30_000);
        await page.GotoAsync($"{AppFixture.BaseUrl}/Ticket_ListView", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Generate Demo Data", new() { Timeout = 60_000 });
        return page;
    }

    private static async Task OpenCreatedAtFunnelAsync(IPage page)
    {
        await page.EvaluateAsync(@"() => {
            for (const h of document.querySelectorAll('.dxbl-grid-header')) {
                if (h.textContent.includes('Created At')) {
                    h.querySelector('.dxbl-grid-filter-menu-funnel-btn')?.click();
                    return;
                }
            }
        }");
        await page.WaitForSelectorAsync(".dxbl-filter-menu-dropdown", new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task LightTheme_FilterMenuRenders()
    {
        Directory.CreateDirectory(ScreenshotDir);
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await NavigateToTicketsAsync(ctx);
        await OpenCreatedAtFunnelAsync(page);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "light-daterange.png") });

        // Two date inputs render. The popup background should not be near-black (sanity contrast check).
        Assert.Equal(2, await page.Locator(".dxbl-filter-menu-dropdown input[type='text'][role='combobox']").CountAsync());

        var bgBrightness = await page.EvaluateAsync<double>(@"() => {
            const el = document.querySelector('.dxbl-filter-menu-dropdown');
            const bg = getComputedStyle(el).backgroundColor;
            const m = bg.match(/rgb[a]?\(([^)]+)\)/);
            if (!m) return 255;
            const [r, g, b] = m[1].split(',').map(Number);
            return 0.299 * r + 0.587 * g + 0.114 * b;
        }");
        Assert.True(bgBrightness > 128, $"Light theme popup should have light background (perceived brightness > 128); got {bgBrightness:F0}.");
    }
}
