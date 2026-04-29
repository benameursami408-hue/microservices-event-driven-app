using ReclamationService.Application.Services;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Services;

public class TicketClassificationServiceTests
{
    private readonly TicketClassificationService _service = new();

    [Fact]
    public void Compute_WithPanneKeyword_ReturnsTechnicalFailure()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-1",
            Description = "Mon produit est en panne totale",
            Priority = NamePriority.MEDUIM,
            ClientId = 1,
            ClientName = "Client"
        };

        var result = _service.Compute(reclamation);

        Assert.Equal(TicketCategory.TechnicalFailure, result.Category);
        Assert.Contains("panne", result.Reason);
    }

    [Fact]
    public void Compute_WithInstallationKeyword_ReturnsInstallation()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-2",
            Description = "Besoin d'installation et configuration du produit",
            Priority = NamePriority.MEDUIM,
            ClientId = 1,
            ClientName = "Client"
        };

        var result = _service.Compute(reclamation);

        Assert.Equal(TicketCategory.Installation, result.Category);
    }

    [Fact]
    public void Compute_WithRemboursementKeyword_ReturnsBillingRefund()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-3",
            Description = "Je demande un remboursement de la facture",
            Priority = NamePriority.MEDUIM,
            ClientId = 1,
            ClientName = "Client"
        };

        var result = _service.Compute(reclamation);

        Assert.Equal(TicketCategory.BillingRefund, result.Category);
    }

    [Fact]
    public void Compute_WithoutMatchingKeyword_ReturnsOther()
    {
        var reclamation = new Reclamation
        {
            Reference = "REC-4",
            Description = "Question generale sur mon dossier",
            Priority = NamePriority.MEDUIM,
            ClientId = 1,
            ClientName = "Client"
        };

        var result = _service.Compute(reclamation);

        Assert.Equal(TicketCategory.Other, result.Category);
    }
}
