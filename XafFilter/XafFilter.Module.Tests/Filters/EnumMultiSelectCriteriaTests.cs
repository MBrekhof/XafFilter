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
