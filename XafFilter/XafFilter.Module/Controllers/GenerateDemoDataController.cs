#nullable enable
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.Templates;
using DevExpress.Persistent.Base;
using XafFilter.Module.BusinessObjects.Demo;
using XafFilter.Module.DemoData;

namespace XafFilter.Module.Controllers;

public sealed class GenerateDemoDataController : ViewController<ListView>
{
    private readonly PopupWindowShowAction _action;

    public GenerateDemoDataController()
    {
        TargetObjectType = typeof(Ticket);

        _action = new PopupWindowShowAction(this, "GenerateDemoData", PredefinedCategory.Edit)
        {
            Caption = "Generate Demo Data",
            ImageName = "Action_Refresh",
            SelectionDependencyType = SelectionDependencyType.Independent,
            PaintStyle = ActionItemPaintStyle.CaptionAndImage,
        };

        _action.CustomizePopupWindowParams += OnCustomizePopupWindowParams;
        _action.Execute += OnExecute;
    }

    private void OnCustomizePopupWindowParams(object? sender, CustomizePopupWindowParamsEventArgs e)
    {
        var os = Application.CreateObjectSpace(typeof(GenerateDemoDataParameters));
        var p  = os.CreateObject<GenerateDemoDataParameters>();
        e.View = Application.CreateDetailView(os, p);
    }

    private void OnExecute(object? sender, PopupWindowShowActionExecuteEventArgs e)
    {
        var parameters = (GenerateDemoDataParameters)e.PopupWindow.View.CurrentObject;

        using var os = Application.CreateObjectSpace(typeof(Ticket));
        var localParams = new GenerateDemoDataParameters
        {
            RowCount           = parameters.RowCount,
            DateFrom           = parameters.DateFrom,
            DateTo             = parameters.DateTo,
            ClearExistingFirst = parameters.ClearExistingFirst,
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();
        DemoDataSeeder.Seed(os, localParams);
        os.CommitChanges();
        sw.Stop();

        Tracing.Tracer.LogText(
            $"DemoDataSeeder: generated {localParams.RowCount} tickets in {sw.ElapsedMilliseconds}ms"
        );

        View.ObjectSpace.Refresh();
    }
}
