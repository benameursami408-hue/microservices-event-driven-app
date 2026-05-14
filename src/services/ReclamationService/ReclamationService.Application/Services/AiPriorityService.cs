using System.Text.Json;
using ReclamationService.Application.DTOs;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Interfaces;

namespace ReclamationService.Application.Services;

public class AiPriorityService
{
    private sealed record PriorityRule(string SuggestedPriority, int ConfidenceScore, string SlaRisk, string RecommendedAction, string[] Keywords);
    private readonly IAiPriorityAnalysisRepository _repository;

    public AiPriorityService(IAiPriorityAnalysisRepository repository)
    {
        _repository = repository;
    }

    // PFE demo AI: deterministic, explainable business rules. It is not an LLM/ML model.
    private static readonly PriorityRule[] Rules =
    {
        new("Urgent", 94, "High", "Assign a technician immediately and create a priority appointment.", new[]
        {
            "production stopped", "production blocked", "production arretee", "production bloquee",
            "stopped", "down", "blocked", "critical", "fire", "danger", "smoke", "safety",
            "arrete", "bloque", "critique", "incendie", "fumee", "securite"
        }),
        new("High", 86, "Medium", "Prioritize planning and monitor SLA closely.", new[]
        {
            "generator stopped", "generateur arrete", "not working", "ne fonctionne pas",
            "failure", "broken", "leak", "overheating", "compressor",
            "panne", "casse", "fuite", "surchauffe", "compresseur"
        }),
        new("Medium", 72, "Medium", "Plan the intervention using the normal SAV queue and watch for escalation.", new[]
        {
            "noise", "slow", "intermittent", "warning", "vibration", "abnormal",
            "bruit", "lent", "alerte", "anormal"
        }),
        new("Low", 58, "Low", "Handle using the standard SAV workflow.", new[]
        {
            "cosmetic", "question", "minor", "information", "request",
            "esthetique", "mineur", "demande"
        })
    };

    public async Task<AiPriorityAnalysisDto> AnalyzeAsync(AnalyzePriorityRequestDto request, CancellationToken cancellationToken = default)
    {
        var text = NormalizeText($"{request.Description} {request.ClientImpact} {request.ProductName} {request.Brand} {request.Model}");
        var matchedRule = Rules
            .Select(rule => new { Rule = rule, Detected = DetectKeywords(text, rule.Keywords) })
            .FirstOrDefault(item => item.Detected.Count > 0);

        var rule = matchedRule?.Rule ?? Rules.Last();
        var detected = matchedRule?.Detected ?? new List<string>();

        var dto = new AiPriorityAnalysisDto
        {
            ReclamationId = request.ReclamationId,
            SuggestedPriority = rule.SuggestedPriority,
            ConfidenceScore = rule.ConfidenceScore,
            SlaRisk = rule.SlaRisk,
            Reason = BuildReason(rule, detected, request.CurrentPriority),
            RecommendedAction = rule.RecommendedAction,
            DetectedKeywords = detected,
            CreatedAt = DateTime.UtcNow
        };

        var entity = await _repository.AddAsync(new AiPriorityAnalysis
        {
            ReclamationId = request.ReclamationId,
            SuggestedPriority = dto.SuggestedPriority,
            ConfidenceScore = dto.ConfidenceScore,
            SlaRisk = dto.SlaRisk,
            Reason = dto.Reason,
            RecommendedAction = dto.RecommendedAction,
            DetectedKeywordsJson = JsonSerializer.Serialize(dto.DetectedKeywords),
            CreatedAt = dto.CreatedAt
        }, cancellationToken);

        dto.Id = entity.Id;
        dto.AnalysisId = entity.Id;
        return dto;
    }

    public async Task<AiPriorityAnalysisDto?> GetLatestAsync(long reclamationId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetLatestForReclamationAsync(reclamationId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<AiPriorityAnalysisDto?> GetByIdAsync(long analysisId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(analysisId, cancellationToken);
        return entity is null ? null : ToDto(entity);
    }

    public async Task<AiPriorityAnalysisDto?> MarkAcceptedAsync(long reclamationId, long analysisId, long userId, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(analysisId, cancellationToken);
        if (entity is null || entity.ReclamationId != reclamationId) return null;
        entity.AcceptedAt = DateTime.UtcNow;
        entity.AcceptedByUserId = userId;
        await _repository.SaveChangesAsync(cancellationToken);
        return ToDto(entity);
    }

    private static List<string> DetectKeywords(string normalizedText, IEnumerable<string> keywords)
    {
        return keywords
            .Where(keyword => normalizedText.Contains(NormalizeText(keyword), StringComparison.Ordinal))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string BuildReason(PriorityRule rule, List<string> detectedKeywords, string? currentPriority)
    {
        var current = string.IsNullOrWhiteSpace(currentPriority) ? "unknown" : currentPriority;
        if (detectedKeywords.Count == 0)
        {
            return $"No high-risk business keywords were detected. Suggested priority is {rule.SuggestedPriority} from the default rule while current priority is {current}.";
        }

        return $"Detected {rule.SuggestedPriority} business rule keywords ({string.Join(", ", detectedKeywords)}). Suggested priority is {rule.SuggestedPriority} while current priority is {current}.";
    }

    private static string NormalizeText(string value)
    {
        return value
            .ToLowerInvariant()
            .Replace('é', 'e')
            .Replace('è', 'e')
            .Replace('ê', 'e')
            .Replace('ë', 'e')
            .Replace('à', 'a')
            .Replace('â', 'a')
            .Replace('î', 'i')
            .Replace('ï', 'i')
            .Replace('ô', 'o')
            .Replace('ö', 'o')
            .Replace('ù', 'u')
            .Replace('û', 'u')
            .Replace('ü', 'u')
            .Replace('ç', 'c');
    }

    private static AiPriorityAnalysisDto ToDto(AiPriorityAnalysis entity)
    {
        var keywords = new List<string>();
        try { keywords = JsonSerializer.Deserialize<List<string>>(entity.DetectedKeywordsJson) ?? new List<string>(); } catch { }
        return new AiPriorityAnalysisDto
        {
            Id = entity.Id,
            AnalysisId = entity.Id,
            ReclamationId = entity.ReclamationId,
            SuggestedPriority = entity.SuggestedPriority,
            ConfidenceScore = entity.ConfidenceScore,
            SlaRisk = entity.SlaRisk,
            Reason = entity.Reason,
            RecommendedAction = entity.RecommendedAction,
            DetectedKeywords = keywords,
            CreatedAt = entity.CreatedAt,
            AcceptedAt = entity.AcceptedAt,
            AcceptedByUserId = entity.AcceptedByUserId
        };
    }
}
