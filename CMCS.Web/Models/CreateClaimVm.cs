using System.ComponentModel.DataAnnotations;

namespace CMCS.Web.Models;

public sealed class CreateClaimVm
{
    [Required, DataType(DataType.Date)]
    [Display(Name = "Work Date")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required, Range(0.5, 24)]
    [Display(Name = "Hours Worked")]
    public double HoursWorked { get; set; }

    [Required, Range(50, 5000)]
    [Display(Name = "Hourly Rate (Automated from HR)")]
    public decimal HourlyRate { get; set; }

    [StringLength(500)]
    [Display(Name = "Additional Notes")]
    public string? Notes { get; set; }

    [Display(Name = "Calculated Total")]
    public decimal CalculatedTotal => Math.Round((decimal)HoursWorked * HourlyRate, 2);
}