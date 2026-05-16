#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using XafFilter.Module.Filters;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(Subject))]
public class Ticket : BaseObject
{
    public virtual string Subject { get; set; } = string.Empty;
    public virtual string? Description { get; set; }
    public virtual DateTime CreatedAt { get; set; }
    public virtual DateTime? ClosedAt { get; set; }
    public virtual TicketStatus Status { get; set; }
    public virtual TicketSeverity Severity { get; set; }
    public virtual int Priority { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal HoursSpent { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public virtual decimal BillableRate { get; set; }

    public virtual bool IsResolved { get; set; }
    public virtual bool IsBillable { get; set; }

    public virtual Customer? Customer { get; set; }
    public virtual Agent? AssignedAgent { get; set; }

    [DisableCustomFilter]
    public virtual int LegacyImportId { get; set; }
}
