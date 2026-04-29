using InterventionService.Application.DTOs;
using InterventionService.Domain.Entities;
using InterventionService.Domain.Interfaces;

namespace InterventionService.Application.Services;

public class PlanningCapacityService
{
    private const int DefaultBufferMinutes = 30;
    private const int DefaultDailyMaxAppointments = 4;
    private const int DefaultWeeklyMaxAppointments = 18;
    private const int DefaultDailyMaxMinutes = 8 * 60;
    private const int DefaultWeeklyMaxMinutes = 5 * 8 * 60;

    private readonly IAppointmentRepository _appointmentRepository;

    public PlanningCapacityService(IAppointmentRepository appointmentRepository)
    {
        _appointmentRepository = appointmentRepository;
    }

    public async Task<ScheduleEvaluation> EvaluateAsync(
        long technicianId,
        DateTime startAt,
        DateTime? endAt,
        int estimatedDurationMinutes,
        Guid? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (estimatedDurationMinutes <= 0)
        {
            return new ScheduleEvaluation(false, new[] { "Estimated duration must be positive." }, null);
        }

        var effectiveEndAt = endAt ?? startAt.AddMinutes(estimatedDurationMinutes);
        if (effectiveEndAt <= startAt)
        {
            return new ScheduleEvaluation(false, new[] { "Appointment end time must be after start time." }, null);
        }

        var bufferWindowStart = startAt.AddMinutes(-DefaultBufferMinutes);
        var bufferWindowEnd = effectiveEndAt.AddMinutes(DefaultBufferMinutes);
        var appointments = await _appointmentRepository.GetTechnicianActiveAppointmentsAsync(
            technicianId,
            bufferWindowStart.AddDays(-7),
            bufferWindowEnd.AddDays(7),
            excludeAppointmentId,
            cancellationToken);

        var conflicts = new List<string>();
        foreach (var existing in appointments)
        {
            var existingEnd = existing.EndAt ?? existing.StartAt.AddMinutes(existing.EstimatedDurationMinutes);
            if (startAt < existingEnd && effectiveEndAt > existing.StartAt)
            {
                conflicts.Add($"Overlap with appointment {existing.Reference} from {existing.StartAt:u} to {existingEnd:u}.");
                continue;
            }

            var bufferBefore = startAt < existingEnd.AddMinutes(DefaultBufferMinutes) && startAt >= existingEnd;
            var bufferAfter = effectiveEndAt > existing.StartAt.AddMinutes(-DefaultBufferMinutes) && effectiveEndAt <= existing.StartAt;
            if (bufferBefore || bufferAfter)
            {
                conflicts.Add($"Insufficient buffer with appointment {existing.Reference}.");
            }
        }

        var dayStart = startAt.Date;
        var dayEnd = dayStart.AddDays(1);
        var dayAppointments = appointments
            .Where(x => x.StartAt >= dayStart && x.StartAt < dayEnd)
            .ToList();
        var weekStart = dayStart.AddDays(-(int)((7 + dayStart.DayOfWeek - DayOfWeek.Monday) % 7));
        var weekEnd = weekStart.AddDays(7);
        var weekAppointments = appointments
            .Where(x => x.StartAt >= weekStart && x.StartAt < weekEnd)
            .ToList();

        var dailyBookedMinutes = dayAppointments.Sum(GetDurationMinutes);
        var weeklyBookedMinutes = weekAppointments.Sum(GetDurationMinutes);

        if (dayAppointments.Count + 1 > DefaultDailyMaxAppointments)
        {
            conflicts.Add("Daily appointment capacity exceeded for this technician.");
        }

        if (weekAppointments.Count + 1 > DefaultWeeklyMaxAppointments)
        {
            conflicts.Add("Weekly appointment capacity exceeded for this technician.");
        }

        if (dailyBookedMinutes + estimatedDurationMinutes > DefaultDailyMaxMinutes)
        {
            conflicts.Add("Daily workload capacity exceeded for this technician.");
        }

        if (weeklyBookedMinutes + estimatedDurationMinutes > DefaultWeeklyMaxMinutes)
        {
            conflicts.Add("Weekly workload capacity exceeded for this technician.");
        }

        var capacity = BuildCapacitySnapshot(
            technicianId,
            dayStart,
            weekStart,
            dayAppointments.Count,
            dailyBookedMinutes,
            weekAppointments.Count,
            weeklyBookedMinutes);

        return new ScheduleEvaluation(conflicts.Count == 0, conflicts, capacity);
    }

    public async Task<TechnicianCapacityDto> GetCapacityAsync(long technicianId, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = (date ?? DateTime.UtcNow).Date;
        var weekStart = targetDate.AddDays(-(int)((7 + targetDate.DayOfWeek - DayOfWeek.Monday) % 7));
        var appointments = await _appointmentRepository.GetTechnicianActiveAppointmentsAsync(
            technicianId,
            weekStart,
            weekStart.AddDays(7),
            null,
            cancellationToken);

        var dayAppointments = appointments.Where(x => x.StartAt >= targetDate && x.StartAt < targetDate.AddDays(1)).ToList();
        var weekAppointments = appointments.Where(x => x.StartAt >= weekStart && x.StartAt < weekStart.AddDays(7)).ToList();

        return BuildCapacitySnapshot(
            technicianId,
            targetDate,
            weekStart,
            dayAppointments.Count,
            dayAppointments.Sum(GetDurationMinutes),
            weekAppointments.Count,
            weekAppointments.Sum(GetDurationMinutes));
    }

    private static TechnicianCapacityDto BuildCapacitySnapshot(
        long technicianId,
        DateTime date,
        DateTime weekStart,
        int dailyAppointments,
        int dailyMinutes,
        int weeklyAppointments,
        int weeklyMinutes)
    {
        return new TechnicianCapacityDto
        {
            TechnicianId = technicianId,
            Date = date,
            WeekStart = weekStart,
            DailyMaxAppointments = DefaultDailyMaxAppointments,
            DailyAssignedAppointments = dailyAppointments,
            DailyMaxMinutes = DefaultDailyMaxMinutes,
            DailyBookedMinutes = dailyMinutes,
            WeeklyMaxAppointments = DefaultWeeklyMaxAppointments,
            WeeklyAssignedAppointments = weeklyAppointments,
            WeeklyMaxMinutes = DefaultWeeklyMaxMinutes,
            WeeklyBookedMinutes = weeklyMinutes,
            BufferMinutes = DefaultBufferMinutes,
            DailyLoadPercent = DefaultDailyMaxMinutes == 0 ? 0 : Math.Round((double)dailyMinutes / DefaultDailyMaxMinutes * 100, 1),
            WeeklyLoadPercent = DefaultWeeklyMaxMinutes == 0 ? 0 : Math.Round((double)weeklyMinutes / DefaultWeeklyMaxMinutes * 100, 1),
            RemainingDailyAppointments = Math.Max(0, DefaultDailyMaxAppointments - dailyAppointments),
            RemainingWeeklyAppointments = Math.Max(0, DefaultWeeklyMaxAppointments - weeklyAppointments)
        };
    }

    private static int GetDurationMinutes(Appointment appointment)
    {
        var end = appointment.EndAt ?? appointment.StartAt.AddMinutes(appointment.EstimatedDurationMinutes);
        return Math.Max(appointment.EstimatedDurationMinutes, (int)Math.Ceiling((end - appointment.StartAt).TotalMinutes));
    }
}

public sealed record ScheduleEvaluation(
    bool IsAvailable,
    IReadOnlyList<string> Conflicts,
    TechnicianCapacityDto? Capacity);
