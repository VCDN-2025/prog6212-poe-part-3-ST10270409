using CMCS.Web.Services;
using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace CMCS.Tests;

public class StatusFlowTests
{
    private AppDbContext CreateTestContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void Pending_To_Verified_To_Approved()
    {
        // Arrange
        using var context = CreateTestContext("TestDb5");
        var repo = new DatabaseClaimRepository(context);
        var c = new Claim
        {
            Date = DateTime.Today,
            HoursWorked = 1,
            HourlyRate = 100m,
            LecturerName = "Test Lecturer",
            LecturerId = Guid.NewGuid().ToString()
        };

        // Act & Assert - Start with Pending
        repo.Add(c);
        var loaded = repo.Get(c.Id);
        loaded!.Status.Should().Be(ClaimStatus.Pending);

        // Transition to VerifiedByCoordinator
        loaded.Status = ClaimStatus.VerifiedByCoordinator;
        loaded.VerifiedAt = DateTime.UtcNow;
        repo.Update(loaded);

        var verified = repo.Get(c.Id);
        verified!.Status.Should().Be(ClaimStatus.VerifiedByCoordinator);
        verified.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Transition to ApprovedByManager
        loaded.Status = ClaimStatus.ApprovedByManager;
        loaded.ApprovedAt = DateTime.UtcNow;
        repo.Update(loaded);

        var approved = repo.Get(c.Id);
        approved!.Status.Should().Be(ClaimStatus.ApprovedByManager);
        approved.ApprovedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Pending_To_Rejected()
    {
        // Arrange
        using var context = CreateTestContext("TestDb6");
        var repo = new DatabaseClaimRepository(context);
        var c = new Claim
        {
            Date = DateTime.Today,
            HoursWorked = 2,
            HourlyRate = 150m,
            LecturerName = "Test Lecturer",
            LecturerId = Guid.NewGuid().ToString()
        };

        // Act & Assert
        repo.Add(c);
        var loaded = repo.Get(c.Id);
        loaded!.Status.Should().Be(ClaimStatus.Pending);

        loaded.Status = ClaimStatus.Rejected;
        repo.Update(loaded);

        var rejected = repo.Get(c.Id);
        rejected!.Status.Should().Be(ClaimStatus.Rejected);
    }

    [Fact]
    public void Claim_WithDocuments_Workflow()
    {
        // Arrange
        using var context = CreateTestContext("TestDb7");
        var repo = new DatabaseClaimRepository(context);

        var claim = new Claim
        {
            Date = DateTime.Today,
            HoursWorked = 5,
            HourlyRate = 200m,
            LecturerName = "Test Lecturer",
            LecturerId = Guid.NewGuid().ToString()
        };

        // Add documents to claim
        claim.Documents.Add(new ClaimDocument
        {
            OriginalFileName = "document.pdf",
            StoredFileName = "encrypted_file.bin",
            SizeBytes = 1024
        });

        // Act & Assert
        repo.Add(claim);
        var loaded = repo.Get(claim.Id);

        loaded.Should().NotBeNull();
        loaded!.Documents.Should().HaveCount(1);
        loaded.Documents.First().OriginalFileName.Should().Be("document.pdf");

        // Update status with documents
        loaded.Status = ClaimStatus.VerifiedByCoordinator;
        repo.Update(loaded);

        var verified = repo.Get(claim.Id);
        verified!.Status.Should().Be(ClaimStatus.VerifiedByCoordinator);
        verified.Documents.Should().HaveCount(1); // Documents should persist
    }
}