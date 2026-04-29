namespace InterventionService.Domain.Enums;

public enum InterventionOutcome
{
    Solved = 0,
    TemporaryFix = 1,
    NeedsReplanning = 2,
    NeedsPart = 3,
    UnableToAccess = 4,
    CustomerAbsent = 5,
    NotRepairable = 6
}
