using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

/// <summary>
/// Register form. The role decides which extra fields are needed: Organisers give an organisation and contact
/// number, Participants give a date of birth (used by the API for age categories). Checked in <see cref="Validate"/>.
/// </summary>
public class RegisterViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Confirm your password.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = "";

    [Required(ErrorMessage = "Choose whether you are an Organiser or a Participant.")]
    public string Role { get; set; } = "Participant";

    [StringLength(100)]
    [Display(Name = "Organisation name")]
    public string? OrganisationName { get; set; }

    [Phone(ErrorMessage = "Enter a valid contact number.")]
    [StringLength(20)]
    [Display(Name = "Contact number")]
    public string? ContactNumber { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateOnly? DateOfBirth { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Role == Api.RaceDayValues.Organiser)
        {
            if (string.IsNullOrWhiteSpace(OrganisationName))
                yield return new ValidationResult("Organisers must enter an organisation name.", [nameof(OrganisationName)]);
            if (string.IsNullOrWhiteSpace(ContactNumber))
                yield return new ValidationResult("Organisers must enter a contact number.", [nameof(ContactNumber)]);
        }
        else if (Role == Api.RaceDayValues.Participant)
        {
            if (DateOfBirth is null)
                yield return new ValidationResult("Participants must enter a date of birth.", [nameof(DateOfBirth)]);
            else if (DateOfBirth >= DateOnly.FromDateTime(DateTime.Today))
                yield return new ValidationResult("Date of birth must be in the past.", [nameof(DateOfBirth)]);
        }
        else
        {
            yield return new ValidationResult("Choose Organiser or Participant.", [nameof(Role)]);
        }
    }
}
