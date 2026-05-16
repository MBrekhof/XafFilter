namespace XafFilter.Module.Filters;

/// <summary>
/// Apply to a property to opt that column out of the auto-applied custom filter menu.
/// The default DevExpress filter menu is used instead.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class DisableCustomFilterAttribute : Attribute
{
}
