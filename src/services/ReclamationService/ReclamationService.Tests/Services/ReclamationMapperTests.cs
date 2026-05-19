using ReclamationService.Application.DTOs;
using ReclamationService.Application.Mappers;
using ReclamationService.Domain.Entities;
using ReclamationService.Domain.Enums;

namespace ReclamationService.Tests.Services;

public class ReclamationMapperTests
{
    [Fact]
    public void ToEntity_WhenPriorityMissing_MarksPendingReviewWithoutMediumDefault()
    {
        var dto = new CreateReclamationDto
        {
            Description = "Production is blocked.",
            ProductName = "Generator"
        };

        var reclamation = dto.ToEntity(10, "Client");

        Assert.Equal(NamePriority.LOW, reclamation.Priority);
        Assert.Equal(NamePriority.LOW, reclamation.Severity);
        Assert.Equal(PrioritySource.PendingReview, reclamation.PrioritySource);
        Assert.False(reclamation.ManualPriorityOverride);
        Assert.Equal(0, reclamation.PriorityScore);
        Assert.Contains("awaiting AI/SAV review", reclamation.PriorityReasons ?? string.Empty);
    }

    [Fact]
    public void ToEntity_WhenPriorityProvided_MarksManualOverride()
    {
        var dto = new CreateReclamationDto
        {
            Description = "Created by SAV.",
            Priority = NamePriority.HIGH
        };

        var reclamation = dto.ToEntity(10, "Client");

        Assert.Equal(NamePriority.HIGH, reclamation.Priority);
        Assert.Equal(NamePriority.HIGH, reclamation.Severity);
        Assert.Equal(PrioritySource.ManualOverride, reclamation.PrioritySource);
        Assert.True(reclamation.ManualPriorityOverride);
    }

    [Fact]
    public void ApplyUpdate_OverwritesAndClearsEditableFields()
    {
        var reclamation = new Reclamation
        {
            Id = 42,
            Reference = "REC-20260424-ABC123",
            Description = "Old description",
            Priority = NamePriority.HIGH,
            Severity = NamePriority.HIGH,
            Status = ReclamationStatus.Open,
            ClientId = 10,
            ClientName = "Client",
            IsBlocking = true,
            FollowUpCount = 7,
            ProductName = "Old product",
            Barcode = "123456",
            ProductImageUrl = "/uploads/reclamations/old-image.png",
            PurchaseDate = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Brand = "Old brand",
            Model = "Old model",
            SerialNumber = "OLD-SN",
            ProductReference = "OLD-REF",
            SellerName = "Old seller",
            PurchaseProofUrl = "/uploads/reclamations/old-proof.pdf",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var dto = new UpdateReclamationDto
        {
            Description = "Updated description",
            Priority = NamePriority.LOW,
            IsBlocking = false,
            FollowUpCount = 0,
            ProductName = null,
            Barcode = string.Empty,
            ProductImageUrl = null,
            PurchaseDate = null,
            Brand = "New brand",
            Model = null,
            SerialNumber = string.Empty,
            ProductReference = null,
            SellerName = "New seller",
            PurchaseProofUrl = null
        };

        reclamation.ApplyUpdate(dto);

        Assert.Equal("Updated description", reclamation.Description);
        Assert.Equal(NamePriority.LOW, reclamation.Severity);
        Assert.False(reclamation.IsBlocking);
        Assert.Equal(0, reclamation.FollowUpCount);
        Assert.Null(reclamation.ProductName);
        Assert.Equal(string.Empty, reclamation.Barcode);
        Assert.Null(reclamation.ProductImageUrl);
        Assert.Null(reclamation.PurchaseDate);
        Assert.Equal("New brand", reclamation.Brand);
        Assert.Null(reclamation.Model);
        Assert.Equal(string.Empty, reclamation.SerialNumber);
        Assert.Null(reclamation.ProductReference);
        Assert.Equal("New seller", reclamation.SellerName);
        Assert.Null(reclamation.PurchaseProofUrl);
    }
}
