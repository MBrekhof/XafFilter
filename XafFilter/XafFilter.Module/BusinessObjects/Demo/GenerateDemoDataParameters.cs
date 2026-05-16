using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DomainComponent]
public class GenerateDemoDataParameters : NonPersistentBaseObject
{
    [RuleRange(1, 100_000)]
    public virtual int RowCount { get; set; } = 500;

    public virtual DateTime DateFrom { get; set; } = DateTime.Today.AddMonths(-6);

    public virtual DateTime DateTo { get; set; } = DateTime.Today;

    [Description("Delete all existing Tickets, Customers, and Agents before seeding.")]
    public virtual bool ClearExistingFirst { get; set; }
}
