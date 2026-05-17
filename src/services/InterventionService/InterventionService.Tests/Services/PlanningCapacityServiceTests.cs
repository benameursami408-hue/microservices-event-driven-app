using InterventionService.Application.Services;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Enums;
using InterventionService.Domain.Interfaces;

namespace InterventionService.Tests.Services;

public class PlanningCapacityServiceTests
{
    [Fact]
    public async Task EvaluateAsync_ReturnsConflict_WhenAppointmentsOverlap()
    {
        var repository = new FakeAppointmentRepository(
            new Appointment
            {
                Id = Guid.NewGuid(),
                Reference = "REC-EXISTING",
                ReclamationId = 1,
                TechnicianId = 42,
                TechnicianName = "Tech",
                StartAt = DateTime.UtcNow.Date.AddHours(9),
                EndAt = DateTime.UtcNow.Date.AddHours(10),
                EstimatedDurationMinutes = 60,
                Status = AppointmentStatus.Confirmed
            });

        var service = new PlanningCapacityService(repository);

        var result = await service.EvaluateAsync(
            42,
            DateTime.UtcNow.Date.AddHours(9).AddMinutes(30),
            DateTime.UtcNow.Date.AddHours(10).AddMinutes(30),
            60);

        Assert.False(result.IsAvailable);
        Assert.Contains(result.Conflicts, x => x.Contains("Overlap"));
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsConflict_WhenDailyCapacityExceeded()
    {
        var day = DateTime.UtcNow.Date.AddHours(8);
        var appointments = Enumerable.Range(0, 4)
            .Select(index => new Appointment
            {
                Id = Guid.NewGuid(),
                Reference = $"REC-{index}",
                ReclamationId = index + 1,
                TechnicianId = 77,
                TechnicianName = "Tech",
                StartAt = day.AddHours(index),
                EndAt = day.AddHours(index + 1),
                EstimatedDurationMinutes = 60,
                Status = AppointmentStatus.Confirmed
            })
            .ToArray();

        var service = new PlanningCapacityService(new FakeAppointmentRepository(appointments));

        var result = await service.EvaluateAsync(77, day.AddHours(5), day.AddHours(6), 60);

        Assert.False(result.IsAvailable);
        Assert.Contains(result.Conflicts, x => x.Contains("Daily appointment capacity exceeded"));
    }

    private sealed class FakeAppointmentRepository : IAppointmentRepository
    {
        private readonly List<Appointment> _appointments;

        public FakeAppointmentRepository(params Appointment[] appointments)
        {
            _appointments = appointments.ToList();
        }

        public Task<Appointment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_appointments.FirstOrDefault(x => x.Id == id));

        public Task<List<Appointment>> QueryAsync(long? reclamationId = null, long? technicianId = null, long? clientId = null, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_appointments.ToList());

        public Task<List<Appointment>> GetTechnicianActiveAppointmentsAsync(long technicianId, DateTime from, DateTime to, Guid? excludeAppointmentId = null, CancellationToken cancellationToken = default)
        {
            var items = _appointments
                .Where(x => x.TechnicianId == technicianId)
                .Where(x => !excludeAppointmentId.HasValue || x.Id != excludeAppointmentId.Value)
                .ToList();
            return Task.FromResult(items);
        }

        public Task<Appointment?> GetActiveConfirmedByReclamationIdAsync(long reclamationId, CancellationToken cancellationToken = default)
            => Task.FromResult(_appointments.FirstOrDefault(x => x.ReclamationId == reclamationId && x.Status == AppointmentStatus.Confirmed));

        public Task AddAsync(Appointment entity, CancellationToken cancellationToken = default)
        {
            _appointments.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddAssignmentAsync(Assignment entity, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
