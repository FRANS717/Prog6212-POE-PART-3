using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

/// <summary>The profile form. Organisers edit organisation and contact number; Participants edit date of birth.</summary>
public class ProfileViewModel : IValidatableObject
{
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }

    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter your email address.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(100)]
    public string Email { get; set; } = "";

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

    public bool IsOrganiser => Role == RaceDayValues.Organiser;

    public static ProfileViewModel From(UserProfile p) => new()
    {
        Role = p.Role,
        ProfilePictureUrl = p.ProfilePictureUrl,
        FullName = p.FullName,
        Email = p.Email,
        OrganisationName = p.OrganisationName,
        ContactNumber = p.ContactNumber,
        DateOfBirth = p.DateOfBirth
    };

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (IsOrganiser)
        {
            if (string.IsNullOrWhiteSpace(OrganisationName))
                yield return new ValidationResult("Enter your organisation name.", [nameof(OrganisationName)]);
            if (string.IsNullOrWhiteSpace(ContactNumber))
                yield return new ValidationResult("Enter a contact number.", [nameof(ContactNumber)]);
        }
        else if (DateOfBirth is null)
        {
            yield return new ValidationResult("Enter your date of birth.", [nameof(DateOfBirth)]);
        }
    }
}
