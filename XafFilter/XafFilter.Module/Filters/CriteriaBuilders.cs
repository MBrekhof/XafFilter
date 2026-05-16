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

    static decimal? ToDecimal(object? v)
        => v is null ? null : Convert.ToDecimal(v, System.Globalization.CultureInfo.InvariantCulture);
}
