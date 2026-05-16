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

## Decimal & string criteria

- Decimal comparisons in `CriteriaOperator.Parse` need explicit `m` suffix or boxed `decimal`: `Parse("[Amount] > ?", 100m)` not `100`.
- String comparisons are **case-sensitive** on SQL Server unless the column's collation is CI. Use `StartsWith`, `Contains`, `Like`, or wrap in `Upper(...)` when the data isn't already normalized.

## Custom filter UI in a ListView

The standard filter row + filter-builder dialog usually suffice. Before building custom UI, check whether a `DashboardView` with a `DashboardFilterController` solves it. Build custom only when you need behavior the built-ins can't express — and put the custom UI in a Razor component invoked via a `PopupWindowShowAction`.
