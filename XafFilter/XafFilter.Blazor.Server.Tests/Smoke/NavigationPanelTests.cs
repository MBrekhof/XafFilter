using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Smoke;

// Regression guard for commit ba5a322: before that commit, Agent/Customer/Ticket
// lacked [DefaultClassOptions] / [NavigationItem("Support")], so the entities were
// not reachable through the nav sidebar. Existing smoke tests deep-link via URL
// (e.g. /Ticket_ListView) and missed it. This test clicks through the panel.
[Collection("App")]
public class NavigationPanelTests
{
    private readonly AppFixture _app;
    public NavigationPanelTests(AppFixture app) => _app = app;

    [Fact]
    public async Task SupportGroup_ListsDemoBOs_AndTicketNavClickOpensListView()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(30_000);

        // Land on Customer list first so the Ticket nav-click below actually navigates.
        await page.GotoAsync($"{AppFixture.BaseUrl}/Customer_ListView", new() { WaitUntil = WaitUntilState.NetworkIdle });

        // Wait until the nav tree is rendered. DevExtreme's accordion group container
        // exposes role="group" via AOM but the DOM element is a plain div, so anchor
        // on the treeitem anchors (which DO carry role="treeitem" literally).
        var ticketLink = page.Locator("a[role='treeitem'][href='Ticket_ListView']");
        await ticketLink.WaitForAsync(new() { Timeout = 30_000 });

        // [NavigationItem("Support")] must place all three demo BOs under a "Support"
        // group header. The header is part of a dxbl-accordion-group container.
        Assert.True(
            await page.Locator(".dxbl-accordion-group:has-text('Support')").CountAsync() > 0,
            "Expected a Support nav group from [NavigationItem(\"Support\")].");

        // [DefaultClassOptions] is what surfaces each demo BO as a nav tree item.
        var navTexts = (await page.Locator("a[role='treeitem']").AllTextContentsAsync())
            .Select(t => t.Trim())
            .ToArray();
        Assert.Contains("Agent", navTexts);
        Assert.Contains("Customer", navTexts);
        Assert.Contains("Ticket", navTexts);

        await ticketLink.ClickAsync();
        await page.WaitForURLAsync(u => u.EndsWith("/Ticket_ListView"), new() { Timeout = 15_000 });

        // Generate Demo Data is unique to the Ticket list view (GenerateDemoDataController).
        await page.WaitForSelectorAsync("text=Generate Demo Data", new() { Timeout = 30_000 });
    }
}
