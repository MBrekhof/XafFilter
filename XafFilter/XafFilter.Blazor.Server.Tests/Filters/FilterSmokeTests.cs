using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Filters;

[Collection("App")]
public class FilterSmokeTests
{
    private readonly AppFixture _app;
    public FilterSmokeTests(AppFixture app) => _app = app;

    private async Task<IPage> OpenTicketsListAsync(IBrowserContext ctx)
    {
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(30_000);
        await page.GotoAsync($"{AppFixture.BaseUrl}/Ticket_ListView", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.WaitForSelectorAsync("text=Generate Demo Data", new() { Timeout = 60_000 });
        return page;
    }

    private static async Task OpenColumnFunnelAsync(IPage page, string columnHeaderText)
    {
        await page.EvaluateAsync(@"(name) => {
            for (const h of document.querySelectorAll('.dxbl-grid-header')) {
                if (h.textContent.includes(name)) {
                    h.querySelector('.dxbl-grid-filter-menu-funnel-btn')?.click();
                    return;
                }
            }
        }", columnHeaderText);
        await page.WaitForSelectorAsync(".dxbl-filter-menu-dropdown", new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task CreatedAt_OpensCustomDateRangeMenu()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Created At");

        // Custom DateRangeFilterMenu has two combobox-role text inputs (DxDateEdit).
        Assert.Equal(2, await page.Locator(".dxbl-filter-menu-dropdown input[type='text'][role='combobox']").CountAsync());
        Assert.Equal(1, await page.Locator(".dxbl-filter-menu-dropdown:has-text(\"From\"):has-text(\"To\")").CountAsync());
    }

    [Fact]
    public async Task Priority_OpensCustomNumericRangeMenu()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Priority");

        // Custom NumericRangeFilterMenu has two spinbutton inputs (DxSpinEdit).
        Assert.Equal(2, await page.Locator(".dxbl-filter-menu-dropdown input[role='spinbutton']").CountAsync());
    }

    [Fact]
    public async Task Subject_OpensCustomWildcardMenu()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Subject");

        Assert.Equal(1, await page.Locator(".dxbl-filter-menu-dropdown input[placeholder='e.g. %login%']").CountAsync());
    }

    [Fact]
    public async Task Status_OpensCustomEnumMultiSelectMenu()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Status");

        // EnumMultiSelectFilterMenu shows DxListBox with one option per TicketStatus value.
        var items = await page.Locator(".dxbl-filter-menu-dropdown [role='option']").AllTextContentsAsync();
        var trimmed = items.Select(t => t.Trim()).ToArray();
        Assert.Contains("New", trimmed);
        Assert.Contains("InProgress", trimmed);
        Assert.Contains("Resolved", trimmed);
    }

    [Fact]
    public async Task IsResolved_OpensCustomBoolTriStateMenu()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Is Resolved");

        // BoolTriStateFilterMenu shows DxRadioGroup with All / Yes / No.
        var labels = await page.Locator(".dxbl-filter-menu-dropdown label, .dxbl-filter-menu-dropdown .dxbl-radio-text").AllTextContentsAsync();
        var trimmed = labels.Select(t => t.Trim()).Where(t => t.Length > 0).ToArray();
        Assert.Contains("All", trimmed);
        Assert.Contains("Yes", trimmed);
        Assert.Contains("No", trimmed);
    }

    [Fact]
    public async Task LegacyImportId_FallsBackToDefaultFilterMenu()
    {
        // [DisableCustomFilter] on Ticket.LegacyImportId should prevent NumericRangeFilterMenu
        // from being wired up — the default DevExpress filter menu (checkbox list with
        // "Select All") should appear instead.
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await OpenTicketsListAsync(ctx);
        await OpenColumnFunnelAsync(page, "Legacy Import Id");

        Assert.Equal(0, await page.Locator(".dxbl-filter-menu-dropdown input[role='spinbutton']").CountAsync());
        Assert.True(await page.Locator(".dxbl-filter-menu-dropdown:has-text(\"Select All\")").CountAsync() > 0,
            "Default filter menu should show a Select All option.");
    }
}
