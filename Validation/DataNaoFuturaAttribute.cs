using System.ComponentModel.DataAnnotations;

namespace FindMe.Validation;

/// <summary>Equivalente ao @PastOrPresent do Bean Validation.</summary>
[AttributeUsage(AttributeTargets.Property)]
public class DataNaoFuturaAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is DateOnly data && data > DateOnly.FromDateTime(DateTime.Now))
        {
            return new ValidationResult(ErrorMessage ?? "A data não pode estar no futuro");
        }
        return ValidationResult.Success;
    }
}
