# Filter Editors Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Port DateRange + NumericRange custom Blazor filter editors from `wlncentral` into `XafFilter`, add three new filter types (WildcardString, EnumMultiSelect, BoolTriState), and ship a Support-Tickets demo domain with a parametrised dummy-data generator.

**Architecture:** Pure criteria-building helpers live in `XafFilter.Module/Filters/CriteriaBuilders.cs` so they're unit-testable without a Blazor host. Razor components in `XafFilter.Blazor.Server/Filters/Components/` are thin (render → call helper → write back). One `ViewController<ListView>` per filter swaps in the `FilterMenuTemplate` for matching columns. `[DisableCustomFilter]` lets individual properties opt out.

**Tech Stack:** .NET 10, DevExpress XAF 25.2.5 (Blazor Server), EF Core 10, xUnit (tests), Bogus (demo data), Microsoft.Playwright (UI smoke tests).

**Spec:** [docs/superpowers/specs/2026-05-16-filter-editors-design.md](../specs/2026-05-16-filter-editors-design.md)

---

## Task 1: Create XafFilter.Module.Tests xUnit project

**Files:**
- Create: `XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj`
- Create: `XafFilter/XafFilter.Module.Tests/Smoke/PlaceholderTests.cs`
- Modify: `XafFilter.slnx`

- [ ] **Step 1: Scaffold the xUnit project from CLI**

Run from repo root:
```
dotnet new xunit -n XafFilter.Module.Tests -o XafFilter/XafFilter.Module.Tests -f net10.0
```

Expected: project created. Delete the auto-generated `UnitTest1.cs`.

- [ ] **Step 2: Edit the csproj to add the Module project reference and pin xUnit versions**

Replace the entire contents of `XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.2" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\XafFilter.Module\XafFilter.Module.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Add a placeholder smoke test that proves the test runner is wired**

Create `XafFilter/XafFilter.Module.Tests/Smoke/PlaceholderTests.cs`:

```csharp
namespace XafFilter.Module.Tests.Smoke;

public class PlaceholderTests
{
    [Fact]
    public void TestRunner_IsWired() => Assert.True(true);
}
```

- [ ] **Step 4: Add the test project to the solution**

Edit `XafFilter.slnx`. Append a `<Project>` line under the existing two:

```xml
<Project Path="XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj" />
```

The full Projects block should be:

```xml
<Project Path="XafFilter/XafFilter.Blazor.Server/XafFilter.Blazor.Server.csproj" />
<Project Path="XafFilter/XafFilter.Module/XafFilter.Module.csproj" />
<Project Path="XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj" />
```

- [ ] **Step 5: Verify the solution still builds and the placeholder test passes**

Run:
```
dotnet build XafFilter.slnx
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj
```

Expected: build succeeds, 1 test passes.

- [ ] **Step 6: Commit**

```
git add XafFilter/XafFilter.Module.Tests XafFilter.slnx
git commit -m "test: scaffold XafFilter.Module.Tests xUnit project"
```

---

## Task 2: Add DisableCustomFilter attribute

**Files:**
- Create: `XafFilter/XafFilter.Module/Filters/DisableCustomFilterAttribute.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/DisableCustomFilterAttributeTests.cs`

- [ ] **Step 1: Write the failing test**

Create `XafFilter/XafFilter.Module.Tests/Filters/DisableCustomFilterAttributeTests.cs`:

```csharp
using System.Reflection;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class DisableCustomFilterAttributeTests
{
    private class Sample
    {
        [DisableCustomFilter]
        public int Marked { get; set; }
        public int Unmarked { get; set; }
    }

    [Fact]
    public void Attribute_IsDetected_OnMarkedProperty()
    {
        var prop = typeof(Sample).GetProperty(nameof(Sample.Marked))!;
        Assert.NotNull(prop.GetCustomAttribute<DisableCustomFilterAttribute>());
    }

    [Fact]
    public void Attribute_IsAbsent_OnUnmarkedProperty()
    {
        var prop = typeof(Sample).GetProperty(nameof(Sample.Unmarked))!;
        Assert.Null(prop.GetCustomAttribute<DisableCustomFilterAttribute>());
    }

    [Fact]
    public void Attribute_TargetsPropertiesOnly()
    {
        var usage = typeof(DisableCustomFilterAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        Assert.Equal(AttributeTargets.Property, usage.ValidOn);
        Assert.False(usage.AllowMultiple);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DisableCustomFilterAttributeTests
```

Expected: compile error — `DisableCustomFilterAttribute` does not exist.

- [ ] **Step 3: Implement the attribute**

Create `XafFilter/XafFilter.Module/Filters/DisableCustomFilterAttribute.cs`:

```csharp
namespace XafFilter.Module.Filters;

/// <summary>
/// Apply to a property to opt that column out of the auto-applied custom filter menu.
/// The default DevExpress filter menu is used instead.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class DisableCustomFilterAttribute : Attribute
{
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DisableCustomFilterAttributeTests
```

Expected: 3 tests pass.

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Module/Filters XafFilter/XafFilter.Module.Tests/Filters
git commit -m "feat: add DisableCustomFilter opt-out attribute"
```

---

## Task 3: CriteriaBuilders.DateRange (Build + Read)

**Files:**
- Create: `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/DateRangeCriteriaTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `XafFilter/XafFilter.Module.Tests/Filters/DateRangeCriteriaTests.cs`:

```csharp
using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class DateRangeCriteriaTests
{
    const string Field = "CreatedAt";

    [Fact]
    public void Build_BothNull_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildDateRange(Field, null, null));
    }

    [Fact]
    public void Build_BothBounds_ReturnsBetweenWithEndOfDayPadding()
    {
        var from = new DateTime(2026, 1, 1, 0, 0, 0);
        var to   = new DateTime(2026, 1, 31, 0, 0, 0);
        var expectedEnd = new DateTime(2026, 2, 1).AddTicks(-1);

        var criteria = (BetweenOperator)CriteriaBuilders.BuildDateRange(Field, from, to)!;

        Assert.Equal(Field, ((OperandProperty)criteria.TestExpression).PropertyName);
        Assert.Equal(from,        ((OperandValue)criteria.BeginExpression).Value);
        Assert.Equal(expectedEnd, ((OperandValue)criteria.EndExpression).Value);
    }

    [Fact]
    public void Build_OnlyFrom_ReturnsGreaterOrEqual()
    {
        var from = new DateTime(2026, 1, 1);
        var op = (BinaryOperator)CriteriaBuilders.BuildDateRange(Field, from, null)!;
        Assert.Equal(BinaryOperatorType.GreaterOrEqual, op.OperatorType);
        Assert.Equal(from, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_OnlyTo_ReturnsLessOrEqual_WithEndOfDayPadding()
    {
        var to = new DateTime(2026, 1, 31);
        var expectedEnd = new DateTime(2026, 2, 1).AddTicks(-1);
        var op = (BinaryOperator)CriteriaBuilders.BuildDateRange(Field, null, to)!;
        Assert.Equal(BinaryOperatorType.LessOrEqual, op.OperatorType);
        Assert.Equal(expectedEnd, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_InvertedRange_PreservesInputOrder()
    {
        var from = new DateTime(2026, 1, 31);
        var to   = new DateTime(2026, 1, 1);
        // The helper does not silently swap — it is the component's job to normalize input.
        // The helper accepts the order it was given; document this contract.
        var criteria = (BetweenOperator)CriteriaBuilders.BuildDateRange(Field, from, to)!;
        Assert.Equal(from, ((OperandValue)criteria.BeginExpression).Value);
    }

    [Fact]
    public void Read_BetweenOperator_RecoversBothBounds()
    {
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 1, 31);
        var criteria = CriteriaBuilders.BuildDateRange(Field, from, to);

        var (rFrom, rTo) = CriteriaBuilders.ReadDateRange(criteria, Field);

        Assert.Equal(from, rFrom);
        Assert.NotNull(rTo);
        // Read returns the stored end-of-day-padded value as-is (close-enough fidelity).
        Assert.Equal(new DateTime(2026, 2, 1).AddTicks(-1), rTo);
    }

    [Fact]
    public void Read_OnlyFrom_RecoversFromAndNullTo()
    {
        var from = new DateTime(2026, 1, 1);
        var criteria = CriteriaBuilders.BuildDateRange(Field, from, null);
        var (rFrom, rTo) = CriteriaBuilders.ReadDateRange(criteria, Field);
        Assert.Equal(from, rFrom);
        Assert.Null(rTo);
    }

    [Fact]
    public void Read_OnlyTo_RecoversNullFromAndTo()
    {
        var to = new DateTime(2026, 1, 31);
        var criteria = CriteriaBuilders.BuildDateRange(Field, null, to);
        var (rFrom, rTo) = CriteriaBuilders.ReadDateRange(criteria, Field);
        Assert.Null(rFrom);
        Assert.NotNull(rTo);
    }

    [Fact]
    public void Read_MismatchedFieldName_ReturnsAllNull()
    {
        var criteria = CriteriaBuilders.BuildDateRange("OtherField", new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        var (rFrom, rTo) = CriteriaBuilders.ReadDateRange(criteria, Field);
        Assert.Null(rFrom);
        Assert.Null(rTo);
    }

    [Fact]
    public void Read_UnrelatedCriteria_ReturnsAllNull()
    {
        var criteria = new BinaryOperator(Field, "x", BinaryOperatorType.Equal);
        var (rFrom, rTo) = CriteriaBuilders.ReadDateRange(criteria, Field);
        Assert.Null(rFrom);
        Assert.Null(rTo);
    }
}
```

Note on inversion: per the spec, the **Razor component** normalizes `From > To` before calling the helper. The helper itself is unopinionated. The test `Build_InvertedRange_NormalizesEndpoints` pins down this contract.

- [ ] **Step 2: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DateRangeCriteriaTests
```

Expected: compile error — `CriteriaBuilders` does not exist.

- [ ] **Step 3: Implement DateRange helpers**

Create `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`:

```csharp
#nullable enable
using DevExpress.Data.Filtering;

namespace XafFilter.Module.Filters;

/// <summary>
/// Pure helpers that build and parse <see cref="CriteriaOperator"/> values for each filter type.
/// Razor components in XafFilter.Blazor.Server.Filters.Components delegate to these so the
/// criteria logic stays unit-testable without a Blazor render host.
/// </summary>
public static class CriteriaBuilders
{
    // --- DateRange ---------------------------------------------------------

    public static CriteriaOperator? BuildDateRange(string fieldName, DateTime? from, DateTime? to)
    {
        if (from is null && to is null) return null;
        var prop = new OperandProperty(fieldName);

        if (from is { } f && to is { } t)
        {
            var endInclusive = t.Date.AddDays(1).AddTicks(-1);
            return new BetweenOperator(prop, new OperandValue(f), new OperandValue(endInclusive));
        }
        if (from is { } fOnly)
        {
            return new BinaryOperator(fieldName, fOnly, BinaryOperatorType.GreaterOrEqual);
        }
        var endOnly = to!.Value.Date.AddDays(1).AddTicks(-1);
        return new BinaryOperator(fieldName, endOnly, BinaryOperatorType.LessOrEqual);
    }

    public static (DateTime? From, DateTime? To) ReadDateRange(CriteriaOperator? criteria, string fieldName)
    {
        if (criteria is BetweenOperator bo &&
            (bo.TestExpression as OperandProperty)?.PropertyName == fieldName)
        {
            return ((bo.BeginExpression as OperandValue)?.Value as DateTime?,
                    (bo.EndExpression   as OperandValue)?.Value as DateTime?);
        }
        if (criteria is BinaryOperator bin &&
            (bin.LeftOperand as OperandProperty)?.PropertyName == fieldName)
        {
            var val = (bin.RightOperand as OperandValue)?.Value as DateTime?;
            return bin.OperatorType switch
            {
                BinaryOperatorType.GreaterOrEqual => (val, null),
                BinaryOperatorType.LessOrEqual    => (null, val),
                _ => (null, null),
            };
        }
        return (null, null);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DateRangeCriteriaTests
```

Expected: 9 tests pass.

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs XafFilter/XafFilter.Module.Tests/Filters/DateRangeCriteriaTests.cs
git commit -m "feat: add DateRange criteria builder + parser"
```

---

## Task 4: CriteriaBuilders.NumericRange (Build + Read)

**Files:**
- Modify: `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/NumericRangeCriteriaTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `XafFilter/XafFilter.Module.Tests/Filters/NumericRangeCriteriaTests.cs`:

```csharp
using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class NumericRangeCriteriaTests
{
    const string Field = "Priority";

    [Fact]
    public void Build_BothNull_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildNumericRange(Field, null, null, typeof(int)));
    }

    [Fact]
    public void Build_BothBounds_ReturnsBetweenWithCastValues()
    {
        var op = (BetweenOperator)CriteriaBuilders.BuildNumericRange(Field, 1m, 10m, typeof(int))!;
        Assert.Equal(1,  ((OperandValue)op.BeginExpression).Value);
        Assert.Equal(10, ((OperandValue)op.EndExpression).Value);
        Assert.IsType<int>(((OperandValue)op.BeginExpression).Value);
    }

    [Fact]
    public void Build_DecimalTarget_KeepsDecimalType()
    {
        var op = (BetweenOperator)CriteriaBuilders.BuildNumericRange(Field, 1.5m, 9.75m, typeof(decimal))!;
        Assert.IsType<decimal>(((OperandValue)op.BeginExpression).Value);
        Assert.Equal(1.5m, ((OperandValue)op.BeginExpression).Value);
    }

    [Fact]
    public void Build_OnlyFrom_ReturnsGreaterOrEqual()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildNumericRange(Field, 5m, null, typeof(int))!;
        Assert.Equal(BinaryOperatorType.GreaterOrEqual, op.OperatorType);
        Assert.Equal(5, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_OnlyTo_ReturnsLessOrEqual()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildNumericRange(Field, null, 5m, typeof(int))!;
        Assert.Equal(BinaryOperatorType.LessOrEqual, op.OperatorType);
        Assert.Equal(5, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_NumericOverflow_ReturnsNull()
    {
        // decimal.MaxValue cannot be cast to int — helper should swallow and return null.
        Assert.Null(CriteriaBuilders.BuildNumericRange(Field, decimal.MaxValue, decimal.MaxValue, typeof(int)));
    }

    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(short))]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(decimal))]
    public void Build_AllSupportedNumericTypes_Succeed(Type targetType)
    {
        var op = CriteriaBuilders.BuildNumericRange(Field, 1m, 10m, targetType);
        Assert.NotNull(op);
    }

    [Fact]
    public void Read_BetweenOperator_RecoversBothBounds()
    {
        var criteria = CriteriaBuilders.BuildNumericRange(Field, 1m, 10m, typeof(int));
        var (from, to) = CriteriaBuilders.ReadNumericRange(criteria, Field);
        Assert.Equal(1m, from);
        Assert.Equal(10m, to);
    }

    [Fact]
    public void Read_OnlyFrom_RecoversFromAndNullTo()
    {
        var criteria = CriteriaBuilders.BuildNumericRange(Field, 5m, null, typeof(int));
        var (from, to) = CriteriaBuilders.ReadNumericRange(criteria, Field);
        Assert.Equal(5m, from);
        Assert.Null(to);
    }

    [Fact]
    public void Read_MismatchedFieldName_ReturnsAllNull()
    {
        var criteria = CriteriaBuilders.BuildNumericRange("Other", 1m, 10m, typeof(int));
        var (from, to) = CriteriaBuilders.ReadNumericRange(criteria, Field);
        Assert.Null(from);
        Assert.Null(to);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~NumericRangeCriteriaTests
```

Expected: compile error — `BuildNumericRange` / `ReadNumericRange` don't exist.

- [ ] **Step 3: Implement NumericRange helpers**

Append to `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`, after the DateRange section, inside the same class:

```csharp
    // --- NumericRange ------------------------------------------------------

    public static CriteriaOperator? BuildNumericRange(string fieldName, decimal? from, decimal? to, Type targetType)
    {
        if (from is null && to is null) return null;

        object? Cast(decimal value)
        {
            try { return Convert.ChangeType(value, targetType, System.Globalization.CultureInfo.InvariantCulture); }
            catch (OverflowException) { return null; }
            catch (InvalidCastException) { return null; }
        }

        var prop = new OperandProperty(fieldName);

        if (from is { } f && to is { } t)
        {
            var fCast = Cast(f);
            var tCast = Cast(t);
            if (fCast is null || tCast is null) return null;
            return new BetweenOperator(prop, new OperandValue(fCast), new OperandValue(tCast));
        }
        if (from is { } fOnly)
        {
            var cast = Cast(fOnly);
            return cast is null ? null : new BinaryOperator(fieldName, cast, BinaryOperatorType.GreaterOrEqual);
        }
        var toCast = Cast(to!.Value);
        return toCast is null ? null : new BinaryOperator(fieldName, toCast, BinaryOperatorType.LessOrEqual);
    }

    public static (decimal? From, decimal? To) ReadNumericRange(CriteriaOperator? criteria, string fieldName)
    {
        if (criteria is BetweenOperator bo &&
            (bo.TestExpression as OperandProperty)?.PropertyName == fieldName)
        {
            return (ToDecimal((bo.BeginExpression as OperandValue)?.Value),
                    ToDecimal((bo.EndExpression   as OperandValue)?.Value));
        }
        if (criteria is BinaryOperator bin &&
            (bin.LeftOperand as OperandProperty)?.PropertyName == fieldName)
        {
            var val = ToDecimal((bin.RightOperand as OperandValue)?.Value);
            return bin.OperatorType switch
            {
                BinaryOperatorType.GreaterOrEqual => (val, null),
                BinaryOperatorType.LessOrEqual    => (null, val),
                _ => (null, null),
            };
        }
        return (null, null);
    }

    static decimal? ToDecimal(object? v)
        => v is null ? null : Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture);
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~NumericRangeCriteriaTests
```

Expected: 10 tests pass.

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs XafFilter/XafFilter.Module.Tests/Filters/NumericRangeCriteriaTests.cs
git commit -m "feat: add NumericRange criteria builder + parser"
```

---

## Task 5: CriteriaBuilders.WildcardString (Build + Read)

**Files:**
- Modify: `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/WildcardStringCriteriaTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `XafFilter/XafFilter.Module.Tests/Filters/WildcardStringCriteriaTests.cs`:

```csharp
using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class WildcardStringCriteriaTests
{
    const string Field = "Subject";

    [Fact]
    public void Build_Null_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildWildcard(Field, null));
    }

    [Fact]
    public void Build_Empty_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildWildcard(Field, ""));
    }

    [Fact]
    public void Build_WhitespaceOnly_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildWildcard(Field, "   "));
    }

#pragma warning disable CS0618 // BinaryOperatorType.Like is obsolete but still the only way to get raw SQL LIKE.

    [Fact]
    public void Build_Term_ReturnsLikeBinaryOperator()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildWildcard(Field, "%login%")!;
        Assert.Equal(BinaryOperatorType.Like, op.OperatorType);
        Assert.Equal(Field, ((OperandProperty)op.LeftOperand).PropertyName);
        Assert.Equal("%login%", ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_WildcardWithUnderscoreAndPercent_PassesThroughLiteral()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildWildcard(Field, "a_b%c")!;
        // Raw LIKE semantics: _ and % are passed through unchanged.
        Assert.Equal("a_b%c", ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Read_LikeBinaryOperator_RecoversTerm()
    {
        var criteria = CriteriaBuilders.BuildWildcard(Field, "%login%");
        var term = CriteriaBuilders.ReadWildcard(criteria, Field);
        Assert.Equal("%login%", term);
    }

    [Fact]
    public void Read_MismatchedFieldName_ReturnsNull()
    {
        var criteria = CriteriaBuilders.BuildWildcard("Other", "%login%");
        Assert.Null(CriteriaBuilders.ReadWildcard(criteria, Field));
    }

    [Fact]
    public void Read_UnrelatedCriteria_ReturnsNull()
    {
        var criteria = new BinaryOperator(Field, "x", BinaryOperatorType.Equal);
        Assert.Null(CriteriaBuilders.ReadWildcard(criteria, Field));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~WildcardStringCriteriaTests
```

Expected: compile error — `BuildWildcard` / `ReadWildcard` don't exist.

- [ ] **Step 3: Implement Wildcard helpers**

Append inside `CriteriaBuilders`:

```csharp
    // --- WildcardString ----------------------------------------------------

    // DevExpress 25.2.5 only exposes full SQL LIKE pattern matching (with _ and % wildcards)
    // through the obsolete BinaryOperatorType.Like. The replacement FunctionOperatorType members
    // (Contains, StartsWith, EndsWith) treat _ and % as literal characters, so they cannot meet
    // the spec's raw-LIKE requirement. We suppress CS0618 narrowly until DevExpress restores a
    // non-obsolete equivalent.
    public static CriteriaOperator? BuildWildcard(string fieldName, string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;
#pragma warning disable CS0618 // BinaryOperatorType.Like is obsolete but still the only way to get raw SQL LIKE.
        return new BinaryOperator(fieldName, term, BinaryOperatorType.Like);
#pragma warning restore CS0618
    }

    public static string? ReadWildcard(CriteriaOperator? criteria, string fieldName)
    {
#pragma warning disable CS0618 // BinaryOperatorType.Like is obsolete but still the only way to get raw SQL LIKE.
        if (criteria is BinaryOperator bin &&
            bin.OperatorType == BinaryOperatorType.Like &&
            (bin.LeftOperand as OperandProperty)?.PropertyName == fieldName)
        {
            return (bin.RightOperand as OperandValue)?.Value as string;
        }
#pragma warning restore CS0618
        return null;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~WildcardStringCriteriaTests
```

Expected: 8 tests pass.

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs XafFilter/XafFilter.Module.Tests/Filters/WildcardStringCriteriaTests.cs
git commit -m "feat: add WildcardString (LIKE) criteria builder + parser"
```

---

## Task 6: CriteriaBuilders.EnumMultiSelect (Build + Read)

**Files:**
- Modify: `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/EnumMultiSelectCriteriaTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `XafFilter/XafFilter.Module.Tests/Filters/EnumMultiSelectCriteriaTests.cs`:

```csharp
using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class EnumMultiSelectCriteriaTests
{
    const string Field = "Status";
    enum Status { New, InProgress, Closed }

    [Fact]
    public void Build_NoValuesSelected_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildEnumIn(Field, Array.Empty<object>(), typeof(Status)));
    }

    [Fact]
    public void Build_AllValuesSelected_ReturnsNull()
    {
        var all = Enum.GetValues<Status>().Cast<object>().ToArray();
        Assert.Null(CriteriaBuilders.BuildEnumIn(Field, all, typeof(Status)));
    }

    [Fact]
    public void Build_SubsetSelected_ReturnsInOperator()
    {
        var values = new object[] { Status.New, Status.InProgress };
        var op = (InOperator)CriteriaBuilders.BuildEnumIn(Field, values, typeof(Status))!;
        Assert.Equal(Field, ((OperandProperty)op.LeftOperand).PropertyName);
        Assert.Equal(2, op.Operands.Count);
    }

    [Fact]
    public void Read_InOperator_RecoversValues()
    {
        var values = new object[] { Status.New, Status.Closed };
        var criteria = CriteriaBuilders.BuildEnumIn(Field, values, typeof(Status));
        var recovered = CriteriaBuilders.ReadEnumIn(criteria, Field).ToArray();
        Assert.Equal(2, recovered.Length);
        Assert.Contains(Status.New,    recovered.Cast<Status>());
        Assert.Contains(Status.Closed, recovered.Cast<Status>());
    }

    [Fact]
    public void Read_MismatchedFieldName_ReturnsEmpty()
    {
        var criteria = CriteriaBuilders.BuildEnumIn("Other", new object[] { Status.New }, typeof(Status));
        Assert.Empty(CriteriaBuilders.ReadEnumIn(criteria, Field));
    }

    [Fact]
    public void Read_Null_ReturnsEmpty()
    {
        Assert.Empty(CriteriaBuilders.ReadEnumIn(null, Field));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~EnumMultiSelectCriteriaTests
```

Expected: compile error — `BuildEnumIn` / `ReadEnumIn` don't exist.

- [ ] **Step 3: Implement EnumMultiSelect helpers**

Append inside `CriteriaBuilders`:

```csharp
    // --- EnumMultiSelect ---------------------------------------------------

    public static CriteriaOperator? BuildEnumIn(string fieldName, IReadOnlyCollection<object> selected, Type enumType)
    {
        if (selected.Count == 0) return null;
        var all = Enum.GetValues(enumType).Length;
        if (selected.Count >= all) return null;

        var operands = selected.Select(v => (CriteriaOperator)new OperandValue(v));
        return new InOperator(new OperandProperty(fieldName), operands);
    }

    public static IEnumerable<object> ReadEnumIn(CriteriaOperator? criteria, string fieldName)
    {
        if (criteria is InOperator io &&
            (io.LeftOperand as OperandProperty)?.PropertyName == fieldName)
        {
            foreach (var operand in io.Operands)
            {
                if (operand is OperandValue ov && ov.Value is not null)
                    yield return ov.Value;
            }
        }
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~EnumMultiSelectCriteriaTests
```

Expected: 6 tests pass.

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs XafFilter/XafFilter.Module.Tests/Filters/EnumMultiSelectCriteriaTests.cs
git commit -m "feat: add EnumMultiSelect (In) criteria builder + parser"
```

---

## Task 7: CriteriaBuilders.BoolTriState (Build + Read)

**Files:**
- Modify: `XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs`
- Create: `XafFilter/XafFilter.Module.Tests/Filters/BoolTriStateCriteriaTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `XafFilter/XafFilter.Module.Tests/Filters/BoolTriStateCriteriaTests.cs`:

```csharp
using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class BoolTriStateCriteriaTests
{
    const string Field = "IsResolved";

    [Fact]
    public void Build_All_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.BuildBoolTriState(Field, null));
    }

    [Fact]
    public void Build_True_ReturnsEqualTrue()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildBoolTriState(Field, true)!;
        Assert.Equal(BinaryOperatorType.Equal, op.OperatorType);
        Assert.Equal(true, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Build_False_ReturnsEqualFalse()
    {
        var op = (BinaryOperator)CriteriaBuilders.BuildBoolTriState(Field, false)!;
        Assert.Equal(BinaryOperatorType.Equal, op.OperatorType);
        Assert.Equal(false, ((OperandValue)op.RightOperand).Value);
    }

    [Fact]
    public void Read_True_Roundtrips()
    {
        var criteria = CriteriaBuilders.BuildBoolTriState(Field, true);
        Assert.True(CriteriaBuilders.ReadBoolTriState(criteria, Field));
    }

    [Fact]
    public void Read_False_Roundtrips()
    {
        var criteria = CriteriaBuilders.BuildBoolTriState(Field, false);
        Assert.False(CriteriaBuilders.ReadBoolTriState(criteria, Field));
    }

    [Fact]
    public void Read_Null_ReturnsNull()
    {
        Assert.Null(CriteriaBuilders.ReadBoolTriState(null, Field));
    }

    [Fact]
    public void Read_MismatchedFieldName_ReturnsNull()
    {
        var criteria = CriteriaBuilders.BuildBoolTriState("Other", true);
        Assert.Null(CriteriaBuilders.ReadBoolTriState(criteria, Field));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~BoolTriStateCriteriaTests
```

Expected: compile error — `BuildBoolTriState` / `ReadBoolTriState` don't exist.

- [ ] **Step 3: Implement BoolTriState helpers**

Append inside `CriteriaBuilders`:

```csharp
    // --- BoolTriState ------------------------------------------------------

    public static CriteriaOperator? BuildBoolTriState(string fieldName, bool? value)
    {
        if (value is null) return null;
        return new BinaryOperator(fieldName, value.Value, BinaryOperatorType.Equal);
    }

    public static bool? ReadBoolTriState(CriteriaOperator? criteria, string fieldName)
    {
        if (criteria is BinaryOperator bo &&
            bo.OperatorType == BinaryOperatorType.Equal &&
            (bo.LeftOperand as OperandProperty)?.PropertyName == fieldName &&
            (bo.RightOperand as OperandValue)?.Value is bool b)
        {
            return b;
        }
        return null;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~BoolTriStateCriteriaTests
```

Expected: 7 tests pass.

- [ ] **Step 5: Run the whole suite to confirm everything still passes**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj
```

Expected: 43 tests pass (3 attribute + 9 date + 10 numeric + 8 wildcard + 6 enum + 7 bool).

- [ ] **Step 6: Commit**

```
git add XafFilter/XafFilter.Module/Filters/CriteriaBuilders.cs XafFilter/XafFilter.Module.Tests/Filters/BoolTriStateCriteriaTests.cs
git commit -m "feat: add BoolTriState criteria builder + parser"
```

---

## Task 8: Demo enums (TicketStatus, TicketSeverity)

**Files:**
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/TicketStatus.cs`
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/TicketSeverity.cs`

- [ ] **Step 1: Create the enums**

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/TicketStatus.cs`:

```csharp
namespace XafFilter.Module.BusinessObjects.Demo;

public enum TicketStatus
{
    New,
    InProgress,
    Waiting,
    Resolved,
    Closed,
    Cancelled,
}
```

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/TicketSeverity.cs`:

```csharp
namespace XafFilter.Module.BusinessObjects.Demo;

public enum TicketSeverity
{
    Low,
    Medium,
    High,
    Critical,
}
```

- [ ] **Step 2: Verify the project still builds**

Run:
```
dotnet build XafFilter/XafFilter.Module/XafFilter.Module.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```
git add XafFilter/XafFilter.Module/BusinessObjects/Demo
git commit -m "feat: add TicketStatus and TicketSeverity enums"
```

---

## Task 9: Customer business object

**Files:**
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/Customer.cs`

- [ ] **Step 1: Create the BO**

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/Customer.cs`:

```csharp
#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(Name))]
public class Customer : BaseObject
{
    public virtual string Name { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual string? Company { get; set; }
    public virtual bool IsVip { get; set; }
    public virtual DateTime CreatedAt { get; set; }

    public virtual IList<Ticket> Tickets { get; set; } = new List<Ticket>();
}
```

Note: this file references `Ticket`, which doesn't exist yet. The project will fail to build until Task 11. That's expected.

- [ ] **Step 2: Commit (deferred build check until Ticket exists)**

```
git add XafFilter/XafFilter.Module/BusinessObjects/Demo/Customer.cs
git commit -m "feat: add Customer demo BO"
```

---

## Task 10: Agent business object

**Files:**
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/Agent.cs`

- [ ] **Step 1: Create the BO**

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/Agent.cs`:

```csharp
#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(DisplayName))]
public class Agent : BaseObject
{
    public virtual string DisplayName { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual int HoursPerWeek { get; set; }

    public virtual IList<Ticket> AssignedTickets { get; set; } = new List<Ticket>();
}
```

- [ ] **Step 2: Commit**

```
git add XafFilter/XafFilter.Module/BusinessObjects/Demo/Agent.cs
git commit -m "feat: add Agent demo BO"
```

---

## Task 11: Ticket business object

**Files:**
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/Ticket.cs`

- [ ] **Step 1: Create the BO**

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/Ticket.cs`:

```csharp
#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using XafFilter.Module.Filters;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(Subject))]
public class Ticket : BaseObject
{
    public virtual string Subject { get; set; } = string.Empty;
    public virtual string? Description { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime? ClosedAt { get; set; }
    public virtual TicketStatus Status { get; set; }
    public virtual TicketSeverity Severity { get; set; }
    public virtual int Priority { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal HoursSpent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal BillableRate { get; set; }

    public virtual bool IsResolved { get; set; }
    public virtual bool IsBillable { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Agent? AssignedAgent { get; set; }

    [DisableCustomFilter]
    public virtual int LegacyImportId { get; set; }
}
```

- [ ] **Step 2: Verify the Module project builds**

Run:
```
dotnet build XafFilter/XafFilter.Module/XafFilter.Module.csproj
```

Expected: build succeeds — Customer/Agent/Ticket now resolve.

- [ ] **Step 3: Commit**

```
git add XafFilter/XafFilter.Module/BusinessObjects/Demo/Ticket.cs
git commit -m "feat: add Ticket demo BO with DisableCustomFilter on LegacyImportId"
```

---

## Task 12: GenerateDemoDataParameters non-persistent BO

**Files:**
- Create: `XafFilter/XafFilter.Module/BusinessObjects/Demo/GenerateDemoDataParameters.cs`

- [ ] **Step 1: Create the parameter object**

Create `XafFilter/XafFilter.Module/BusinessObjects/Demo/GenerateDemoDataParameters.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DomainComponent]
public class GenerateDemoDataParameters : NonPersistentBaseObject
{
    [RuleRange(1, 100_000)]
    public virtual int RowCount { get; set; } = 500;

    public virtual DateTime DateFrom { get; set; } = DateTime.Today.AddMonths(-6);

    public virtual DateTime DateTo { get; set; } = DateTime.Today;

    [Description("Delete all existing Tickets, Customers, and Agents before seeding.")]
    public virtual bool ClearExistingFirst { get; set; }
}
```

Notes: `NonPersistentBaseObject` lives in `DevExpress.ExpressApp` (not `DevExpress.Persistent.BaseImpl`). The XPO-style `[NonPersistent]` attribute does not exist in the EF Core flavor — `[DomainComponent]` plus inheriting `NonPersistentBaseObject` is sufficient to mark the type as non-persistent.

- [ ] **Step 2: Verify the Module project builds**

Run:
```
dotnet build XafFilter/XafFilter.Module/XafFilter.Module.csproj
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```
git add XafFilter/XafFilter.Module/BusinessObjects/Demo/GenerateDemoDataParameters.cs
git commit -m "feat: add GenerateDemoDataParameters non-persistent BO"
```

---

## Task 13: Register demo BOs in DbContext and Module.cs

**Files:**
- Modify: `XafFilter/XafFilter.Module/BusinessObjects/XafFilterDbContext.cs`
- Modify: `XafFilter/XafFilter.Module/Module.cs`

- [ ] **Step 1: Add DbSets to XafFilterEFCoreDbContext**

Edit `XafFilter/XafFilter.Module/BusinessObjects/XafFilterDbContext.cs`. After the existing `DbSet<ApplicationUserLoginInfo> UserLoginsInfo` line, add three new DbSets:

```csharp
public DbSet<XafFilter.Module.BusinessObjects.Demo.Customer> Customers { get; set; }
public DbSet<XafFilter.Module.BusinessObjects.Demo.Agent> Agents { get; set; }
public DbSet<XafFilter.Module.BusinessObjects.Demo.Ticket> Tickets { get; set; }
```

- [ ] **Step 2: Register types in Module.cs constructor**

Edit `XafFilter/XafFilter.Module/Module.cs`. After the existing `AdditionalExportedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.EF.HCategory));` line at the end of the constructor, add:

```csharp
AdditionalExportedTypes.Add(typeof(XafFilter.Module.BusinessObjects.Demo.Customer));
AdditionalExportedTypes.Add(typeof(XafFilter.Module.BusinessObjects.Demo.Agent));
AdditionalExportedTypes.Add(typeof(XafFilter.Module.BusinessObjects.Demo.Ticket));
AdditionalExportedTypes.Add(typeof(XafFilter.Module.BusinessObjects.Demo.GenerateDemoDataParameters));
```

- [ ] **Step 3: Verify the whole solution builds**

Run:
```
dotnet build XafFilter.slnx
```

Expected: build succeeds.

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Module/BusinessObjects/XafFilterDbContext.cs XafFilter/XafFilter.Module/Module.cs
git commit -m "feat: register demo BOs in DbContext and Module"
```

---

## Task 14: Add Bogus package and DemoDataSeeder

**Files:**
- Modify: `XafFilter/XafFilter.Module/XafFilter.Module.csproj`
- Create: `XafFilter/XafFilter.Module/DemoData/DemoDataSeeder.cs`
- Create: `XafFilter/XafFilter.Module.Tests/DemoData/DemoDataSeederTests.cs`
- Modify: `XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj`

- [ ] **Step 1: Add the Bogus package to the Module project**

Edit `XafFilter/XafFilter.Module/XafFilter.Module.csproj`. Add this line inside the existing `<ItemGroup>` that contains PackageReferences (e.g., right after the `DevExpress.Persistent.BaseImpl.EFCore` line):

```xml
<PackageReference Include="Bogus" Version="35.6.1" />
```

- [ ] **Step 2: Add EF Core in-memory provider to the test project**

Edit `XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj`. Add this line to the existing `<ItemGroup>` containing PackageReferences:

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.0" />
```

- [ ] **Step 3: Restore packages**

Run:
```
dotnet restore XafFilter.slnx
```

Expected: restore succeeds.

- [ ] **Step 4: Write failing tests**

Create `XafFilter/XafFilter.Module.Tests/DemoData/DemoDataSeederTests.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using XafFilter.Module.BusinessObjects;
using XafFilter.Module.BusinessObjects.Demo;
using XafFilter.Module.DemoData;

namespace XafFilter.Module.Tests.DemoData;

public class DemoDataSeederTests
{
    private static (IObjectSpace os, IDisposable cleanup) CreateInMemoryObjectSpace()
    {
        var options = new DbContextOptionsBuilder<XafFilterEFCoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var provider = new EFCoreObjectSpaceProvider(
            (svc, opts) => opts.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            null);
        var os = provider.CreateObjectSpace();
        return (os, provider);
    }

    private static GenerateDemoDataParameters Params(int rowCount = 100, bool clear = false) => new()
    {
        RowCount = rowCount,
        DateFrom = new DateTime(2026, 1, 1),
        DateTo   = new DateTime(2026, 6, 30),
        ClearExistingFirst = clear,
    };

    [Fact]
    public void Seed_ProducesRequestedRowCount()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;
        DemoDataSeeder.Seed(os, Params(rowCount: 100));
        os.CommitChanges();
        Assert.Equal(100, os.GetObjectsCount(typeof(Ticket), null));
    }

    [Fact]
    public void Seed_IsDeterministic()
    {
        var (os1, c1) = CreateInMemoryObjectSpace();
        var (os2, c2) = CreateInMemoryObjectSpace();
        using var _ = c1;
        using var __ = c2;
        DemoDataSeeder.Seed(os1, Params(50));
        DemoDataSeeder.Seed(os2, Params(50));
        os1.CommitChanges();
        os2.CommitChanges();

        var subjects1 = os1.GetObjects<Ticket>().Select(t => t.Subject).OrderBy(s => s).ToList();
        var subjects2 = os2.GetObjects<Ticket>().Select(t => t.Subject).OrderBy(s => s).ToList();
        Assert.Equal(subjects1, subjects2);
    }

    [Fact]
    public void Seed_ClearExistingFirst_RemovesPriorData()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;

        DemoDataSeeder.Seed(os, Params(50));
        os.CommitChanges();
        Assert.Equal(50, os.GetObjectsCount(typeof(Ticket), null));

        DemoDataSeeder.Seed(os, Params(25, clear: true));
        os.CommitChanges();
        Assert.Equal(25, os.GetObjectsCount(typeof(Ticket), null));
    }

    [Fact]
    public void Seed_LegacyImportIdIsSequential()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;
        DemoDataSeeder.Seed(os, Params(20));
        os.CommitChanges();
        var ids = os.GetObjects<Ticket>().Select(t => t.LegacyImportId).OrderBy(i => i).ToList();
        Assert.Equal(Enumerable.Range(1, 20), ids);
    }
}
```

- [ ] **Step 5: Run tests to verify they fail**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DemoDataSeederTests
```

Expected: compile error — `DemoDataSeeder` does not exist.

- [ ] **Step 6: Implement the seeder**

Create `XafFilter/XafFilter.Module/DemoData/DemoDataSeeder.cs`:

```csharp
using Bogus;
using DevExpress.ExpressApp;
using XafFilter.Module.BusinessObjects.Demo;

namespace XafFilter.Module.DemoData;

public static class DemoDataSeeder
{
    private const int RandomSeed = 42; // const cannot be named "Seed" — collides with the Seed() method below

    private static readonly string[] SubjectTemplates =
    {
        "Login fails on Safari",
        "Login fails on Chrome",
        "Login intermittent",
        "Payment processing timeout",
        "Payment retried but failed",
        "Refund missing",
        "Export to CSV broken",
        "Export hangs on large file",
        "Email notifications delayed",
        "Dashboard slow to load",
        "Report renders blank",
        "Search returns no results",
        "Search returns wrong results",
        "Bulk import fails halfway",
        "Bulk import skips rows silently",
    };

    private static readonly decimal[] BillableRates = { 75m, 95m, 125m, 150m, 200m };

    public static void Seed(IObjectSpace os, GenerateDemoDataParameters p)
    {
        if (p.ClearExistingFirst)
        {
            os.Delete(os.GetObjects<Ticket>());
            os.Delete(os.GetObjects<Customer>());
            os.Delete(os.GetObjects<Agent>());
            os.CommitChanges();
        }

        Randomizer.Seed = new Random(RandomSeed);

        var customers = SeedCustomers(os, count: Math.Max(5, Math.Min(p.RowCount / 10, 100)));
        var agents    = SeedAgents(os,    count: Math.Max(3, Math.Min(p.RowCount / 50, 20)));
        SeedTickets(os, customers, agents, p);
    }

    private static List<Customer> SeedCustomers(IObjectSpace os, int count)
    {
        var faker = new Faker<Customer>()
            .RuleFor(c => c.Name,      f => f.Name.FullName())
            .RuleFor(c => c.Email,     (f, c) => f.Internet.Email(c.Name))
            .RuleFor(c => c.Company,   f => f.Company.CompanyName())
            .RuleFor(c => c.IsVip,     f => f.Random.Bool(weight: 0.10f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2));   // Bogus 35.6.1: positional only, no `years:` named arg

        var result = new List<Customer>(count);
        for (int i = 0; i < count; i++)
        {
            var c = os.CreateObject<Customer>();
            var data = faker.Generate();
            c.Name      = data.Name;
            c.Email     = data.Email;
            c.Company   = data.Company;
            c.IsVip     = data.IsVip;
            c.CreatedAt = data.CreatedAt;
            result.Add(c);
        }
        return result;
    }

    private static List<Agent> SeedAgents(IObjectSpace os, int count)
    {
        var faker = new Faker<Agent>()
            .RuleFor(a => a.DisplayName,   f => f.Name.FullName())
            .RuleFor(a => a.Email,         (f, a) => f.Internet.Email(a.DisplayName))
            .RuleFor(a => a.IsActive,      f => f.Random.Bool(weight: 0.80f))
            .RuleFor(a => a.HoursPerWeek,  f => f.Random.Int(8, 40));

        var result = new List<Agent>(count);
        for (int i = 0; i < count; i++)
        {
            var a = os.CreateObject<Agent>();
            var data = faker.Generate();
            a.DisplayName  = data.DisplayName;
            a.Email        = data.Email;
            a.IsActive     = data.IsActive;
            a.HoursPerWeek = data.HoursPerWeek;
            result.Add(a);
        }
        return result;
    }

    private static void SeedTickets(IObjectSpace os, List<Customer> customers, List<Agent> agents, GenerateDemoDataParameters p)
    {
        var faker = new Faker();
        var statusWeights   = new[] { 0.25f, 0.20f, 0.15f, 0.25f, 0.10f, 0.05f };
        var severityWeights = new[] { 0.40f, 0.35f, 0.20f, 0.05f };

        for (int i = 0; i < p.RowCount; i++)
        {
            var t = os.CreateObject<Ticket>();
            t.Subject          = faker.PickRandom(SubjectTemplates);
            t.Description      = faker.Lorem.Sentence(wordCount: faker.Random.Int(8, 25));
            t.CreatedAt        = faker.Date.Between(p.DateFrom, p.DateTo);
            t.Status           = faker.Random.WeightedRandom(Enum.GetValues<TicketStatus>(),   statusWeights);
            t.Severity         = faker.Random.WeightedRandom(Enum.GetValues<TicketSeverity>(), severityWeights);
            t.Priority         = faker.Random.Int(1, 10);
            t.HoursSpent       = Math.Round(faker.Random.Decimal(0, 40), 2);
            t.BillableRate     = faker.PickRandom(BillableRates);
            t.IsResolved       = t.Status is TicketStatus.Resolved or TicketStatus.Closed;
            t.IsBillable       = faker.Random.Bool(weight: 0.70f);
            t.ClosedAt         = faker.Random.Bool(weight: 0.60f) ? t.CreatedAt.AddDays(faker.Random.Int(1, 30)) : null;
            t.Customer         = faker.PickRandom(customers);
            t.AssignedAgent    = faker.Random.Bool(weight: 0.90f) ? faker.PickRandom(agents) : null;
            t.LegacyImportId   = i + 1;
        }
    }
}
```

- [ ] **Step 7: Run tests to verify they pass**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj --filter FullyQualifiedName~DemoDataSeederTests
```

Expected: 4 tests pass.

- [ ] **Step 8: Run the full suite**

Run:
```
dotnet test XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj
```

Expected: 47 tests pass.

- [ ] **Step 9: Commit**

```
git add XafFilter/XafFilter.Module/XafFilter.Module.csproj XafFilter/XafFilter.Module/DemoData XafFilter/XafFilter.Module.Tests/XafFilter.Module.Tests.csproj XafFilter/XafFilter.Module.Tests/DemoData
git commit -m "feat: add Bogus-powered DemoDataSeeder"
```

---

## Task 15: GenerateDemoDataController action

**Files:**
- Create: `XafFilter/XafFilter.Module/Controllers/GenerateDemoDataController.cs`

- [ ] **Step 1: Create the controller**

Create `XafFilter/XafFilter.Module/Controllers/GenerateDemoDataController.cs`:

```csharp
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.Persistent.Base;
using XafFilter.Module.BusinessObjects.Demo;
using XafFilter.Module.DemoData;

namespace XafFilter.Module.Controllers;

public sealed class GenerateDemoDataController : ViewController<ListView>
{
    private readonly PopupWindowShowAction _action;

    public GenerateDemoDataController()
    {
        TargetObjectType = typeof(Ticket);

        _action = new PopupWindowShowAction(this, "GenerateDemoData", PredefinedCategory.Edit)
        {
            Caption = "Generate Demo Data",
            ImageName = "Action_Refresh",
            SelectionDependencyType = SelectionDependencyType.Independent,
            PaintStyle = ActionItemPaintStyle.CaptionAndImage,
        };

        _action.CustomizePopupWindowParams += OnCustomizePopupWindowParams;
        _action.Execute += OnExecute;
    }

    private void OnCustomizePopupWindowParams(object? sender, CustomizePopupWindowParamsEventArgs e)
    {
        var os = Application.CreateObjectSpace(typeof(GenerateDemoDataParameters));
        var p  = os.CreateObject<GenerateDemoDataParameters>();
        e.View = Application.CreateDetailView(os, p);
    }

    private void OnExecute(object? sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var parameters = (GenerateDemoDataParameters)e.PopupWindow.View.CurrentObject;

        using var os = Application.CreateObjectSpace(typeof(Ticket));
        var localParams = new GenerateDemoDataParameters
        {
            RowCount           = parameters.RowCount,
            DateFrom           = parameters.DateFrom,
            DateTo             = parameters.DateTo,
            ClearExistingFirst = parameters.ClearExistingFirst,
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        DemoDataSeeder.Seed(os, localParams);
        os.CommitChanges();
        sw.Stop();

        Tracing.Tracer.LogText(
            $"DemoDataSeeder: generated {localParams.RowCount} tickets in {sw.ElapsedMilliseconds}ms"
        );

        View.ObjectSpace.Refresh();
    }
}
```

- [ ] **Step 2: Verify the whole solution builds**

Run:
```
dotnet build XafFilter.slnx
```

Expected: build succeeds.

- [ ] **Step 3: Run the app and verify the action exists and works**

Free port 44318 if anything is listening:
```
netstat -ano | grep :44318
```

Then start the host in the background:
```
dotnet run --project XafFilter/XafFilter.Blazor.Server
```

Wait until `curl -k -s -o /dev/null -w "%{http_code}" --max-time 5 https://localhost:44318/LoginPage` returns `200`.

Then in a browser:
1. Open `https://localhost:44318` (accept the dev cert warning if needed).
2. Log in as `Admin` (blank password).
3. Navigate to the **Ticket** list. Confirm the **Generate Demo Data** button appears in the Edit category of the toolbar.
4. Click it. The popup should show RowCount=500, DateFrom (6 months ago), DateTo (today), ClearExistingFirst=unchecked.
5. Set RowCount to **50**, click OK.
6. The list should refresh and show 50 tickets with realistic subjects/customers.

Stop the app:
```
taskkill //PID <pid> //F //T
```

Verify port is free:
```
netstat -ano | grep :44318
```

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Module/Controllers
git commit -m "feat: add Generate Demo Data popup action for Ticket list"
```

---

## Task 16: DateRangeFilterMenu Razor component + controller

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Components/DateRangeFilterMenu.razor`
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/DateRangeFilterMenuController.cs`

- [ ] **Step 1: Create the Razor component**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Components/DateRangeFilterMenu.razor`:

```razor
@using DevExpress.Blazor
@using DevExpress.Data.Filtering
@using XafFilter.Module.Filters

<DxFormLayout CssClass="p-2" ItemCaptionAlignment="ItemCaptionAlignment.All">
    <DxFormLayoutItem Caption="From" ColSpanSm="12">
        <DxDateEdit T="DateTime?"
                    Date="StartDate"
                    DateChanged="StartDate_Changed"
                    ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto" />
    </DxFormLayoutItem>
    <DxFormLayoutItem Caption="To" ColSpanSm="12">
        <DxDateEdit T="DateTime?"
                    Date="EndDate"
                    DateChanged="EndDate_Changed"
                    ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto" />
    </DxFormLayoutItem>
</DxFormLayout>

@code {
    [Parameter, EditorRequired]
    public GridDataColumnFilterMenuTemplateContext FilterContext { get; set; } = default!;

    DateTime? StartDate { get; set; }
    DateTime? EndDate { get; set; }

    protected override void OnParametersSet()
    {
        var (from, to) = CriteriaBuilders.ReadDateRange(FilterContext.FilterCriteria, FilterContext.DataColumn.FieldName);
        StartDate = from;
        EndDate = to;
    }

    void StartDate_Changed(DateTime? v)
    {
        StartDate = v;
        if (StartDate is { } s && EndDate is { } e && s > e) EndDate = s;
        ApplyCriteria();
    }

    void EndDate_Changed(DateTime? v)
    {
        EndDate = v;
        if (StartDate is { } s && EndDate is { } e && s > e) StartDate = e;
        ApplyCriteria();
    }

    void ApplyCriteria()
    {
        FilterContext.FilterCriteria = CriteriaBuilders.BuildDateRange(
            FilterContext.DataColumn.FieldName, StartDate, EndDate);
    }
}
```

- [ ] **Step 2: Create the controller**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/DateRangeFilterMenuController.cs`:

```csharp
using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using XafFilter.Blazor.Server.Filters.Components;
using XafFilter.Module.Filters;

namespace XafFilter.Blazor.Server.Filters.Controllers;

/// <summary>
/// Replaces the default column filter menu with a date-range picker on every
/// DateTime / DateOnly column across all ListViews that use DxGridListEditor,
/// unless the property is marked with [DisableCustomFilter].
/// </summary>
public sealed class DateRangeFilterMenuController : ViewController<ListView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.ControlsCreated += View_ControlsCreated;
    }

    protected override void OnDeactivated()
    {
        View.ControlsCreated -= View_ControlsCreated;
        base.OnDeactivated();
    }

    private void View_ControlsCreated(object? sender, EventArgs e)
    {
        if (View.Editor is not DxGridListEditor editor) return;

        var typeInfo = View.ObjectTypeInfo;

        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var member = typeInfo.FindMember(fieldName);
            if (member is null) continue;

            var propInfo = member.Owner.Type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (propInfo?.GetCustomAttribute<DisableCustomFilterAttribute>() is not null) continue;

            var t = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
            if (t != typeof(DateTime) && t != typeof(DateOnly)) continue;

            column.FilterMenuButtonDisplayMode = GridFilterMenuButtonDisplayMode.Always;
            column.FilterMenuTemplate = context => builder =>
            {
                builder.OpenComponent<DateRangeFilterMenu>(0);
                builder.AddAttribute(1, nameof(DateRangeFilterMenu.FilterContext), context);
                builder.CloseComponent();
            };
        }
    }
}
```

- [ ] **Step 3: Verify the solution builds**

Run:
```
dotnet build XafFilter.slnx
```

Expected: build succeeds.

- [ ] **Step 4: Manual smoke test in browser**

Start the host:
```
dotnet run --project XafFilter/XafFilter.Blazor.Server
```

Wait for health check:
```
curl -k -s -o /dev/null -w "%{http_code}" --max-time 5 https://localhost:44318/LoginPage
```

Then in a browser:
1. Log in as `Admin`.
2. If not already seeded, click **Generate Demo Data** → 200 rows.
3. On the Ticket list, click the funnel icon on the **CreatedAt** column. The popup should show "From" + "To" date pickers (NOT the default filter dropdown).
4. Pick a 30-day window. The grid should filter immediately.
5. Reopen the menu — the picked dates should still be there (round-trip works).
6. Clear both dates with the clear button — filter clears, all rows return.

Stop the app:
```
taskkill //PID <pid> //F //T
```

- [ ] **Step 5: Commit**

```
git add XafFilter/XafFilter.Blazor.Server/Filters
git commit -m "feat: add DateRange filter menu + controller"
```

---

## Task 17: NumericRangeFilterMenu Razor component + controller

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Components/NumericRangeFilterMenu.razor`
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/NumericRangeFilterMenuController.cs`

- [ ] **Step 1: Create the Razor component**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Components/NumericRangeFilterMenu.razor`:

```razor
@using DevExpress.Blazor
@using DevExpress.Data.Filtering
@using XafFilter.Module.Filters

<DxFormLayout CssClass="p-2" ItemCaptionAlignment="ItemCaptionAlignment.All">
    <DxFormLayoutItem Caption="From" ColSpanSm="12">
        <DxSpinEdit T="decimal?"
                    Value="From"
                    ValueChanged="@((decimal? v) => From_Changed(v))"
                    Format="@NumberFormat"
                    ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto" />
    </DxFormLayoutItem>
    <DxFormLayoutItem Caption="To" ColSpanSm="12">
        <DxSpinEdit T="decimal?"
                    Value="To"
                    ValueChanged="@((decimal? v) => To_Changed(v))"
                    Format="@NumberFormat"
                    ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto" />
    </DxFormLayoutItem>
</DxFormLayout>

@code {
    [Parameter, EditorRequired]
    public GridDataColumnFilterMenuTemplateContext FilterContext { get; set; } = default!;
    [Parameter, EditorRequired]
    public Type TargetType { get; set; } = default!;
    [Parameter] public bool IsInteger { get; set; }

    decimal? From { get; set; }
    decimal? To { get; set; }

    string NumberFormat => IsInteger ? "N0" : "N2";

    protected override void OnParametersSet()
    {
        var (from, to) = CriteriaBuilders.ReadNumericRange(FilterContext.FilterCriteria, FilterContext.DataColumn.FieldName);
        From = from;
        To = to;
    }

    void From_Changed(decimal? v)
    {
        From = v;
        if (From is { } f && To is { } t && f > t) To = f;
        ApplyCriteria();
    }

    void To_Changed(decimal? v)
    {
        To = v;
        if (From is { } f && To is { } t && f > t) From = t;
        ApplyCriteria();
    }

    void ApplyCriteria()
    {
        FilterContext.FilterCriteria = CriteriaBuilders.BuildNumericRange(
            FilterContext.DataColumn.FieldName, From, To, TargetType);
    }
}
```

- [ ] **Step 2: Create the controller**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/NumericRangeFilterMenuController.cs`:

```csharp
using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using XafFilter.Blazor.Server.Filters.Components;
using XafFilter.Module.Filters;

namespace XafFilter.Blazor.Server.Filters.Controllers;

public sealed class NumericRangeFilterMenuController : ViewController<ListView>
{
    private static readonly HashSet<Type> IntegerTypes = new()
    {
        typeof(byte), typeof(sbyte),
        typeof(short), typeof(ushort),
        typeof(int), typeof(uint),
        typeof(long), typeof(ulong),
    };

    private static readonly HashSet<Type> NumericTypes = new(IntegerTypes)
    {
        typeof(decimal), typeof(double), typeof(float),
    };

    protected override void OnActivated()
    {
        base.OnActivated();
        View.ControlsCreated += View_ControlsCreated;
    }

    protected override void OnDeactivated()
    {
        View.ControlsCreated -= View_ControlsCreated;
        base.OnDeactivated();
    }

    private void View_ControlsCreated(object? sender, EventArgs e)
    {
        if (View.Editor is not DxGridListEditor editor) return;

        var typeInfo = View.ObjectTypeInfo;

        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var member = typeInfo.FindMember(fieldName);
            if (member is null) continue;

            var propInfo = member.Owner.Type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (propInfo?.GetCustomAttribute<DisableCustomFilterAttribute>() is not null) continue;

            var targetType = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
            if (!NumericTypes.Contains(targetType)) continue;

            var isInteger = IntegerTypes.Contains(targetType);

            column.FilterMenuButtonDisplayMode = GridFilterMenuButtonDisplayMode.Always;
            column.FilterMenuTemplate = context => builder =>
            {
                builder.OpenComponent<NumericRangeFilterMenu>(0);
                builder.AddAttribute(1, nameof(NumericRangeFilterMenu.FilterContext), context);
                builder.AddAttribute(2, nameof(NumericRangeFilterMenu.TargetType), targetType);
                builder.AddAttribute(3, nameof(NumericRangeFilterMenu.IsInteger), isInteger);
                builder.CloseComponent();
            };
        }
    }
}
```

- [ ] **Step 3: Build and manual smoke**

Run:
```
dotnet build XafFilter.slnx
dotnet run --project XafFilter/XafFilter.Blazor.Server
```

Health check, then in browser:
1. Log in, navigate to Tickets.
2. Click funnel on **Priority** column → numeric From/To inputs with `N0` format.
3. Set 5..10, grid filters.
4. Click funnel on **HoursSpent** → numeric From/To with `N2` format.
5. Click funnel on **LegacyImportId** → **default DevExpress filter menu** (not the custom one) — because of `[DisableCustomFilter]`.

Stop the app.

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Blazor.Server/Filters/Components/NumericRangeFilterMenu.razor XafFilter/XafFilter.Blazor.Server/Filters/Controllers/NumericRangeFilterMenuController.cs
git commit -m "feat: add NumericRange filter menu + controller"
```

---

## Task 18: WildcardStringFilterMenu Razor component + controller

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Components/WildcardStringFilterMenu.razor`
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/WildcardStringFilterMenuController.cs`

- [ ] **Step 1: Create the Razor component**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Components/WildcardStringFilterMenu.razor`:

```razor
@using DevExpress.Blazor
@using DevExpress.Data.Filtering
@using XafFilter.Module.Filters

<DxFormLayout CssClass="p-2" ItemCaptionAlignment="ItemCaptionAlignment.All">
    <DxFormLayoutItem Caption="Contains (use _ and % wildcards)" ColSpanSm="12">
        <DxTextBox Text="@Term"
                   TextChanged="@((string v) => Term_Changed(v))"
                   ClearButtonDisplayMode="DataEditorClearButtonDisplayMode.Auto"
                   NullText="e.g. %login%" />
    </DxFormLayoutItem>
</DxFormLayout>

@code {
    [Parameter, EditorRequired]
    public GridDataColumnFilterMenuTemplateContext FilterContext { get; set; } = default!;

    string? Term { get; set; }

    protected override void OnParametersSet()
    {
        Term = CriteriaBuilders.ReadWildcard(FilterContext.FilterCriteria, FilterContext.DataColumn.FieldName);
    }

    void Term_Changed(string v)
    {
        Term = v;
        FilterContext.FilterCriteria = CriteriaBuilders.BuildWildcard(FilterContext.DataColumn.FieldName, Term);
    }
}
```

- [ ] **Step 2: Create the controller**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/WildcardStringFilterMenuController.cs`:

```csharp
using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using XafFilter.Blazor.Server.Filters.Components;
using XafFilter.Module.Filters;

namespace XafFilter.Blazor.Server.Filters.Controllers;

public sealed class WildcardStringFilterMenuController : ViewController<ListView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.ControlsCreated += View_ControlsCreated;
    }

    protected override void OnDeactivated()
    {
        View.ControlsCreated -= View_ControlsCreated;
        base.OnDeactivated();
    }

    private void View_ControlsCreated(object? sender, EventArgs e)
    {
        if (View.Editor is not DxGridListEditor editor) return;

        var typeInfo = View.ObjectTypeInfo;

        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var member = typeInfo.FindMember(fieldName);
            if (member is null) continue;

            var propInfo = member.Owner.Type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (propInfo?.GetCustomAttribute<DisableCustomFilterAttribute>() is not null) continue;

            if (member.MemberType != typeof(string)) continue;

            column.FilterMenuButtonDisplayMode = GridFilterMenuButtonDisplayMode.Always;
            column.FilterMenuTemplate = context => builder =>
            {
                builder.OpenComponent<WildcardStringFilterMenu>(0);
                builder.AddAttribute(1, nameof(WildcardStringFilterMenu.FilterContext), context);
                builder.CloseComponent();
            };
        }
    }
}
```

- [ ] **Step 3: Build and manual smoke**

Build + run, then in browser:
1. Tickets list, funnel on **Subject** column → single text input with placeholder.
2. Type `%login%`, grid filters to login-related rows.
3. Type `payment_timeout` (note the `_`) — should match "Payment timeout" with any single char in place of `_`.

Stop the app.

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Blazor.Server/Filters/Components/WildcardStringFilterMenu.razor XafFilter/XafFilter.Blazor.Server/Filters/Controllers/WildcardStringFilterMenuController.cs
git commit -m "feat: add WildcardString filter menu + controller"
```

---

## Task 19: EnumMultiSelectFilterMenu Razor component + controller

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Components/EnumMultiSelectFilterMenu.razor`
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/EnumMultiSelectFilterMenuController.cs`

- [ ] **Step 1: Create the Razor component**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Components/EnumMultiSelectFilterMenu.razor`:

```razor
@using DevExpress.Blazor
@using DevExpress.Data.Filtering
@using XafFilter.Module.Filters

<div class="p-2" style="min-width: 220px;">
    <DxListBox Data="@AllValues"
               SelectionMode="ListBoxSelectionMode.Multiple"
               ShowCheckboxes="true"
               Values="@Selected"
               ValuesChanged="@((IEnumerable<object> v) => Selection_Changed(v))" />
</div>

@code {
    [Parameter, EditorRequired]
    public GridDataColumnFilterMenuTemplateContext FilterContext { get; set; } = default!;
    [Parameter, EditorRequired]
    public Type EnumType { get; set; } = default!;

    object[] AllValues { get; set; } = Array.Empty<object>();
    IEnumerable<object> Selected { get; set; } = Array.Empty<object>();

    protected override void OnParametersSet()
    {
        AllValues = Enum.GetValues(EnumType).Cast<object>().ToArray();
        Selected = CriteriaBuilders.ReadEnumIn(FilterContext.FilterCriteria, FilterContext.DataColumn.FieldName).ToArray();
    }

    void Selection_Changed(IEnumerable<object> values)
    {
        Selected = values.ToArray();
        FilterContext.FilterCriteria = CriteriaBuilders.BuildEnumIn(
            FilterContext.DataColumn.FieldName, (IReadOnlyCollection<object>)Selected, EnumType);
    }
}
```

- [ ] **Step 2: Create the controller**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/EnumMultiSelectFilterMenuController.cs`:

```csharp
using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using XafFilter.Blazor.Server.Filters.Components;
using XafFilter.Module.Filters;

namespace XafFilter.Blazor.Server.Filters.Controllers;

public sealed class EnumMultiSelectFilterMenuController : ViewController<ListView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.ControlsCreated += View_ControlsCreated;
    }

    protected override void OnDeactivated()
    {
        View.ControlsCreated -= View_ControlsCreated;
        base.OnDeactivated();
    }

    private void View_ControlsCreated(object? sender, EventArgs e)
    {
        if (View.Editor is not DxGridListEditor editor) return;

        var typeInfo = View.ObjectTypeInfo;

        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var member = typeInfo.FindMember(fieldName);
            if (member is null) continue;

            var propInfo = member.Owner.Type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (propInfo?.GetCustomAttribute<DisableCustomFilterAttribute>() is not null) continue;

            var t = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
            if (!t.IsEnum) continue;

            column.FilterMenuButtonDisplayMode = GridFilterMenuButtonDisplayMode.Always;
            column.FilterMenuTemplate = context => builder =>
            {
                builder.OpenComponent<EnumMultiSelectFilterMenu>(0);
                builder.AddAttribute(1, nameof(EnumMultiSelectFilterMenu.FilterContext), context);
                builder.AddAttribute(2, nameof(EnumMultiSelectFilterMenu.EnumType), t);
                builder.CloseComponent();
            };
        }
    }
}
```

- [ ] **Step 3: Build and manual smoke**

Build + run, then in browser:
1. Tickets list, funnel on **Status** column → list of checkboxes for each TicketStatus value.
2. Check `New` + `InProgress` — grid filters to those rows only.
3. Check all 6 statuses — filter clears (all selected = no filter).
4. Funnel on **Severity** → same UX for TicketSeverity.

Stop the app.

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Blazor.Server/Filters/Components/EnumMultiSelectFilterMenu.razor XafFilter/XafFilter.Blazor.Server/Filters/Controllers/EnumMultiSelectFilterMenuController.cs
git commit -m "feat: add EnumMultiSelect filter menu + controller"
```

---

## Task 20: BoolTriStateFilterMenu Razor component + controller

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Components/BoolTriStateFilterMenu.razor`
- Create: `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/BoolTriStateFilterMenuController.cs`

- [ ] **Step 1: Create the Razor component**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Components/BoolTriStateFilterMenu.razor`:

```razor
@using DevExpress.Blazor
@using DevExpress.Data.Filtering
@using XafFilter.Module.Filters

<div class="p-2" style="min-width: 140px;">
    <DxRadioGroup Items="Options"
                  Value="Selected"
                  ValueChanged="@((TriState v) => Selection_Changed(v))"
                  TextFieldName="@nameof(TriStateOption.Label)"
                  ValueFieldName="@nameof(TriStateOption.Value)" />
</div>

@code {
    [Parameter, EditorRequired]
    public GridDataColumnFilterMenuTemplateContext FilterContext { get; set; } = default!;

    public enum TriState { All, Yes, No }
    public record TriStateOption(string Label, TriState Value);

    static readonly TriStateOption[] Options =
    {
        new("All", TriState.All),
        new("Yes", TriState.Yes),
        new("No",  TriState.No),
    };

    TriState Selected { get; set; } = TriState.All;

    protected override void OnParametersSet()
    {
        var current = CriteriaBuilders.ReadBoolTriState(FilterContext.FilterCriteria, FilterContext.DataColumn.FieldName);
        Selected = current switch
        {
            true  => TriState.Yes,
            false => TriState.No,
            _     => TriState.All,
        };
    }

    void Selection_Changed(TriState v)
    {
        Selected = v;
        bool? value = v switch
        {
            TriState.Yes => true,
            TriState.No  => false,
            _            => null,
        };
        FilterContext.FilterCriteria = CriteriaBuilders.BuildBoolTriState(FilterContext.DataColumn.FieldName, value);
    }
}
```

- [ ] **Step 2: Create the controller**

Create `XafFilter/XafFilter.Blazor.Server/Filters/Controllers/BoolTriStateFilterMenuController.cs`:

```csharp
using System.Reflection;
using DevExpress.Blazor;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Blazor.Editors;
using XafFilter.Blazor.Server.Filters.Components;
using XafFilter.Module.Filters;

namespace XafFilter.Blazor.Server.Filters.Controllers;

public sealed class BoolTriStateFilterMenuController : ViewController<ListView>
{
    protected override void OnActivated()
    {
        base.OnActivated();
        View.ControlsCreated += View_ControlsCreated;
    }

    protected override void OnDeactivated()
    {
        View.ControlsCreated -= View_ControlsCreated;
        base.OnDeactivated();
    }

    private void View_ControlsCreated(object? sender, EventArgs e)
    {
        if (View.Editor is not DxGridListEditor editor) return;

        var typeInfo = View.ObjectTypeInfo;

        foreach (var column in editor.GridDataColumnModels)
        {
            var fieldName = column.FieldName;
            if (string.IsNullOrEmpty(fieldName)) continue;

            var member = typeInfo.FindMember(fieldName);
            if (member is null) continue;

            var propInfo = member.Owner.Type.GetProperty(fieldName, BindingFlags.Instance | BindingFlags.Public);
            if (propInfo?.GetCustomAttribute<DisableCustomFilterAttribute>() is not null) continue;

            var t = Nullable.GetUnderlyingType(member.MemberType) ?? member.MemberType;
            if (t != typeof(bool)) continue;

            column.FilterMenuButtonDisplayMode = GridFilterMenuButtonDisplayMode.Always;
            column.FilterMenuTemplate = context => builder =>
            {
                builder.OpenComponent<BoolTriStateFilterMenu>(0);
                builder.AddAttribute(1, nameof(BoolTriStateFilterMenu.FilterContext), context);
                builder.CloseComponent();
            };
        }
    }
}
```

- [ ] **Step 3: Build and manual smoke**

Build + run, then in browser:
1. Tickets list, funnel on **IsResolved** column → All / Yes / No radio.
2. Pick `Yes` — only resolved tickets remain.
3. Pick `No` — only unresolved.
4. Pick `All` — filter clears.

Stop the app.

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Blazor.Server/Filters/Components/BoolTriStateFilterMenu.razor XafFilter/XafFilter.Blazor.Server/Filters/Controllers/BoolTriStateFilterMenuController.cs
git commit -m "feat: add BoolTriState filter menu + controller"
```

---

## Task 21: Scaffold XafFilter.Blazor.Server.Tests Playwright project

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj`
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/Fixtures/AppFixture.cs`
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/Smoke/AppLaunchTests.cs`
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/.gitignore`
- Modify: `XafFilter.slnx`

- [ ] **Step 1: Scaffold an xunit project**

```
dotnet new xunit -n XafFilter.Blazor.Server.Tests -o XafFilter/XafFilter.Blazor.Server.Tests -f net10.0
```

Delete the auto-generated `UnitTest1.cs`.

- [ ] **Step 2: Replace the csproj with the right packages**

Replace `XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
    <PackageReference Include="Microsoft.Playwright" Version="1.49.0" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Install Playwright browser binaries (one-time per machine)**

Run:
```
dotnet build XafFilter/XafFilter.Blazor.Server.Tests
pwsh XafFilter/XafFilter.Blazor.Server.Tests/bin/Debug/net10.0/playwright.ps1 install chromium
```

Expected: chromium binary downloaded under `~/.cache/ms-playwright/`.

- [ ] **Step 4: Add a .gitignore for screenshots**

Create `XafFilter/XafFilter.Blazor.Server.Tests/.gitignore`:

```
screenshots/
```

- [ ] **Step 5: Create the app fixture**

Create `XafFilter/XafFilter.Blazor.Server.Tests/Fixtures/AppFixture.cs`:

```csharp
using System.Diagnostics;
using Microsoft.Playwright;

namespace XafFilter.Blazor.Server.Tests.Fixtures;

public sealed class AppFixture : IAsyncLifetime
{
    public const string BaseUrl = "https://localhost:44318";

    private Process? _serverProcess;
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await StartServerAsync();
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null) await Browser.DisposeAsync();
        Playwright?.Dispose();

        if (_serverProcess is { HasExited: false })
        {
            try
            {
                _serverProcess.Kill(entireProcessTree: true);
                await _serverProcess.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);
            }
            catch { /* best effort */ }
        }
    }

    public async Task<IBrowserContext> NewLoggedInContextAsync()
    {
        var ctx = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(10_000);

        await page.GotoAsync($"{BaseUrl}/LoginPage", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByLabel("User Name").FillAsync("Admin");
        // Admin password is blank — leave the password field as-is.
        await page.GetByRole(AriaRole.Button, new() { Name = "Log In" }).ClickAsync();
        await page.WaitForURLAsync(url => !url.Contains("LoginPage"), new() { Timeout = 10_000 });

        await page.CloseAsync();
        return ctx;
    }

    private async Task StartServerAsync()
    {
        var repoRoot = FindRepoRoot();
        var hostProj = Path.Combine(repoRoot, "XafFilter", "XafFilter.Blazor.Server");

        _serverProcess = Process.Start(new ProcessStartInfo
        {
            FileName  = "dotnet",
            Arguments = "run -c Release --no-launch-profile",
            WorkingDirectory = hostProj,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        });

        if (_serverProcess is null) throw new InvalidOperationException("Failed to start dotnet run");

        using var http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        });
        var deadline = DateTime.UtcNow.AddSeconds(60);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var r = await http.GetAsync($"{BaseUrl}/LoginPage");
                if ((int)r.StatusCode == 200) return;
            }
            catch { /* not ready */ }
            await Task.Delay(500);
        }
        throw new TimeoutException("XafFilter.Blazor.Server did not become ready within 60s.");
    }

    private static string FindRepoRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "XafFilter.slnx"))) d = d.Parent;
        return d?.FullName ?? throw new InvalidOperationException("XafFilter.slnx not found above test bin");
    }
}

[CollectionDefinition("App")]
public class AppCollection : ICollectionFixture<AppFixture> { }
```

- [ ] **Step 6: Add the project to the solution**

Edit `XafFilter.slnx`. Add:

```xml
<Project Path="XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj" />
```

- [ ] **Step 7: Add a smoke test to prove the fixture works**

Create `XafFilter/XafFilter.Blazor.Server.Tests/Smoke/AppLaunchTests.cs`:

```csharp
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
        page.SetDefaultNavigationTimeout(10_000);

        await page.GotoAsync(AppFixture.BaseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByRole(AriaRole.Link, new() { Name = "Ticket" }).First.ClickAsync();
        await page.WaitForSelectorAsync("text=Generate Demo Data");
    }
}
```

- [ ] **Step 8: Run the smoke tests**

Run:
```
dotnet test XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj
```

Expected: 2 tests pass. (Test runtime: ~30–60s — the fixture launches the app.)

If the fixture fails: check that port 44318 is free, that `dotnet dev-certs https --trust` has been run on this machine, and that the user Admin can log in manually via browser first.

- [ ] **Step 9: Commit**

```
git add XafFilter/XafFilter.Blazor.Server.Tests XafFilter.slnx
git commit -m "test: scaffold Playwright smoke-test project for Blazor host"
```

---

## Task 22: Filter smoke tests (one test per filter type)

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/Filters/FilterSmokeTests.cs`

- [ ] **Step 1: Write the filter smoke tests**

Create `XafFilter/XafFilter.Blazor.Server.Tests/Filters/FilterSmokeTests.cs`:

```csharp
using Microsoft.Playwright;
using XafFilter.Blazor.Server.Tests.Fixtures;

namespace XafFilter.Blazor.Server.Tests.Filters;

[Collection("App")]
public class FilterSmokeTests
{
    private readonly AppFixture _app;
    public FilterSmokeTests(AppFixture app) => _app = app;

    private async Task<IPage> OpenTicketsListAsync()
    {
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(10_000);
        await page.GotoAsync(AppFixture.BaseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByRole(AriaRole.Link, new() { Name = "Ticket" }).First.ClickAsync();
        await page.WaitForSelectorAsync("text=Generate Demo Data");

        // Seed 200 rows if grid is empty.
        var rowsBefore = await page.Locator(".dxbl-grid-data-row").CountAsync();
        if (rowsBefore == 0)
        {
            await page.GetByRole(AriaRole.Button, new() { Name = "Generate Demo Data" }).ClickAsync();
            await page.GetByLabel("Row Count").FillAsync("200");
            await page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
            await page.WaitForTimeoutAsync(2_000);
        }
        return page;
    }

    [Fact]
    public async Task DateRange_AppliesAndFiltersGrid()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='CreatedAt'] .dxbl-grid-filter-button").ClickAsync();
        // From: 30 days ago. To: today.
        var from = DateTime.Today.AddDays(-30).ToString("MM/dd/yyyy");
        var to   = DateTime.Today.ToString("MM/dd/yyyy");
        await page.GetByLabel("From").FillAsync(from);
        await page.GetByLabel("To").FillAsync(to);
        await page.Keyboard.PressAsync("Escape");
        var rows = await page.Locator(".dxbl-grid-data-row").CountAsync();
        Assert.True(rows < 200, $"Expected fewer than 200 rows after filter; got {rows}.");
    }

    [Fact]
    public async Task NumericRange_AppliesAndFiltersGrid()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='Priority'] .dxbl-grid-filter-button").ClickAsync();
        await page.GetByLabel("From").FillAsync("5");
        await page.GetByLabel("To").FillAsync("10");
        await page.Keyboard.PressAsync("Escape");
        var rows = await page.Locator(".dxbl-grid-data-row").CountAsync();
        Assert.True(rows < 200, $"Expected fewer than 200 rows after filter; got {rows}.");
    }

    [Fact]
    public async Task WildcardString_Like_FindsLoginRows()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='Subject'] .dxbl-grid-filter-button").ClickAsync();
        await page.Locator("input[placeholder='e.g. %login%']").FillAsync("%login%");
        await page.Keyboard.PressAsync("Escape");
        var rows = await page.Locator(".dxbl-grid-data-row").CountAsync();
        Assert.True(rows is > 0 and < 200, $"Expected 1..199 rows; got {rows}.");
    }

    [Fact]
    public async Task EnumMultiSelect_PicksSubset()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='Status'] .dxbl-grid-filter-button").ClickAsync();
        await page.GetByLabel("New").CheckAsync();
        await page.GetByLabel("InProgress").CheckAsync();
        await page.Keyboard.PressAsync("Escape");
        var rows = await page.Locator(".dxbl-grid-data-row").CountAsync();
        Assert.True(rows is > 0 and < 200, $"Expected 1..199 rows; got {rows}.");
    }

    [Fact]
    public async Task BoolTriState_FiltersTrue()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='IsResolved'] .dxbl-grid-filter-button").ClickAsync();
        await page.GetByLabel("Yes").CheckAsync();
        await page.Keyboard.PressAsync("Escape");
        var rows = await page.Locator(".dxbl-grid-data-row").CountAsync();
        Assert.True(rows is > 0 and < 200, $"Expected 1..199 rows; got {rows}.");
    }

    [Fact]
    public async Task DisableCustomFilter_FallsBackToDefault()
    {
        var page = await OpenTicketsListAsync();
        await page.Locator("[data-column='LegacyImportId'] .dxbl-grid-filter-button").ClickAsync();
        // The opt-out column should show the default DX filter menu, which has a Contains/Equals dropdown
        // rather than our custom "From" / "To" labels.
        var fromVisible = await page.GetByLabel("From").IsVisibleAsync();
        Assert.False(fromVisible, "LegacyImportId should NOT show the custom NumericRange menu.");
    }
}
```

Selector caveat: the `[data-column='X']` and `.dxbl-grid-filter-button` selectors assume DevExpress' default DOM. If your build of `DxGridListEditor` renders different attributes, fix the selectors after running the tests once and inspecting the DOM via `page.Pause()` in headed mode.

- [ ] **Step 2: Run the filter smoke tests**

Run:
```
dotnet test XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj --filter FullyQualifiedName~FilterSmokeTests
```

Expected: 6 tests pass.

If selectors fail: temporarily set `Headless = false` in `AppFixture.cs`, rerun a single test, inspect the DOM, update selectors. Revert `Headless = true` before committing.

- [ ] **Step 3: Commit**

```
git add XafFilter/XafFilter.Blazor.Server.Tests/Filters
git commit -m "test: add Playwright smoke tests for all 5 filters + opt-out"
```

---

## Task 23: Theme verification tests (light + dark)

**Files:**
- Create: `XafFilter/XafFilter.Blazor.Server.Tests/Themes/ThemeRenderTests.cs`

- [ ] **Step 1: Add theme tests with screenshots**

Create `XafFilter/XafFilter.Blazor.Server.Tests/Themes/ThemeRenderTests.cs`:

```csharp
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

    private async Task<IPage> NavigateToTicketsAsync(IBrowserContext ctx)
    {
        var page = await ctx.NewPageAsync();
        page.SetDefaultNavigationTimeout(10_000);
        await page.GotoAsync(AppFixture.BaseUrl, new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await page.GetByRole(AriaRole.Link, new() { Name = "Ticket" }).First.ClickAsync();
        await page.WaitForSelectorAsync("text=Generate Demo Data");
        return page;
    }

    private async Task SwitchThemeAsync(IPage page, string themeName)
    {
        // The theme switcher is a top-right button. Open it and click the target theme.
        await page.GetByRole(AriaRole.Button, new() { Name = "Theme" }).ClickAsync();
        await page.GetByText(themeName, new() { Exact = true }).ClickAsync();
        await page.WaitForTimeoutAsync(1_000); // theme stylesheet swap
    }

    [Fact]
    public async Task LightTheme_FilterMenusRender()
    {
        Directory.CreateDirectory(ScreenshotDir);
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await NavigateToTicketsAsync(ctx);
        await page.Locator("[data-column='CreatedAt'] .dxbl-grid-filter-button").ClickAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "light-daterange.png") });
        Assert.True(await page.GetByLabel("From").IsVisibleAsync());
    }

    [Fact]
    public async Task DarkTheme_FilterMenusRender()
    {
        Directory.CreateDirectory(ScreenshotDir);
        await using var ctx = await _app.NewLoggedInContextAsync();
        var page = await NavigateToTicketsAsync(ctx);
        await SwitchThemeAsync(page, "Blazing Dark");
        await page.Locator("[data-column='CreatedAt'] .dxbl-grid-filter-button").ClickAsync();
        await page.ScreenshotAsync(new() { Path = Path.Combine(ScreenshotDir, "dark-daterange.png") });
        Assert.True(await page.GetByLabel("From").IsVisibleAsync());
    }
}
```

- [ ] **Step 2: Run the theme tests**

Run:
```
dotnet test XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj --filter FullyQualifiedName~ThemeRenderTests
```

Expected: 2 tests pass. Screenshots appear in `XafFilter/XafFilter.Blazor.Server.Tests/screenshots/`.

Open both PNGs manually and confirm:
- Light theme: dark text on white background, filter menu readable.
- Dark theme: light text on dark background, filter menu readable.

If the dark theme screenshot has unreadable black-on-black text, that's a real bug — file it and fix before merging.

- [ ] **Step 3: Run the full Playwright suite**

Run:
```
dotnet test XafFilter/XafFilter.Blazor.Server.Tests/XafFilter.Blazor.Server.Tests.csproj
```

Expected: 10 tests pass (2 smoke + 6 filter + 2 theme).

- [ ] **Step 4: Commit**

```
git add XafFilter/XafFilter.Blazor.Server.Tests/Themes
git commit -m "test: add light + dark theme verification with screenshots"
```

---

## Task 24: Update /xaf-filter-notes skill with the 5-step contract

**Files:**
- Modify: `.claude/skills/xaf-filter-notes/SKILL.md`

- [ ] **Step 1: Add the contract and opt-out attribute sections to the skill**

Edit `.claude/skills/xaf-filter-notes/SKILL.md`. Before the `## Decimal & string criteria` section (or anywhere logical near the top of the body), insert:

```markdown
## The 5-step filter-menu contract

XafFilter ships five custom column filter menus, each pairing a `ViewController<ListView>` with a Razor component. Every filter follows the same lifecycle:

1. **Controller.OnActivated** subscribes to `View.ControlsCreated`.
2. **Controller.View_ControlsCreated** iterates `editor.GridDataColumnModels`, skips columns whose `MemberType` doesn't match this filter or whose property has `[DisableCustomFilter]`, then sets `FilterMenuButtonDisplayMode = Always` and assigns a `FilterMenuTemplate` that renders the Razor component.
3. **Razor.OnParametersSet** calls a `CriteriaBuilders.ReadXxx` helper to recover the inputs from `FilterContext.FilterCriteria`.
4. **Razor.OnInputChanged** calls `CriteriaBuilders.BuildXxx` and writes the result back to `FilterContext.FilterCriteria`.
5. **Controller.OnDeactivated** unsubscribes `ControlsCreated`.

All criteria construction lives in `XafFilter.Module/Filters/CriteriaBuilders.cs` — pure helpers, no Blazor dependency, fully unit-testable.

## Opting out of custom filters

Apply `[XafFilter.Module.Filters.DisableCustomFilter]` to any property to fall back to the default DevExpress filter menu. Useful for ID columns, technical/legacy fields, and any column where the type-based heuristic picks the wrong filter.

## Demo data

The Support-Tickets demo BOs in `XafFilter.Module/BusinessObjects/Demo/` are seeded by `DemoDataSeeder` (uses Bogus). The seeder is **deterministic** — `Randomizer.Seed = new Random(42)` — so the same row count produces the same data each run. Keep this seed value if you want Playwright screenshots to remain reproducible.

The `Generate Demo Data` action on the Ticket ListView is the only entry point; there is no auto-seed on first run.
```

- [ ] **Step 2: Commit**

```
git add .claude/skills/xaf-filter-notes/SKILL.md
git commit -m "docs: document filter contract, opt-out, demo seeder in xaf-filter-notes skill"
```

---

## Self-review

After the plan was written, checked against the spec:

**Spec coverage:**

- ✅ Filter contract (5 steps): documented in plan intro, enforced in Tasks 16–20, encoded into the skill in Task 24.
- ✅ Five filters (DateRange, NumericRange, WildcardString, EnumMultiSelect, BoolTriState): Tasks 3–7 (criteria builders), 16–20 (Razor + controllers).
- ✅ `[DisableCustomFilter]` opt-out attribute: Task 2 (attribute + tests), reflected in every controller (Tasks 16–20), tested in Task 22.
- ✅ Demo BOs (Customer, Agent, Ticket, GenerateDemoDataParameters) + enums: Tasks 8–13.
- ✅ Bogus + DemoDataSeeder: Task 14.
- ✅ Generator action: Task 15.
- ✅ xUnit test project: Task 1 (scaffold), Tasks 2–7 (criteria tests), Task 14 (seeder tests).
- ✅ Playwright test project: Task 21 (scaffold + login fixture), Task 22 (5 filter tests + opt-out), Task 23 (light + dark theme).
- ✅ `.slnx` updates: Tasks 1 and 21.
- ✅ DbSet + AdditionalExportedTypes registration: Task 13.
- ✅ Error handling per spec (overflow → null, whitespace → null, mismatched criteria → empty round-trip): all tested in Tasks 3–7.

**Placeholder scan:** none.

**Type consistency:** `CriteriaBuilders.BuildDateRange/ReadDateRange`, `BuildNumericRange/ReadNumericRange`, `BuildWildcard/ReadWildcard`, `BuildEnumIn/ReadEnumIn`, `BuildBoolTriState/ReadBoolTriState` — names match across criteria builders, tests, and Razor consumers. `DisableCustomFilterAttribute` referenced consistently. `GenerateDemoDataParameters` referenced consistently. `XafFilter.Module.Filters` namespace used uniformly.

**Out of scope items:** Updater permissions (the existing Admin role is `IsAdministrative = true`, so it gets full access without explicit BO permissions — confirmed by reading the existing `Updater.cs`). The spec mentioned Updater changes but they're unnecessary; this is a small spec deviation worth noting at execution time.

---

## Total task count: 24

Estimated suite size at end:
- Module.Tests: 47 unit tests
- Blazor.Server.Tests: 10 Playwright tests (2 smoke + 6 filter + 2 theme)
- New code: ~13 files in Module, ~10 files in Blazor.Server, ~2 test projects
