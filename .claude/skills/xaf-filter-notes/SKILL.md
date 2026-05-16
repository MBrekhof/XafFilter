---
name: xaf-filter-notes
description: Domain reference for custom filter, criteria-editor, and query-builder work in DevExpress XAF Blazor Server. Use when implementing or modifying anything involving CriteriaOperator, FilterController, ListView filters, custom ListEditor filter UI, FullTextSearchTargetPropertiesAttribute, FilterByCollectionAttribute, view variants for filtering, or filter persistence.
---

# Custom filter / criteria-editor work in XAF

This project's purpose is custom filter and criteria UI on top of XAF. Treat this skill as the place to land hard-won facts about filtering — extend it as you learn things.

## Core building blocks

- **`CriteriaOperator`** (`DevExpress.Data.Filtering`) — parsed expression tree. Build with `CriteriaOperator.Parse("[Status] = ?", status)` (parameter substitution prevents injection — never string-concat user input).
- **`FilterController`** (`DevExpress.ExpressApp.SystemModule`) — the framework controller that owns the filter actions on a ListView. Subclass or extend by registering a ViewController with `TargetViewType = ViewType.ListView` and customizing in `OnActivated`.
- **`CollectionSourceBase.Criteria`** — the dictionary of named criteria applied to a ListView's collection source. `View.CollectionSource.Criteria["Custom"] = CriteriaOperator.Parse(...)` adds a filter; setting it to `null` removes it.
- **View variants** (`DevExpress.ExpressApp.ViewVariantsModule`) — already registered in `Module.cs`. Lets you predefine named filtered views the user can switch between via the variants action.

## When writing ViewControllers for filtering

Always invoke the `xaf-viewcontroller-patterns` skill — it covers OnActivated/OnDeactivated lifecycle, BoolList for action Active/Enabled, and the ObjectSpace leak pitfalls that bite filter work specifically (custom criteria often hold references to disposed ObjectSpaces).

Filter-specific lifecycle gotchas:
- Subscribe to `View.CollectionSource.CollectionChanged` in `OnActivated`, unsubscribe in `OnDeactivated` — otherwise stale ListViews keep firing filter logic.
- `View.CurrentObjectChanged` fires before criteria reapply — don't read `View.CurrentObject` in a filter-changed handler without a null guard.

## Criteria editor customization

- **`ICriteriaPropertyEditor`** — implement on a property editor when you want the field to participate in the criteria UI with a custom shape.
- **`IComplexCriteriaOperator`** — for criteria nodes the standard parser doesn't render. Register via `CriteriaOperator.RegisterCustomFunction`.
- **`FilterController.FullTextFilterAction`** — the search-box action. Customize its `Execute` to override how free-text turns into a `CriteriaOperator`.

## Persisting filters

XAF stores user filters in the application model (`Model.xafml` for design-time, `ModelDifference` table for runtime per-user). The `ModelDifference` and `ModelDifferenceAspect` entities are already registered in this project's `XafFilterEFCoreDbContext`.

To save a runtime filter programmatically, modify the per-user `ModelDifference` via `IModelApplication` rather than writing the XML directly.

## The 5-step filter-menu contract

XafFilter ships five custom column filter menus, each pairing a `ViewController<ListView>` with a Razor component under `XafFilter.Blazor.Server/Filters/`. Every filter follows the same lifecycle:

1. **Controller.OnActivated** subscribes to `View.ControlsCreated`.
2. **Controller.View_ControlsCreated** iterates `editor.GridDataColumnModels` on the `DxGridListEditor`, skips columns whose `MemberType` doesn't match this filter or whose property has `[DisableCustomFilter]`, then sets `FilterMenuButtonDisplayMode = Always` and assigns a `FilterMenuTemplate` that renders the Razor component.
3. **Razor.OnParametersSet** calls a `CriteriaBuilders.ReadXxx` helper to recover the inputs from `FilterContext.FilterCriteria`.
4. **Razor.OnInputChanged** calls `CriteriaBuilders.BuildXxx` and writes the result back to `FilterContext.FilterCriteria`. The grid re-filters on every change — the popup's **Apply button is disabled** because there are no pending criteria to commit; the popup closes via Cancel / click-outside.
5. **Controller.OnDeactivated** unsubscribes `ControlsCreated`.

All criteria construction lives in `XafFilter.Module/Filters/CriteriaBuilders.cs` — pure helpers, no Blazor dependency, fully unit-testable.

The five filters and their column-type heuristics:

| Filter                  | Targets                          | UI                                  |
|-------------------------|----------------------------------|--------------------------------------|
| DateRangeFilterMenu     | `DateTime`, `DateOnly`           | Two `DxDateEdit` (From/To)           |
| NumericRangeFilterMenu  | All numeric types (`int`, `decimal`, `double`, ...) | Two `DxSpinEdit` with `N0`/`N2` format |
| WildcardStringFilterMenu| `string`                         | Single `DxTextBox` accepting `_` and `%` |
| EnumMultiSelectFilterMenu | enum types                     | `DxListBox` with one checkbox per enum value |
| BoolTriStateFilterMenu  | `bool`                           | `DxRadioGroup` with All / Yes / No   |

## Opting out of custom filters

Apply `[XafFilter.Module.Filters.DisableCustomFilter]` to any property to fall back to the default DevExpress filter menu. Useful for ID columns, technical/legacy fields, and any column where the type-based heuristic picks the wrong filter. The reference implementation is `Ticket.LegacyImportId`.

## Demo data

The Support-Tickets demo BOs in `XafFilter.Module/BusinessObjects/Demo/` are seeded by `DemoDataSeeder` (uses Bogus). The seeder is **deterministic** — `Randomizer.Seed = new Random(42)` — so the same row count produces the same data each run. Keep this seed value if you want Playwright screenshots to remain reproducible.

The `Generate Demo Data` action on the Ticket ListView is the only entry point; there is no auto-seed on first run. The action runs `DemoDataSeeder.Seed(os, parameters)` inside a fresh `IObjectSpace`, calls `CommitChanges()`, and refreshes the host view.

## Navigation-collection gotcha

XAF entities use `ChangeTrackingStrategy.ChangingAndChangedNotificationsWithOriginalValues`, so navigation collections **must** implement `INotifyCollectionChanged`. Initialize with `new ObservableCollection<T>()`, not `new List<T>()` — `List<T>` compiles but throws at `CreateObject<T>()` runtime ("does not implement INotifyCollectionChanged"). This bit Customer.Tickets and Agent.AssignedTickets in early task runs; the in-memory test DbContext uses a different change-tracking strategy and silently passes, so the failure only shows up against the real XAF DbContext.

## Decimal & string criteria

- Decimal comparisons in `CriteriaOperator.Parse` need explicit `m` suffix or boxed `decimal`: `Parse("[Amount] > ?", 100m)` not `100`.
- String comparisons are **case-sensitive** on SQL Server unless the column's collation is CI. Use `StartsWith`, `Contains`, `Like`, or wrap in `Upper(...)` when the data isn't already normalized.

## Custom filter UI in a ListView

The standard filter row + filter-builder dialog usually suffice. Before building custom UI, check whether a `DashboardView` with a `DashboardFilterController` solves it. Build custom only when you need behavior the built-ins can't express — and put the custom UI in a Razor component invoked via a `PopupWindowShowAction`.
