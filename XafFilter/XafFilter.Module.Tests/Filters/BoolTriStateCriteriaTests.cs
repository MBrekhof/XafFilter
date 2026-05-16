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
