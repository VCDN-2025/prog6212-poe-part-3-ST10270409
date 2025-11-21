using CMCS.Web.Services;
using CMCS.Web.Data;
using CMCS.Web.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace CMCS.Tests;

public class RepositoryTests
{
    private AppDbContext CreateTestContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void DatabaseRepo_Add_Then_Get_Works()
    {
        // Arrange
        using var context = CreateTestContext("TestDb1");
        var repo = new DatabaseClaimRepository(context);
        var claim = new Claim
        {
            Date = DateTime.Today,
            HoursWorked = 3,
            HourlyRate = 200m,
            Notes = "demo",
            LecturerName = "Test Lecturer",
            LecturerId = Guid.NewGuid().ToString()
        };

        // Act
        repo.Add(claim);
        var loaded = repo.Get(claim.Id);

        // Assert
        loaded.Should().NotBeNull();
        loaded!.Total.Should().Be(600m);
        loaded.Notes.Should().Be("demo");
        repo.GetAll().Should().ContainSingle(c => c.Id == claim.Id);
    }

    [Fact]
    public void GetAll_ReturnsClaimsInCorrectOrder()
    {
        // Arrange
        using var context = CreateTestContext("TestDb2");
        var repo = new DatabaseClaimRepository(context);

        var claim1 = new Claim
        {
            Date = DateTime.Today.AddDays(-2),
            HoursWorked = 2,
            HourlyRate = 100m,
            LecturerName = "Lecturer 1",
            LecturerId = Guid.NewGuid().ToString()
        };

        var claim2 = new Claim
        {
            Date = DateTime.Today.AddDays(-1),
            HoursWorked = 3,
            HourlyRate = 150m,
            LecturerName = "Lecturer 2",
            LecturerId = Guid.NewGuid().ToString()
        };

        // Act
        repo.Add(claim1);
        repo.Add(claim2);
        var allClaims = repo.GetAll();

        // Assert - Should be ordered by date descending (newest first)
        allClaims.Should().HaveCount(2);
        allClaims[0].Date.Should().Be(claim2.Date); // Newer date first
        allClaims[1].Date.Should().Be(claim1.Date); // Older date last
    }

    [Fact]
    public void Get_NonExistentClaim_ReturnsNull()
    {
        // Arrange
        using var context = CreateTestContext("TestDb3");
        var repo = new DatabaseClaimRepository(context);

        // Act
        var result = repo.Get(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void UpdateClaim_ModifiesExistingClaim()
    {
        // Arrange
        using var context = CreateTestContext("TestDb4");
        var repo = new DatabaseClaimRepository(context);

        var claim = new Claim
        {
            Date = DateTime.Today,
            HoursWorked = 4,
            HourlyRate = 100m,
            LecturerName = "Original Name",
            LecturerId = Guid.NewGuid().ToString()
        };

        repo.Add(claim);

        // Act
        var loaded = repo.Get(claim.Id);
        loaded!.HoursWorked = 8;
        loaded.Notes = "Updated notes";
        repo.Update(loaded);

        // Assert
        var updated = repo.Get(claim.Id);
        updated!.HoursWorked.Should().Be(8);
        updated.Notes.Should().Be("Updated notes");
        updated.Total.Should().Be(800m); // 8 hours * 100 rate
    }
}