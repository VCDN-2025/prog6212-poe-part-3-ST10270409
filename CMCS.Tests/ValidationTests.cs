using System.ComponentModel.DataAnnotations;
using CMCS.Web.Models;
using FluentAssertions;

namespace CMCS.Tests;

public class ValidationTests
{
    [Fact]
    public void Claim_Total_Computes_Correctly()
    {
        var c = new Claim { Date = DateTime.Today, HoursWorked = 2.5, HourlyRate = 123.45m };
        c.Total.Should().Be(308.63m); // 2.5 * 123.45
    }

    [Fact]
    public void CreateClaimVm_Requires_Valid_Ranges()
    {
        var vm = new CreateClaimVm
        {
            Date = DateTime.Today,
            HoursWorked = 0.1, // invalid: min 0.5
            HourlyRate = 10m,  // invalid: min 50
            Notes = new string('x', 300) // invalid: > 250
        };

        var ctx = new ValidationContext(vm);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(vm, ctx, results, true);

        valid.Should().BeFalse();
        results.Should().NotBeEmpty();
    }
}
