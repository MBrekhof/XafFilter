#nullable enable
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
