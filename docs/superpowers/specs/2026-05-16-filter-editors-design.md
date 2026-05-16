# XafFilter — Custom Column Filter Editors

**Date:** 2026-05-16
**Status:** Approved, ready for implementation planning

## Goal

Incorporate the custom Blazor grid filter editors from `wlncentral` into `XafFilter`, expand the set with three additional filter types, and add demo business objects plus a parametrised dummy-data generator that exercises every filter in a realistic way.

The deliverable is a reusable filter-editor layer that:

1. Auto-applies custom `FilterMenuTemplate`s to grid columns based on member type.
2. Supports an opt-out attribute so individual properties can fall back to the default DevExpress filter menu.
3. Keeps all criteria-building logic in pure helpers that are unit-testable without a Blazor host.
4. Ships with a Support-Tickets demo domain (Ticket, Agent, Customer) and a manual data-generator action.
5. Has xUnit tests for the criteria builders and Playwright smoke tests for the rendered filter UI.

## Stack baseline

- .NET 10 (`net10.0`) for all four projects.
- DevExpress 25.2.5 across all `DevExpress.*` packages — bump together if at all.
- EF Core 10 with `DevExpress.ExpressApp.EFCore`. SQL Server LocalDB (catalog `XafFilter`).
- New NuGet dependency: **Bogus** — used by the demo-data seeder for realistic names/companies/subjects.

## Filter contract

Every filter implementation follows the same five-step contract. Documenting it explicitly so the five filters stay symmetric and a sixth filter (future) can follow the same shape.

```
1. Controller.OnActivated         → subscribe to View.ControlsCreated
2. Controller.View_ControlsCreated → iterate editor.GridDataColumnModels
                                     - skip if column type doesn't match this filter
                                     - skip if member has [DisableCustomFilter]
                                     - skip if FieldName is empty (computed/template columns)
                                     - set FilterMenuButtonDisplayMode = Always
                                     - set FilterMenuTemplate → render the Razor component
3. Razor.OnParametersSet           → ReadXxx(FilterContext.FilterCriteria) → populate inputs
4. Razor.OnInputChanged            → BuildXxx(...) → FilterContext.FilterCriteria = newCriteria
5. Controller.OnDeactivated        → unsubscribe ControlsCreated
```

Steps 3 and 4 delegate to pure helpers in `XafFilter.Module/Filters/CriteriaBuilders.cs`. The Razor components stay thin: render, call helper, write back.

## The five filters

| Filter | Triggers on member type | Empty input | Both inputs filled | One-sided input |
|---|---|---|---|---|
| **DateRange** | `DateTime`/`DateOnly` (+ nullable) | `FilterCriteria = null` | `BetweenOperator(prop, from, endOfDay(to))` | `BinaryOperator >= from` or `<= endOfDay(to)` |
| **NumericRange** | `byte/short/int/long/decimal/double/float` (+ unsigned & nullable) | `null` | `BetweenOperator(prop, from, to)` after `Convert.ChangeType` to column type | `BinaryOperator >= from` or `<= to` |
| **WildcardString** | `string` | `null` | n/a (single input) | `FunctionOperator(FunctionOperatorType.Like, prop, term)` — raw `_` and `%` pass through |
| **EnumMultiSelect** | `Enum` (+ nullable) | `null` | `InOperator(prop, OperandValue[] selected)` | If all values selected → `null` |
| **BoolTriState** | `bool`/`bool?` | All (default) → `null` | n/a | `BinaryOperator == true` or `== false` |

### Per-filter quirks each helper handles

- **Range inversion** — both range filters auto-correct `From > To` by snapping the other end.
- **End-of-day padding** — DateRange pads `To` to `23:59:59.999...` so rows with non-zero time portion on that day aren't excluded. Direct port from `wlncentral`.
- **Numeric casting** — NumericRange casts the picker's `decimal?` back to the column's exact CLR type via `Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture)`. Without this the SQL provider may refuse the comparison.
- **Wildcard literal escapes** — none. Raw `LIKE` semantics. A user wanting a literal `%` types `[%]` (standard SQL LIKE bracket-escape). Whitespace-only input is treated as empty.
- **Round-trip parsing** — every `BuildXxx` has a matching `ReadXxx` that recovers the inputs from an existing `CriteriaOperator`. Required for the menu to remember its state when reopened. If the existing criteria doesn't match the expected shape, `ReadXxx` returns "all empty" (no throw).
- **Opt-out attribute** — all five controllers honor `[DisableCustomFilter]` on the property.

## Project layout

```
XafFilter.Module/                                ← platform-agnostic
├─ Filters/
│  ├─ CriteriaBuilders.cs                        ← pure helpers (testable)
│  └─ DisableCustomFilterAttribute.cs            ← opt-out marker
├─ BusinessObjects/
│  ├─ XafFilterDbContext.cs                      ← + DbSets for demo BOs
│  └─ Demo/
│     ├─ Ticket.cs
│     ├─ Agent.cs
│     ├─ Customer.cs
│     ├─ TicketStatus.cs        (enum)
│     ├─ TicketSeverity.cs      (enum)
│     └─ GenerateDemoDataParameters.cs   ← NonPersistentBaseObject for the action
├─ Controllers/
│  └─ GenerateDemoDataController.cs              ← PopupWindowShowAction
├─ DemoData/
│  └─ DemoDataSeeder.cs                          ← pure seeder, uses Bogus
├─ DatabaseUpdate/
│  └─ Updater.cs                                 ← + seed Admin permissions for new BOs
└─ Module.cs                                     ← + AdditionalExportedTypes for new BOs

XafFilter.Blazor.Server/                         ← Blazor host
└─ Filters/
   ├─ Controllers/
   │  ├─ DateRangeFilterMenuController.cs
   │  ├─ NumericRangeFilterMenuController.cs
   │  ├─ WildcardStringFilterMenuController.cs
   │  ├─ EnumMultiSelectFilterMenuController.cs
   │  └─ BoolTriStateFilterMenuController.cs
   └─ Components/
      ├─ DateRangeFilterMenu.razor
      ├─ NumericRangeFilterMenu.razor
      ├─ WildcardStringFilterMenu.razor
      ├─ EnumMultiSelectFilterMenu.razor
      └─ BoolTriStateFilterMenu.razor

XafFilter.Module.Tests/                          ← NEW xUnit project, net10.0
└─ Filters/
   ├─ DateRangeCriteriaTests.cs
   ├─ NumericRangeCriteriaTests.cs
   ├─ WildcardStringCriteriaTests.cs
   ├─ EnumMultiSelectCriteriaTests.cs
   ├─ BoolTriStateCriteriaTests.cs
└─ DemoData/
   └─ DemoDataSeederTests.cs

XafFilter.Blazor.Server.Tests/                   ← NEW Playwright project, net10.0
└─ FilterSmokeTests.cs
```

Both test projects are added to `XafFilter.slnx`. They build under `Debug` and `Release` only — they skip the `EasyTest` configuration.

### Why this layout

- Criteria-building logic in `XafFilter.Module` is platform-agnostic and unit-testable.
- Razor components are thin: render, call helper, write back.
- Five controllers share the same shape but are not extracted into a base class on day one (YAGNI; only 5 instances). If a 6th filter type appears later, extract a `ColumnFilterMenuController<TPredicate>` base then.
- Demo BOs in `Module` are reusable from any future host (a Windows host, an API host, etc.).

## Demo business objects

All persistent BOs inherit `DevExpress.Persistent.BaseImpl.EF.BaseObject` (Guid PK) for consistency with the existing `ApplicationUser` pattern. Every property is `virtual` to satisfy XAF/EF Core change tracking per the `xaf-efcore-entities` skill.

### `Customer.cs`

```csharp
public class Customer : BaseObject {
    public virtual string Name { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual string? Company { get; set; }
    public virtual bool IsVip { get; set; }                          // ← BoolTriState
    public virtual DateTime CreatedAt { get; set; }                  // ← DateRange
    public virtual IList<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

### `Agent.cs`

```csharp
public class Agent : BaseObject {
    public virtual string DisplayName { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual bool IsActive { get; set; }                       // ← BoolTriState
    public virtual int HoursPerWeek { get; set; }                    // ← NumericRange (int)
    public virtual IList<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}
```

### `Ticket.cs` — primary filter target

```csharp
public class Ticket : BaseObject {
    public virtual string Subject { get; set; } = string.Empty;      // ← WildcardString
    public virtual string? Description { get; set; }                 // ← WildcardString
    public virtual DateTime CreatedAt { get; set; }                  // ← DateRange
    public virtual DateTime? ClosedAt { get; set; }                  // ← DateRange (nullable)
    public virtual TicketStatus Status { get; set; }                 // ← EnumMultiSelect
    public virtual TicketSeverity Severity { get; set; }             // ← EnumMultiSelect
    public virtual int Priority { get; set; }                        // ← NumericRange (int)

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal HoursSpent { get; set; }                  // ← NumericRange (decimal, N2)

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal BillableRate { get; set; }                // ← NumericRange (decimal, N2)

    public virtual bool IsResolved { get; set; }                     // ← BoolTriState
    public virtual bool IsBillable { get; set; }                     // ← BoolTriState

    public virtual Customer? Customer { get; set; }
    public virtual Agent? AssignedAgent { get; set; }

    [DisableCustomFilter]                                            // ← exercises opt-out
    public virtual int LegacyImportId { get; set; }
}
```

### Enums

```csharp
public enum TicketStatus   { New, InProgress, Waiting, Resolved, Closed, Cancelled }
public enum TicketSeverity { Low, Medium, High, Critical }
```

### `GenerateDemoDataParameters.cs` — non-persistent

```csharp
[DomainComponent]
public class GenerateDemoDataParameters : NonPersistentBaseObject {
    public virtual int RowCount { get; set; } = 500;
    public virtual DateTime DateFrom { get; set; } = DateTime.Today.AddMonths(-6);
    public virtual DateTime DateTo { get; set; } = DateTime.Today;
    public virtual bool ClearExistingFirst { get; set; }
}
```

### Wire-up

In `XafFilterEFCoreDbContext.cs`:

```csharp
public DbSet<Customer> Customers { get; set; }
public DbSet<Agent> Agents { get; set; }
public DbSet<Ticket> Tickets { get; set; }
```

In `Module.cs` constructor, add `AdditionalExportedTypes.Add(typeof(...))` for `Customer`, `Agent`, `Ticket`, `GenerateDemoDataParameters`. Enums don't need explicit registration — they're picked up via the `Ticket` registration.

`Updater.cs` adds Admin role permissions (read/write/delete/navigate) for `Ticket`, `Customer`, `Agent`, `GenerateDemoDataParameters`. The `.AddNonPersistent()` call in `Startup.cs` (line 71) already handles non-persistent ObjectSpace for the parameter object — no extra wiring needed.

## Dummy data generator

`GenerateDemoDataController.cs` lives in `XafFilter.Module/Controllers/`. Pure XAF, no Blazor types, so the same action will work from a future Windows host.

### Controller

```csharp
public sealed class GenerateDemoDataController : ViewController<ListView> {
    public GenerateDemoDataController() {
        TargetObjectType = typeof(Ticket);
        var action = new PopupWindowShowAction(this, "GenerateDemoData", PredefinedCategory.Edit) {
            Caption = "Generate Demo Data",
            ImageName = "Action_Refresh",
            SelectionDependencyType = SelectionDependencyType.Independent,
        };
        action.CustomizePopupWindowParams += OnCustomizePopupWindowParams;
        action.Execute += OnExecute;
    }

    void OnCustomizePopupWindowParams(/* args */) {
        var os = Application.CreateObjectSpace(typeof(GenerateDemoDataParameters));
        var p  = os.CreateObject<GenerateDemoDataParameters>();
        e.View = Application.CreateDetailView(os, p);
    }

    void OnExecute(/* args */) {
        var p = (GenerateDemoDataParameters)((DetailView)e.PopupWindow.View).CurrentObject;
        using var os = Application.CreateObjectSpace(typeof(Ticket));
        DemoDataSeeder.Seed(os, p);
        os.CommitChanges();
        View.ObjectSpace.Refresh();
    }
}
```

Subscription/disposal follows `xaf-viewcontroller-patterns`.

### Seeder

`XafFilter.Module/DemoData/DemoDataSeeder.cs`:

```csharp
public static class DemoDataSeeder {
    public static void Seed(IObjectSpace os, GenerateDemoDataParameters p) {
        if (p.ClearExistingFirst) {
            os.Delete(os.GetObjects<Ticket>());
            os.Delete(os.GetObjects<Customer>());
            os.Delete(os.GetObjects<Agent>());
        }
        var faker = new Faker { Random = new Bogus.Randomizer(42) };   // deterministic seed
        var customers = SeedCustomers(os, faker, count: Math.Min(p.RowCount / 10, 100));
        var agents    = SeedAgents(os, faker, count: Math.Min(p.RowCount / 50, 20));
        SeedTickets(os, faker, customers, agents, p);
    }
    // ...
}
```

`RowCount` is hard-capped at **100,000** in the parameter object's validation (default **500**, sensible for first-load demos).

### Data shape

- **Customers**: name + email + company via Bogus `Faker<Customer>`. ~10% `IsVip = true`. `CreatedAt` uniform in past 2 years.
- **Agents**: 5–20 agents. ~80% `IsActive = true`. `HoursPerWeek` in `[8, 40]`.
- **Tickets**:
  - `CreatedAt` uniform in `[DateFrom, DateTo]`.
  - `ClosedAt = CreatedAt + Random(1..30 days)` for 60% of tickets, `null` for 40%.
  - `Status` weighted: 25% New, 20% InProgress, 15% Waiting, 25% Resolved, 10% Closed, 5% Cancelled.
  - `Severity` weighted: 40% Low, 35% Medium, 20% High, 5% Critical.
  - `Priority` = `1..10`.
  - `HoursSpent` = decimal in `[0, 40]`.
  - `BillableRate` = pick from `{75, 95, 125, 150, 200}` decimal.
  - `IsResolved` = derived from `Status in {Resolved, Closed}`.
  - `IsBillable` = 70% true.
  - `Subject` / `Description` = Bogus `Hacker.Phrase()` / `Lorem.Sentences()` with ~15 repeated templates (`"Login fails on Safari"`, `"Payment processing timeout"`, …) so wildcard searches like `%login%` return a meaningful slice.
  - `Customer` and `AssignedAgent` random from seeded sets (10% of tickets have no agent).
  - `LegacyImportId` = sequential `int`.

### Why these specific shapes

- **Weighted enum distributions** — uniform distribution makes enum multi-select look uninteresting (every selection returns ~equal slices). Weighted gives obviously-different result counts.
- **Repeated subject templates** — make wildcard search results meaningful.
- **40% `ClosedAt = null`** — lets DateRange exercise a nullable column.
- **`LegacyImportId`** with `[DisableCustomFilter]` — proves the opt-out attribute works: that column shows the default DevExpress filter menu, not the custom numeric-range one.

## Error handling

| Failure | Where | Strategy |
|---|---|---|
| Column member can't be resolved | Controller `View_ControlsCreated` | Skip via `typeInfo.FindMember(fieldName) is null` guard. |
| Existing `FilterCriteria` doesn't match expected shape | Razor `OnParametersSet → ReadXxx` | Return "all empty"; criteria unchanged until user touches a control. |
| Numeric cast overflows (`decimal 1e20 → int`) | `BuildNumericRange` | Wrap `Convert.ChangeType` in `try/catch OverflowException` → return `null`. |
| Wildcard input is whitespace-only | `BuildWildcard` | Treat as empty → `FilterCriteria = null`. |
| Seeder commit fails | Action `Execute` | `try`/`catch`; surface `"Demo data seeding failed: {ex.Message}"` through XAF's message system. No silent swallow. |

No `Result<T>` pattern, no custom exceptions. XAF's standard error pipeline is sufficient.

## Logging

`appsettings.json` already has `DevExpress.ExpressApp: Information`. The seeder logs one summary line per run via `DevExpress.Persistent.Base.Tracing.Tracer.LogText`:

```
Generated 500 tickets, 50 customers, 10 agents in 1240ms
```

No new logging infrastructure.

## Tests

### `XafFilter.Module.Tests/` (xUnit, mandatory)

References: `XafFilter.Module`, `DevExpress.ExpressApp`, `xunit`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, `Bogus`, `Microsoft.EntityFrameworkCore.InMemory` (for seeder tests).

Per filter (one test class each):

| Test | What it pins down |
|---|---|
| `Build_BothBounds_ReturnsBetweenOperator` | Happy path |
| `Build_OnlyFrom_ReturnsGreaterOrEqual` | One-sided range |
| `Build_OnlyTo_ReturnsLessOrEqual_WithEndOfDayPadding` | DateRange: padding applied |
| `Build_BothNull_ReturnsNull` | Empty input clears filter |
| `Build_InvertedRange_NormalizesEndpoints` | From > To → swap |
| `Read_RoundTrip_RecoversInputs` | `Read(Build(x)) == x` |
| `Build_AllEnumValuesSelected_ReturnsNull` | EnumMultiSelect: full selection = no filter |
| `Build_NoEnumValuesSelected_ReturnsNull` | Empty selection = no filter |
| `Build_NumericCastsToTargetType` | NumericRange: `decimal? → int` round-trips |
| `Build_NumericOverflow_ReturnsNull` | Overflow handled gracefully |
| `Build_WildcardWithUnderscoreAndPercent_PassesThroughLiteral` | Raw `_`/`%` semantics |
| `Build_WildcardWhitespaceOnly_ReturnsNull` | Empty after trim |
| `Build_BoolTriState_True/False/All` | Three-way logic |

`DemoDataSeederTests`:

| Test | What it pins down |
|---|---|
| `Seed_ProducesRequestedRowCount` | `Seed(RowCount=100)` → 100 tickets |
| `Seed_IsDeterministic` | Same `Random(seed: 42)` → identical output across runs |
| `Seed_ClearExistingFirst_RemovesPriorData` | Idempotent re-seed |
| `Seed_LegacyImportIdIsSequential` | `1..N` ordered |

Uses `XafFilterEFCoreDbContext` with `UseInMemoryDatabase` + an `EFCoreObjectSpaceProvider`-style `IObjectSpace` shim.

### `XafFilter.Blazor.Server.Tests/` (Playwright, recommended)

References: `Microsoft.Playwright`, `xunit`, `Microsoft.Playwright.MSTest` (or fixture pattern).

Per global `CLAUDE.md`: `navigationTimeout: 10000`, `waitUntil: 'domcontentloaded'`. Run against a **production build** (`dotnet build -c Release && dotnet run -c Release --no-build`) per global rule.

Test fixture: launch host, wait for `https://localhost:44318/LoginPage` → 200, log in as `Admin`, navigate to Tickets, seed 200 rows via the action, then run the smoke tests.

| Test | What it verifies |
|---|---|
| `DateRange_AppliesAndFiltersGrid` | Filter menu on `CreatedAt`, pick 30-day range, row count drops |
| `NumericRange_AppliesAndFiltersGrid` | `Priority` 5..10, row count drops |
| `WildcardString_Like_FindsRows` | `%login%` in Subject, only matching rows |
| `EnumMultiSelect_PicksSubset` | Status: `New` + `InProgress`, grid only shows those |
| `BoolTriState_FiltersTrue` | `IsResolved = Yes`, only resolved tickets |
| `DisableCustomFilter_FallsBackToDefault` | `LegacyImportId` shows default DX menu, not numeric-range |
| `LightTheme_FilterMenusRender` | Default Fluent Blue: open each filter, screenshot |
| `DarkTheme_FilterMenusRender` | Switch to `Blazing Dark`, repeat — text readable |

Screenshots in `XafFilter.Blazor.Server.Tests/screenshots/`, gitignored.

### Out of scope for this round

- DevExpress's `BetweenOperator` SQL translation correctness.
- Performance benchmarks on 100k rows (we ship the cap, not the benchmark).
- Auth flow tests (covered by XAF's template).
- View Variants pre-defining filters.
- Saving filter state per-user across sessions (XAF's `ModelDifference` already handles this).
- Localization of menu captions ("From", "To", "Yes", "No", "All") — app's `Languages` is `en-US;` only.

## Performance considerations

- **Filter controllers**: `View_ControlsCreated` runs once per ListView activation, iterates ≤ 30 columns typically. Negligible.
- **Seeder**: single `os.CommitChanges()` at end of action. For 100k rows expected to finish in seconds; if it doesn't, future optimization, not this round.
- **Wildcard `Like '%foo%'`**: forces table scan on SQL Server. Acceptable — user choice, demo dataset stays small.

## Follow-ups (post-merge documentation)

- Extend `/xaf-filter-notes` skill with the 5-step filter contract.
- Document `[DisableCustomFilter]` in `Module/Filters/` so it's discoverable.
- Document the deterministic `Random(seed: 42)` choice — keeps Playwright screenshots reproducible.
