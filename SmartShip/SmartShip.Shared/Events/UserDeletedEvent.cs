namespace SmartShip.Shared.Events;

/// <summary>
/// Domain event published when a user account is removed or deactivated in IdentityService.
/// Triggers cascading cleanup or metrics update consumers across microservices (ShipmentService, PaymentService, AdminService).
/// </summary>
public class UserDeletedEvent
{
    /// <summary>
    /// Gets or sets the unique database identifier of the deleted user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the email address of the deleted account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the account deletion occurred.
    /// </summary>
    public DateTime DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the application role held by the user prior to deletion.
    /// </summary>
    public string Role { get; set; } = string.Empty;
}