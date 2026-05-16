# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Custom filter / criteria-editor UI built on DevExpress XAF (Blazor Server). The eventual deliverable is reusable filter and query-builder functionality that can be dropped into other XAF apps.

## Stack pins

- .NET 10 (`net10.0`) — both projects target this.
- DevExpress 25.2.5 — all `DevExpress.*` packages stay on this version together. Bump them as a set, not individually.
- EF Core 10 with `DevExpress.ExpressApp.EFCore`. SQL Server LocalDB (`(localdb)\\mssqllocaldb`, catalog `XafFilter`).

## Solution layout

- `XafFilter.slnx` (new XML solution format, not `.sln`) — open with VS 2022 17.10+ or `dotnet` CLI.
- `XafFilter/XafFilter.Module` — business objects, `XafFilterEFCoreDbContext`, `Updater.cs`, `Module.cs`.
- `XafFilter/XafFilter.Blazor.Server` — host: `Startup.cs`, `Program.cs`, `BlazorApplication.cs`, `appsettings.json`.

## Build configurations

Three configurations exist: `Debug`, `Release`, **`EasyTest`**. `EasyTest` is non-standard — it sets the `EASYTEST` preprocessor symbol which switches the connection string in `Startup.cs` to `EasyTestConnectionString` (catalog `XafFilterEasyTest`). Use it only when running DevExpress EasyTest UI tests.

Build a specific config: `dotnet build XafFilter.slnx -c EasyTest`.

## Adding business objects

When you add a new persistent type, you must do both of:
1. Add a `DbSet<T>` to `XafFilter.Module/BusinessObjects/XafFilterDbContext.cs` (the class is `XafFilterEFCoreDbContext`).
2. Add `AdditionalExportedTypes.Add(typeof(T));` in `XafFilter.Module/Module.cs` constructor so XAF picks the type up at runtime.

Forgetting step 2 results in the type compiling fine but never appearing in the XAF model — silent failure.

When authoring EF Core entities for XAF, always invoke the `xaf-efcore-entities` skill first (virtual properties, no OwnsOne, BaseObjectInt, decimal precision, aggregated relationships — all of these silently break XAF if wrong).

## Running locally

Blazor Server host project: `XafFilter/XafFilter.Blazor.Server`. Run with `dotnet run --project XafFilter/XafFilter.Blazor.Server` or use the `/run-xaf` skill (which adds the port-free check and HTTP health probe).

First run creates the LocalDB database via `Updater.cs`. Default seeded login is the standard XAF template (`Admin` / blank password — change before sharing).

## Skills available

- `/run-xaf` — start the Blazor Server app with port-free check + HTTP health probe, and stop it cleanly.
- `/xaf-filter-notes` — domain reference for custom filter / criteria editor work in XAF.
