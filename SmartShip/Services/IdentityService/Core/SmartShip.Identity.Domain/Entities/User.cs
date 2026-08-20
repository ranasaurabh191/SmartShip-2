using System.ComponentModel.DataAnnotations;

namespace SmartShip.Identity.Domain.Entities;

/// Domain entity representing a user account within the SmartShip system.
/// Holds authentication credentials, personal information, role assignment, and account status.
public class User
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public string Role { get; set; } = "CUSTOMER"; 
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
