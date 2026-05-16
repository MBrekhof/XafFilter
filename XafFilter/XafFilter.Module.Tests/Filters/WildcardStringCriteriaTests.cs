using DevExpress.Data.Filtering;
using XafFilter.Module.Filters;

namespace XafFilter.Module.Tests.Filters;

public class WildcardStringCriteriaTests
{
    const string Field = "Subject";

#pragma warning disable CS0618 // BinaryOperatorType.Like is obsolete but still the only way to get raw SQL LIKE.

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
