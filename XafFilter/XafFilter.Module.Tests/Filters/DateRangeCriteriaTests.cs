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
