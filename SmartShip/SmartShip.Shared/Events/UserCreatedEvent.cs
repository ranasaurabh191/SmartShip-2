namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a new user account (Customer, Driver, Admin) is created in IdentityService.
/// Dispatches user registration details to AdminService and other subscriber microservices.
/// </summary>
public class UserCreatedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the created user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the primary email address of the new user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the full display name of the user.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application role assigned to the user (e.g., CUSTOMER, ADMIN, DRIVER).
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the user account was registered.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}