namespace CMCS.Web.Models
{
    public class ValidationResult
    {
        public bool IsValid { get; init; }
        public string ErrorMessage { get; init; } = string.Empty;

        public static ValidationResult Success() => new() { IsValid = true };

        public static ValidationResult Fail(string message) =>
            new() { IsValid = false, ErrorMessage = message };
    }
}
