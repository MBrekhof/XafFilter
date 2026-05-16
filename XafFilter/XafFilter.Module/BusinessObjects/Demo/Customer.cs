#nullable enable
using DevExpress.Persistent.BaseImpl.EF;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace XafFilter.Module.BusinessObjects.Demo;

[DefaultProperty(nameof(Name))]
public class Customer : BaseObject
{
    public virtual string Name { get; set; } = string.Empty;
    public virtual string? Email { get; set; }
    public virtual string? Company { get; set; }
    public virtual bool IsVip { get; set; }
    public virtual DateTime CreatedAt { get; set; }

    public virtual IList<Ticket> Tickets { get; set; } = new ObservableCollection<Ticket>();
}
