using ReclamationService.Application.DTOs;
using ReclamationService.Application.Exceptions;
using ReclamationService.Application.Outbox;
using ReclamationService.Application.Security;
using ReclamationService.Application.Services;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;
using ReclamationService.Domain.Interfaces;
using SharedEvents.Events;

namespace ReclamationService.Tests.Services;

public class ReclamationsServiceOwnershipTests
{
    [Fact]
    public async Task FirstSavCanClaimUnclaimedReclamation()
    {
        var harness = Harness.Create(CreateReclamation());

        var result = await harness.Service.ClaimAsync(100, Sav(20, "SAV A"));

        Assert.True(result.IsClaimed);
        Assert.True(result.IsClaimedByCurrentUser);
        Assert.True(result.CanCurrentUserWorkOnIt);
        Assert.Equal(20, harness.Reclamations.Items[100].ClaimedBySavId);
        Assert.Contains(harness.History.Items, item => item.Comment?.Contains("taken by SAV SAV A") == true);
    }

    [Fact]
    public async Task SecondSavClaimingSameReclamationReceivesConflict()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));

        var ex = await Assert.ThrowsAsync<ConflictException>(() => harness.Service.ClaimAsync(100, Sav(21, "SAV B")));

        Assert.Equal(409, ex.StatusCode);
        Assert.Contains("already taken by SAV A", ex.Message);
    }

    [Fact]
    public async Task SameSavClaimingAgainIsIdempotent()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));

        var result = await harness.Service.ClaimAsync(100, Sav(20, "SAV A"));

        Assert.True(result.IsClaimedByCurrentUser);
        Assert.Equal("Assigned to me", result.OwnershipLabel);
    }

    [Fact]
    public async Task SavCannotApplyAiPriorityWhenNotOwner()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));
        var analysis = Analysis();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.ApplyAiPrioritySuggestionAsync(100, analysis, null, Sav(21, "SAV B")));
    }

    [Fact]
    public async Task OwnerSavCanApplyAiPriority()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));

        var result = await harness.Service.ApplyAiPrioritySuggestionAsync(100, Analysis(), null, Sav(20, "SAV A"));

        Assert.Equal(NamePriority.URGENT, result.Priority);
        Assert.Contains(harness.History.Items, item => item.Comment?.Contains("applied by SAV A") == true);
    }

    [Fact]
    public async Task AdminCanReleaseOwnership()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));

        var result = await harness.Service.ReleaseAsync(100, Admin());

        Assert.False(result.IsClaimed);
        Assert.Null(harness.Reclamations.Items[100].ClaimedBySavId);
        Assert.NotNull(harness.Reclamations.Items[100].ReleasedAt);
    }

    [Fact]
    public async Task AdminCanReassignOwnershipToSavUser()
    {
        var harness = Harness.Create(CreateReclamation(claimedBySavId: 20, claimedBySavName: "SAV A"));
        harness.ServiceUsers.Upsert(new ServiceUser { Id = 21, FullName = "SAV B", Email = "savb@test.local", Role = "SAV" });

        var result = await harness.Service.ReassignSavAsync(100, new ReassignSavDto { SavUserId = 21 }, Admin());

        Assert.Equal(21, result.ClaimedBySavId);
        Assert.Equal("SAV B", result.ClaimedBySavName);
        Assert.Contains(harness.History.Items, item => item.Comment?.Contains("reassigned from SAV A to SAV B") == true);
    }

    [Fact]
    public async Task ClientCannotClaimReclamation()
    {
        var harness = Harness.Create(CreateReclamation());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.ClaimAsync(100, new CurrentUser(50, "client@test.local", "Client", "CLIENT", "corr")));
    }

    [Fact]
    public async Task ClientCreationDoesNotSetSavOwnership()
    {
        var harness = Harness.Create();

        var result = await harness.Service.CreateAsync(new CreateReclamationDto { Description = "The generator stopped suddenly. Production is blocked." }, new CurrentUser(50, "client@test.local", "Client", "CLIENT", "corr"));

        Assert.False(result.IsClaimed);
        Assert.Null(harness.Reclamations.Items[result.Id].ClaimedBySavId);
        Assert.Null(harness.Reclamations.Items[result.Id].ClaimedBySavName);
    }

    [Fact]
    public async Task PlanningRequestCannotBeCreatedTwiceByTwoSavUsers()
    {
        var harness = Harness.Create(CreateReclamation(
            status: ReclamationStatus.Assigned,
            savId: 20,
            claimedBySavId: 20,
            claimedBySavName: "SAV A"));

        await harness.Service.RequestPlanningAsync(100, new RequestPlanningDto { Comment = "Plan it" }, Sav(20, "SAV A"));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            harness.Service.RequestPlanningAsync(100, new RequestPlanningDto { Comment = "Second SAV attempt" }, Sav(21, "SAV B")));

        await Assert.ThrowsAsync<ConflictException>(() =>
            harness.Service.RequestPlanningAsync(100, new RequestPlanningDto { Comment = "Duplicate owner attempt" }, Sav(20, "SAV A")));
    }

    private static CurrentUser Sav(long id, string name) => new(id, $"{id}@sav.local", name, "SAV", "corr");

    private static CurrentUser Admin() => new(1, "admin@test.local", "Admin", "ADMIN", "corr");

    private static AiPriorityAnalysisDto Analysis() => new()
    {
        ReclamationId = 100,
        SuggestedPriority = "Urgent",
        ConfidenceScore = 95,
        SlaRisk = "High",
        Reason = "Production is blocked.",
        RecommendedAction = "Assign technician immediately."
    };

    private static Reclamation CreateReclamation(
        ReclamationStatus status = ReclamationStatus.Open,
        long? savId = null,
        long? claimedBySavId = null,
        string? claimedBySavName = null)
    {
        return new Reclamation
        {
            Id = 100,
            Reference = "REC-TEST-1",
            Description = "The generator stopped suddenly. Production is blocked.",
            Priority = NamePriority.LOW,
            Severity = NamePriority.LOW,
            PrioritySource = PrioritySource.PendingReview,
            Status = status,
            ClientId = 50,
            ClientName = "Client",
            SAVId = savId,
            SAVName = savId.HasValue ? "SAV A" : null,
            AssignedAt = status == ReclamationStatus.Assigned ? DateTime.UtcNow : null,
            ClaimedBySavId = claimedBySavId,
            ClaimedBySavName = claimedBySavName,
            ClaimedAt = claimedBySavId.HasValue ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
    }

    private sealed class Harness
    {
        public FakeReclamationRepository Reclamations { get; }
        public FakeHistoryRepository History { get; }
        public FakeServiceUserRepository ServiceUsers { get; }
        public ReclamationsService Service { get; }

        private Harness(FakeReclamationRepository reclamations, FakeHistoryRepository history, FakeServiceUserRepository serviceUsers, ReclamationsService service)
        {
            Reclamations = reclamations;
            History = history;
            ServiceUsers = serviceUsers;
            Service = service;
        }

        public static Harness Create(params Reclamation[] reclamations)
        {
            var reclamationRepository = new FakeReclamationRepository(reclamations);
            var clientRepository = new FakeClientRepository();
            var serviceUserRepository = new FakeServiceUserRepository();
            serviceUserRepository.Upsert(new ServiceUser { Id = 20, FullName = "SAV A", Email = "sava@test.local", Role = "SAV" });
            var historyRepository = new FakeHistoryRepository();
            var service = new ReclamationsService(
                reclamationRepository,
                clientRepository,
                serviceUserRepository,
                historyRepository,
                new FakeOutboxWriter(),
                new TicketClassificationService(),
                new ReclamationPriorityService(),
                new ReclamationSlaService());

            return new Harness(reclamationRepository, historyRepository, serviceUserRepository, service);
        }
    }

    private sealed class FakeReclamationRepository : IReclamationRepository
    {
        private long _nextId = 1000;
        public Dictionary<long, Reclamation> Items { get; }

        public FakeReclamationRepository(IEnumerable<Reclamation> items)
        {
            Items = items.ToDictionary(item => item.Id);
        }

        public List<Reclamation> GetAll() => Items.Values.OrderByDescending(x => x.CreatedAt).ToList();
        public IQueryable<Reclamation> Query() => Items.Values.AsQueryable();
        public List<Reclamation> GetForClient(long clientId) => Items.Values.Where(x => x.ClientId == clientId).ToList();
        public List<Reclamation> GetOpenBacklog() => Items.Values.Where(x => x.Status == ReclamationStatus.Open).ToList();
        public List<Reclamation> GetForSav(long savId) => Items.Values.Where(x => x.SAVId == savId || x.ClaimedBySavId == savId).ToList();
        public List<Reclamation> GetForTechnician(long technicianId) => Items.Values.Where(x => x.TechnicianId == technicianId).ToList();
        public List<Reclamation> GetByStatus(ReclamationStatus status) => Items.Values.Where(x => x.Status == status).ToList();
        public Reclamation? GetById(long id) => Items.GetValueOrDefault(id);
        public Reclamation? GetByRefernce(string reference) => Items.Values.FirstOrDefault(x => x.Reference == reference);
        public List<Reclamation> GetByPriority(NamePriority priority) => Items.Values.Where(x => x.Priority == priority).ToList();

        public Reclamation Create(Reclamation reclamation)
        {
            reclamation.Id = ++_nextId;
            Items[reclamation.Id] = reclamation;
            return reclamation;
        }

        public Reclamation Update(Reclamation reclamation)
        {
            Items[reclamation.Id] = reclamation;
            return reclamation;
        }

        public Task<int> ClaimIfAvailableAsync(long id, long savId, string savName, DateTime claimedAt, CancellationToken cancellationToken = default)
        {
            if (!Items.TryGetValue(id, out var item)
                || item.ClaimedBySavId.HasValue
                || item.Status is ReclamationStatus.Closed or ReclamationStatus.Cancelled or ReclamationStatus.Rejected)
            {
                return Task.FromResult(0);
            }

            item.ClaimedBySavId = savId;
            item.ClaimedBySavName = savName;
            item.ClaimedAt = claimedAt;
            item.ReleasedAt = null;
            item.UpdatedAt = claimedAt;
            return Task.FromResult(1);
        }

        public void Delete(long id) => Items.Remove(id);
    }

    private sealed class FakeClientRepository : IClientRepository
    {
        public List<Client> GetAll() => new();
        public Client? GetById(long id) => new() { Id = id, FullName = "Client", Email = "client@test.local", PhoneNumber = "+100000000" };
        public Client? GetByEmail(string email) => null;
        public long GetNextId() => 1;
        public void Add(Client client) { }
        public void Update(Client client) { }
    }

    private sealed class FakeServiceUserRepository : IServiceUserRepository
    {
        private readonly Dictionary<long, ServiceUser> _items = new();

        public ServiceUser? GetById(long id) => _items.GetValueOrDefault(id);

        public void Upsert(ServiceUser user)
        {
            _items[user.Id] = user;
        }
    }

    private sealed class FakeHistoryRepository : IReclamationHistoryRepository
    {
        public List<ReclamationHistory> Items { get; } = new();
        public List<ReclamationHistory> GetByReclamationId(long reclamationId) => Items.Where(x => x.ReclamationId == reclamationId).ToList();
        public ReclamationHistory Add(ReclamationHistory item)
        {
            Items.Add(item);
            return item;
        }
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public Task EnqueueAsync(IIntegrationEvent evt, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
