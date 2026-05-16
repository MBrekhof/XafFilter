using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Themes;

/// <summary>
/// One-shot screenshot generator used to populate the README's "Screenshots" section.
/// Skip by default — flip the Skip attribute off and run it once when README images need refresh.
/// </summary>
[Collection("App")]
public class ReadmeScreenshots
{
    private readonly AppFixture _app;
    public ReadmeScreenshots(AppFixture app) => _app = app;

    private static string ReadmeDir => Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "screenshots"));

    private static async Task<IPage> OpenTicketsListAsync(IBrowserContext ctx)
    {
        var page = await ctx.NewPageAsync();
        await page.SetViewportSizeAsync(1400, 900);
        page.SetDefaultNavigationTimeout(30_000);
        await page.GotoAsync($"{AppFixture.BaseUrl}/Ticket_ListView", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Generate Demo Data", new() { Timeout = 60_000 });
        // Wait a beat so DxGrid has fully rendered its column headers and data.
        await page.WaitForTimeoutAsync(750);
        return page;
    }

    private static async Task ClickColumnFunnelAsync(IPage page, string columnHeaderText)
    {
        // Clicking a new column's funnel auto-closes any open popup, so we don't need an
        // explicit "close" step between screenshots.
        await page.EvaluateAsync(@"(name) => {
            for (const h of document.querySelectorAll('.dxbl-grid-header')) {
                if (h.textContent.includes(name)) {
                    h.querySelector('.dxbl-grid-filter-menu-funnel-btn')?.click();
                    return;
                }
            }
        }", columnHeaderText);
        await page.WaitForSelectorAsync(".dxbl-filter-menu-dropdown", new() { Timeout = 10_000 });
        await page.WaitForTimeoutAsync(300);
    }

    [Fact(Skip = "Generates README artifacts — un-skip and run once to refresh docs/screenshots/")]
    public async Task Generate_All_Screenshots()
    {
        Directory.CreateDirectory(ReadmeDir);
        await using var ctx = await _app.NewLoggedInContextAsync();

        // 1. Ticket list with seeded data.
        var page = await OpenTicketsListAsync(ctx);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "01-ticket-list.png") });

        // 2. Generate Demo Data popup.
        await page.GetByRole(AriaRole.Button, new() { Name = "Generate Demo Data" }).ClickAsync();
        await page.WaitForSelectorAsync("text=Generate Demo Data Parameters", new() { Timeout = 10_000 });
        await page.WaitForTimeoutAsync(300);
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "02-generate-demo-data.png") });
        // Cancel out of the popup.
        await page.EvaluateAsync(@"() => {
            for (const b of document.querySelectorAll('[role=""dialog""] button')) {
                if (b.textContent.trim() === 'Cancel') { b.click(); return; }
            }
        }");
        await page.WaitForSelectorAsync("text=Generate Demo Data Parameters", new() { State = WaitForSelectorState.Detached, Timeout = 10_000 });
        await page.WaitForTimeoutAsync(300);

        // 3. DateRange filter (CreatedAt).
        await ClickColumnFunnelAsync(page, "Created At");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "03-filter-daterange.png") });

        //4. NumericRange filter (Priority).
        await ClickColumnFunnelAsync(page, "Priority");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "04-filter-numericrange.png") });

        //5. Wildcard string filter (Subject).
        await ClickColumnFunnelAsync(page, "Subject");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "05-filter-wildcard.png") });

        //6. Enum multi-select filter (Status).
        await ClickColumnFunnelAsync(page, "Status");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "06-filter-enum.png") });

        //7. Bool tri-state filter (Is Resolved).
        await ClickColumnFunnelAsync(page, "Is Resolved");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "07-filter-bool.png") });

        //8. Disable-custom-filter opt-out (LegacyImportId → default DX menu).
        await ClickColumnFunnelAsync(page, "Legacy Import Id");
        await page.ScreenshotAsync(new() { Path = Path.Combine(ReadmeDir, "08-filter-disabled-fallback.png") });
    }
}
