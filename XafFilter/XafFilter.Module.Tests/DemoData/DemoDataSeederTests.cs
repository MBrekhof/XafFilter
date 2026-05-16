using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using Microsoft.EntityFrameworkCore;
using XafFilter.Module.BusinessObjects.Demo;
using XafFilter.Module.DemoData;

namespace XafFilter.Module.Tests.DemoData;

/// <summary>
/// Minimal DbContext for in-memory seeder tests.
/// Strips the XAF-specific change tracking strategy that is incompatible with EF InMemory.
/// </summary>
file sealed class SeederTestDbContext : DbContext
{
    public SeederTestDbContext(DbContextOptions<SeederTestDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
}

public class DemoDataSeederTests
{
    private static (IObjectSpace os, IDisposable cleanup) CreateInMemoryObjectSpace()
    {
        var dbName = Guid.NewGuid().ToString();
        var provider = new EFCoreObjectSpaceProvider<SeederTestDbContext>(
            (builder, _) => builder
                .UseInMemoryDatabase(dbName)
                .UseXafCalculatedProperties());
        var os = provider.CreateObjectSpace();
        return (os, provider);
    }

    private static GenerateDemoDataParameters Params(int rowCount = 100, bool clear = false) => new()
    {
        RowCount = rowCount,
        DateFrom = new DateTime(2026, 1, 1),
        DateTo   = new DateTime(2026, 6, 30),
        ClearExistingFirst = clear,
    };

    [Fact]
    public void Seed_ProducesRequestedRowCount()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;
        DemoDataSeeder.Seed(os, Params(rowCount: 100));
        os.CommitChanges();
        Assert.Equal(100, os.GetObjectsCount(typeof(Ticket), null));
    }

    [Fact]
    public void Seed_IsDeterministic()
    {
        var (os1, c1) = CreateInMemoryObjectSpace();
        var (os2, c2) = CreateInMemoryObjectSpace();
        using var _ = c1;
        using var __ = c2;
        DemoDataSeeder.Seed(os1, Params(50));
        DemoDataSeeder.Seed(os2, Params(50));
        os1.CommitChanges();
        os2.CommitChanges();

        var subjects1 = os1.GetObjects<Ticket>().Select(t => t.Subject).OrderBy(s => s).ToList();
        var subjects2 = os2.GetObjects<Ticket>().Select(t => t.Subject).OrderBy(s => s).ToList();
        Assert.Equal(subjects1, subjects2);
    }

    [Fact]
    public void Seed_ClearExistingFirst_RemovesPriorData()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;

        DemoDataSeeder.Seed(os, Params(50));
        os.CommitChanges();
        Assert.Equal(50, os.GetObjectsCount(typeof(Ticket), null));

        DemoDataSeeder.Seed(os, Params(25, clear: true));
        os.CommitChanges();
        Assert.Equal(25, os.GetObjectsCount(typeof(Ticket), null));
    }

    [Fact]
    public void Seed_LegacyImportIdIsSequential()
    {
        var (os, cleanup) = CreateInMemoryObjectSpace();
        using var _ = cleanup;
        DemoDataSeeder.Seed(os, Params(20));
        os.CommitChanges();
        var ids = os.GetObjects<Ticket>().Select(t => t.LegacyImportId).OrderBy(i => i).ToList();
        Assert.Equal(Enumerable.Range(1, 20), ids);
    }
}
