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
