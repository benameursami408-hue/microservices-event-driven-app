using System.Text.Json;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Services;

public class ReclamationPriorityService
{
    private static readonly string[] UrgencyKeywords =
    {
        "urgent",
        "urgence",
        "asap",
        "bloque",
        "bloquant",
        "blocking",
        "critical",
        "critique",
        "panne totale",
        "immediat",
        "immediate"
    };

    public PriorityComputation Compute(Reclamation reclamation)
    {
        var reasons = new List<string>();
        var score = 0;

        score += reclamation.Severity switch
        {
            NamePriority.LOW => 10,
            NamePriority.MEDUIM => 25,
            NamePriority.HIGH => 45,
            NamePriority.URGENT => 60,
            _ => 0
        };
        reasons.Add($"Severite declaree: {reclamation.Severity}");

        var age = DateTime.UtcNow - reclamation.CreatedAt;
        if (age.TotalDays >= 5)
        {
            score += 20;
            reasons.Add("Anciennete du dossier elevee");
        }
        else if (age.TotalDays >= 2)
        {
            score += 10;
            reasons.Add("Dossier ouvert depuis plus de 48h");
        }

        if (reclamation.IsBlocking)
        {
            score += 20;
            reasons.Add("Panne bloquante");
        }

        if (reclamation.FollowUpCount >= 4)
        {
            score += 20;
            reasons.Add("Client deja relance plusieurs fois");
        }
        else if (reclamation.FollowUpCount >= 2)
        {
            score += 10;
            reasons.Add("Relances client en hausse");
        }

        if (reclamation.SlaStatus == SlaStatus.Breached)
        {
            score += 35;
            reasons.Add("SLA depasse");
        }
        else if (reclamation.SlaStatus == SlaStatus.NearBreach)
        {
            score += 15;
            reasons.Add("Risque SLA eleve");
        }

        var keywords = FindUrgencyKeywords(reclamation.Description);
        if (keywords.Count > 0)
        {
            score += 15;
            reasons.Add($"Description contient mots d'urgence: {string.Join(", ", keywords)}");
        }

        var level = score switch
        {
            >= 85 => NamePriority.URGENT,
            >= 55 => NamePriority.HIGH,
            >= 25 => NamePriority.MEDUIM,
            _ => NamePriority.LOW
        };

        return new PriorityComputation(level, score, reasons);
    }

    public void ApplyAutomaticPriority(Reclamation reclamation)
    {
        var result = Compute(reclamation);
        reclamation.Priority = result.Level;
        reclamation.PriorityScore = result.Score;
        reclamation.PriorityReasons = JsonSerializer.Serialize(result.Reasons);
        reclamation.PrioritySource = PrioritySource.Rules;
        reclamation.PriorityUpdatedAt = DateTime.UtcNow;
    }

    private static List<string> FindUrgencyKeywords(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new List<string>();
        }

        var normalized = description.ToLowerInvariant();
        return UrgencyKeywords.Where(normalized.Contains).Distinct().Take(3).ToList();
    }
}

public sealed record PriorityComputation(NamePriority Level, int Score, List<string> Reasons);
