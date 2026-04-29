using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Application.Services;

public class TicketClassificationService
{
    private static readonly (TicketCategory Category, string[] Keywords)[] Rules =
    {
        (TicketCategory.TechnicalFailure, new[]
        {
            "panne", "defaut", "erreur", "bug", "hs", "ne marche pas", "bloque", "bloquant"
        }),
        (TicketCategory.Installation, new[]
        {
            "installation", "installer", "mise en service", "configuration", "configurer"
        }),
        (TicketCategory.Maintenance, new[]
        {
            "maintenance", "entretien", "controle", "verification", "revision"
        }),
        (TicketCategory.SpareParts, new[]
        {
            "piece", "pieces", "piece de rechange", "composant", "remplacement"
        }),
        (TicketCategory.BillingRefund, new[]
        {
            "remboursement", "facture", "paiement", "avoir", "retour", "garantie"
        })
    };

    public TicketClassificationResult Compute(Reclamation reclamation)
    {
        var haystack = Normalize(string.Join(' ',
            reclamation.Description,
            reclamation.ProductName,
            reclamation.Brand,
            reclamation.Model,
            reclamation.ProductReference));

        foreach (var (category, keywords) in Rules)
        {
            var matchedKeyword = keywords.FirstOrDefault(haystack.Contains);
            if (matchedKeyword is not null)
            {
                return new TicketClassificationResult(category, $"Matched keyword: {matchedKeyword}");
            }
        }

        return new TicketClassificationResult(TicketCategory.Other, "No classification rule matched");
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public sealed record TicketClassificationResult(TicketCategory Category, string Reason);
