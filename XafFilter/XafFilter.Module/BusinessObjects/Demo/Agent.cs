#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(DisplayName))]
public class Agent : BaseObject
{
    public virtual string DisplayName { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual bool IsActive { get; set; }
    public virtual int HoursPerWeek { get; set; }

    public virtual IList<Ticket> AssignedTickets { get; set; } = new ObservableCollection<Ticket>();
}
