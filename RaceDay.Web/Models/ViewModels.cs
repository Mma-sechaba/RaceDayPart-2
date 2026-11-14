using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models;

public sealed class EventViewModel
{
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string EventName { get => Name; set => Name = value; }
    public string? Description { get; set; }
    public DateTime Date { get; set; }
    public DateTime EventDate { get => Date; set => Date = value; }
    public string Location { get; set; } = "";
    public decimal Distance { get; set; }
    public decimal DistanceKm { get => Distance; set => Distance = value; }
    public string EventType { get; set; } = "";
    public string Status { get; set; } = "";
    public int OrganiserId { get; set; }
    public string? BannerImageUrl { get; set; }
    public List<CategoryViewModel> Categories { get; set; } = [];
}

public sealed class CategoryViewModel
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string Name { get; set; } = "";
    public string CategoryName { get => Name; set => Name = value; }
    public int MinimumAge { get; set; }
    public int MaximumAge { get; set; }
    public decimal Distance { get; set; }
    public decimal Price { get; set; }
    public int? Capacity { get; set; }
    public int EnrolledCount { get; set; }
    public string EventName { get; set; } = "";
}

public sealed class RegistrationViewModel
{
    public int RegistrationId { get; set; }
    public int ParticipantId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public DateTime RegistrationDate { get; set; }
    public string Status { get; set; } = "";
    public string ParticipantName { get; set; } = "";
    public string EventName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public string Email { get; set; } = "";
    public List<ResultViewModel> Results { get; set; } = [];
}

public sealed class ResultViewModel
{
    public int ResultId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int Position { get; set; }
    public string EventName { get; set; } = "";
    public DateTime? EventDate { get; set; }
    public string Category { get; set; } = "";
    public int RegistrationId { get; set; }
    public string Participant { get; set; } = "";
    public string ParticipantName { get => Participant; set => Participant = value; }
    public string Status { get; set; } = "Finished";
    public int TotalFinishers { get; set; }
    public string PositionOfNFinishers { get; set; } = "";
}

public sealed class ProfileViewModel
{
    public int UserId { get; set; }
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Phone, StringLength(20)] public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}

public sealed class LoginViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
}

public sealed class RegisterViewModel
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
    [DataType(DataType.Date)] public DateTime? DateOfBirth { get; set; }
    [Phone, StringLength(20)] public string? Phone { get; set; }
}

public sealed class EventFormViewModel
{
    public int EventId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    [Required, DataType(DataType.Date)] public DateTime Date { get; set; } = DateTime.Today;
    [Required, StringLength(150)] public string Location { get; set; } = "";
    [Range(0.01, 9999)] public decimal Distance { get; set; }
    [Required] public string EventType { get; set; } = "Run";
    public string? BannerImageUrl { get; set; }
    public IFormFile? BannerFile { get; set; }
}

public sealed class CategoryFormViewModel
{
    public int CategoryId { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(0, 120)] public int MinimumAge { get; set; }
    [Range(0, 120)] public int MaximumAge { get; set; }
    [Range(0.01, 9999)] public decimal Distance { get; set; }
    [Range(0, 999999.99)] public decimal Price { get; set; }
    [Range(1, int.MaxValue)] public int? Capacity { get; set; }
    [Range(1, int.MaxValue)] public int EventId { get; set; }
}

public sealed class ImageUploadViewModel
{
    [Required] public IFormFile? File { get; set; }
}

public sealed class WeatherAlertFormViewModel
{
    [Range(1, int.MaxValue)] public int EventId { get; set; }
    [Required, StringLength(100)] public string AlertType { get; set; } = "";
    [Required, StringLength(100)] public string Message { get; set; } = "";
}

public sealed class RegistrationFormViewModel
{
    [Range(1, int.MaxValue)] public int EventId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
}

public sealed class PasswordFormViewModel
{
    [Required, DataType(DataType.Password)] public string CurrentPassword { get; set; } = "";
    [Required, MinLength(8), DataType(DataType.Password)] public string NewPassword { get; set; } = "";
}

public sealed class ResultFormViewModel
{
    public int ResultId { get; set; }
    [Range(1, int.MaxValue)] public int RegistrationId { get; set; }
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int Position { get; set; } = 1;
    [Required] public string Status { get; set; } = "Finished";
}

public sealed class StatusFormViewModel
{
    public int RegistrationId { get; set; }
    [Required] public string Status { get; set; } = "Pending";
}

public sealed class LoginResponseViewModel
{
    public int UserId { get; set; }
    public string Role { get; set; } = "";
    public string Email { get; set; } = "";
    public string? FullName { get; set; }
    public string? ProfilePictureUrl { get; set; }
}

public sealed class ProfilePictureResponseViewModel
{
    public string? ProfilePictureUrl { get; set; }
}
