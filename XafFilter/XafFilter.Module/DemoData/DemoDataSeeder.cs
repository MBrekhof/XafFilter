using Bogus;
using DevExpress.ExpressApp;
using XafFilter.Module.BusinessObjects.Demo;

namespace XafFilter.Module.DemoData;

public static class DemoDataSeeder
{
    private const int RandomSeed = 42;

    private static readonly string[] SubjectTemplates =
    {
        "Login fails on Safari",
        "Login fails on Chrome",
        "Login intermittent",
        "Payment processing timeout",
        "Payment retried but failed",
        "Refund missing",
        "Export to CSV broken",
        "Export hangs on large file",
        "Email notifications delayed",
        "Dashboard slow to load",
        "Report renders blank",
        "Search returns no results",
        "Search returns wrong results",
        "Bulk import fails halfway",
        "Bulk import skips rows silently",
    };

    private static readonly decimal[] BillableRates = { 75m, 95m, 125m, 150m, 200m };

    public static void Seed(IObjectSpace os, GenerateDemoDataParameters p)
    {
        if (p.ClearExistingFirst)
        {
            os.Delete(os.GetObjects<Ticket>());
            os.Delete(os.GetObjects<Customer>());
            os.Delete(os.GetObjects<Agent>());
            os.CommitChanges();
        }

        Randomizer.Seed = new Random(RandomSeed);

        var customers = SeedCustomers(os, count: Math.Max(5, Math.Min(p.RowCount / 10, 100)));
        var agents    = SeedAgents(os,    count: Math.Max(3, Math.Min(p.RowCount / 50, 20)));
        SeedTickets(os, customers, agents, p);
    }

    private static List<Customer> SeedCustomers(IObjectSpace os, int count)
    {
        var faker = new Faker<Customer>()
            .RuleFor(c => c.Name,      f => f.Name.FullName())
            .RuleFor(c => c.Email,     (f, c) => f.Internet.Email(c.Name))
            .RuleFor(c => c.Company,   f => f.Company.CompanyName())
            .RuleFor(c => c.IsVip,     f => f.Random.Bool(weight: 0.10f))
            .RuleFor(c => c.CreatedAt, f => f.Date.Past(2));

        var result = new List<Customer>(count);
        for (int i = 0; i < count; i++)
        {
            var c = os.CreateObject<Customer>();
            var data = faker.Generate();
            c.Name      = data.Name;
            c.Email     = data.Email;
            c.Company   = data.Company;
            c.IsVip     = data.IsVip;
            c.CreatedAt = data.CreatedAt;
            result.Add(c);
        }
        return result;
    }

    private static List<Agent> SeedAgents(IObjectSpace os, int count)
    {
        var faker = new Faker<Agent>()
            .RuleFor(a => a.DisplayName,   f => f.Name.FullName())
            .RuleFor(a => a.Email,         (f, a) => f.Internet.Email(a.DisplayName))
            .RuleFor(a => a.IsActive,      f => f.Random.Bool(weight: 0.80f))
            .RuleFor(a => a.HoursPerWeek,  f => f.Random.Int(8, 40));

        var result = new List<Agent>(count);
        for (int i = 0; i < count; i++)
        {
            var a = os.CreateObject<Agent>();
            var data = faker.Generate();
            a.DisplayName  = data.DisplayName;
            a.Email        = data.Email;
            a.IsActive     = data.IsActive;
            a.HoursPerWeek = data.HoursPerWeek;
            result.Add(a);
        }
        return result;
    }

    private static void SeedTickets(IObjectSpace os, List<Customer> customers, List<Agent> agents, GenerateDemoDataParameters p)
    {
        var faker = new Faker();
        var statusWeights   = new[] { 0.25f, 0.20f, 0.15f, 0.25f, 0.10f, 0.05f };
        var severityWeights = new[] { 0.40f, 0.35f, 0.20f, 0.05f };

        for (int i = 0; i < p.RowCount; i++)
        {
            var t = os.CreateObject<Ticket>();
            t.Subject          = faker.PickRandom(SubjectTemplates);
            t.Description      = faker.Lorem.Sentence(wordCount: faker.Random.Int(8, 25));
            t.CreatedAt        = faker.Date.Between(p.DateFrom, p.DateTo);
            t.Status           = faker.Random.WeightedRandom(Enum.GetValues<TicketStatus>(),   statusWeights);
            t.Severity         = faker.Random.WeightedRandom(Enum.GetValues<TicketSeverity>(), severityWeights);
            t.Priority         = faker.Random.Int(1, 10);
            t.HoursSpent       = Math.Round(faker.Random.Decimal(0, 40), 2);
            t.BillableRate     = faker.PickRandom(BillableRates);
            t.IsResolved       = t.Status is TicketStatus.Resolved or TicketStatus.Closed;
            t.IsBillable       = faker.Random.Bool(weight: 0.70f);
            t.ClosedAt         = faker.Random.Bool(weight: 0.60f) ? t.CreatedAt.AddDays(faker.Random.Int(1, 30)) : null;
            t.Customer         = faker.PickRandom(customers);
            t.AssignedAgent    = faker.Random.Bool(weight: 0.90f) ? faker.PickRandom(agents) : null;
            t.LegacyImportId   = i + 1;
        }
    }
}
