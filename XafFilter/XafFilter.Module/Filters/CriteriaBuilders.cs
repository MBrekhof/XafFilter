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
