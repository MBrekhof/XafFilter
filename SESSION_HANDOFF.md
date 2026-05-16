# Session Handoff — 2026-05-16

## Current state

- **Branch:** `main` (no worktree, no remote — solo project, history is local-only)
- **HEAD:** `df54ddd` (plan fix for Task 14 adaptations)
- **Working tree:** clean
- **Build:** 0 warnings, 0 errors across whole solution
- **Tests:** 55 passing (xUnit, criteria builders + DemoDataSeeder)

## What's done

Following the implementation plan at `docs/superpowers/plans/2026-05-16-filter-editors.md`.

| Plan task | Status | Notes |
|---|---|---|
| Task 1: xUnit test project scaffold | ✅ | Required `<Using Include="Xunit" />` not in spec — plan corrected |
| Task 2: `DisableCustomFilterAttribute` | ✅ | |
| Task 3: `CriteriaBuilders.DateRange` | ✅ | Required `#nullable enable` directive — plan corrected |
| Task 4: `CriteriaBuilders.NumericRange` | ✅ | |
| Task 5: `CriteriaBuilders.WildcardString` | ✅ | Uses obsolete `BinaryOperatorType.Like` (only way to get raw SQL LIKE in DX 25.2.5) — `#pragma warning disable CS0618` in place, justifying comment added |
| Task 6: `CriteriaBuilders.EnumMultiSelect` | ✅ | |
| Task 7: `CriteriaBuilders.BoolTriState` | ✅ | All 5 filter types complete; class is ~180 lines |
| Task 8: `TicketStatus` + `TicketSeverity` enums | ✅ | |
| Task 9: `Customer` BO | ✅ | `#nullable enable` added post-hoc |
| Task 10: `Agent` BO | ✅ | `#nullable enable` added post-hoc |
| Task 11: `Ticket` BO | ✅ | All 14 properties present; `[DisableCustomFilter]` on `LegacyImportId` |
| Task 12: `GenerateDemoDataParameters` | ✅ | Spec was wrong: `NonPersistentBaseObject` is in `DevExpress.ExpressApp`, no `[NonPersistent]` attribute needed — plan corrected |
| Task 13: Register demo BOs in DbContext + Module | ✅ | |
| Task 14: Bogus + `DemoDataSeeder` | ✅ | Real-world adaptations: `RandomSeed` const (collision with `Seed()` method), `file sealed class SeederTestDbContext` for in-memory tests (XAF change-tracking strategy incompatible with EF InMemory), `UseXafCalculatedProperties()` required on DbContextOptions |

## Next up

**Task 15: `GenerateDemoDataController` action** (`docs/superpowers/plans/2026-05-16-filter-editors.md` — search for "Task 15: GenerateDemoDataController action"). This wires the seeder into a `PopupWindowShowAction` on the Ticket ListView so a user can click "Generate Demo Data", pick row count + date range, and seed the database from the UI.

After Task 15: Tasks 16–20 add the five filter Razor components + controllers (DateRange, NumericRange, WildcardString, EnumMultiSelect, BoolTriState). Then Tasks 21–23 add Playwright smoke tests. Task 24 documents everything in the `/xaf-filter-notes` skill.

## How to resume

This work is using `superpowers:subagent-driven-development` to dispatch one implementer subagent per plan task, then a spec-compliance reviewer subagent, then a code-quality reviewer subagent. Dispatch flow:

1. Mark the next task (#23 / Task 15) as `in_progress` in the task list.
2. Dispatch an implementer subagent (`general-purpose`, `haiku` for mechanical tasks like Task 15, upgrade to `sonnet` if the task involves integration concerns like Task 14 did) using the full task text from the plan plus scene-setting context (current HEAD, what's done, important constraints).
3. When implementer reports DONE, dispatch the spec-compliance reviewer with the same task text and the implementer's commit SHA.
4. When spec review passes, dispatch the code-quality reviewer with BASE_SHA + HEAD_SHA.
5. Fix any issues by sending the implementer back via `SendMessage` (use the agentId returned from the original Agent call).
6. Mark complete, move to next task.

For trivial single-file tasks (enums, simple BOs), inline `Read` + `git show` verification is acceptable in place of formal subagent reviews — keeps overhead down.

## Spec deviations encountered (lessons learned, encoded in plan)

- `<ImplicitUsings>` does NOT include `Xunit` — need explicit `<Using Include="Xunit" />` in test csproj.
- New source files in `XafFilter.Module` need `#nullable enable` directive (project doesn't enable nullable globally; existing legacy code would warn if it did).
- `FunctionOperatorType.Like` does not exist in DevExpress 25.2.5 — only `BinaryOperatorType.Like` (obsolete) supports SQL LIKE with `_` and `%`. Suppress CS0618 with a comment.
- `NonPersistentBaseObject` is in `DevExpress.ExpressApp`, not `DevExpress.Persistent.BaseImpl`. The `[NonPersistent]` attribute is XPO-only and triggers analyzer warning XAF0025 on EF Core types.
- `EFCoreObjectSpaceProvider` is generic in 25.2.5: `EFCoreObjectSpaceProvider<TDbContext>(builder => …)`. In-memory tests need a private test DbContext (not the production one — XAF's change-tracking strategy is incompatible with EF InMemory).
- Bogus 35.6.1: `f.Date.Past(2)` — positional only, no `years:` named arg.

## Files / commits

```
df54ddd Fix plan: Task 14 adaptations (RandomSeed const, Bogus positional arg)
f2010f0 feat: add Bogus-powered DemoDataSeeder
7da1101 feat: register demo BOs in DbContext and Module
ad9b265 Fix plan: NonPersistentBaseObject namespace, drop XPO [NonPersistent]
ff184ba feat: add GenerateDemoDataParameters non-persistent BO
65677bf Fix plan: add #nullable enable to BO file templates
438ed7c fix: add #nullable enable to demo BOs to silence CS8632
d7318f7 feat: add Ticket demo BO with DisableCustomFilter on LegacyImportId
39b399e feat: add Agent demo BO
219c5f9 feat: add Customer demo BO
04f3448 feat: add TicketStatus and TicketSeverity enums
313b778 feat: add BoolTriState criteria builder + parser
... (older commits per `git log`)
```
