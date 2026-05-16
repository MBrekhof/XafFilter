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
