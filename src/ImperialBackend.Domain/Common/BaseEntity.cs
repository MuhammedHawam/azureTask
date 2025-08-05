namespace ImperialBackend.Domain.Common;

/// <summary>
/// Base entity class that provides common properties and functionality for all entities
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity
    /// </summary>
    public Guid Id { get; protected set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the date and time when the entity was created
    /// </summary>
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the entity was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Gets or sets the identifier of the user who created the entity
    /// </summary>
    public string? CreatedBy { get; protected set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last updated the entity
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Updates the audit information for the entity
    /// </summary>
    /// <param name="userId">The identifier of the user making the update</param>
    public void UpdateAuditInfo(string userId)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = userId;
    }

    /// <summary>
    /// Sets the creation audit information for the entity
    /// </summary>
    /// <param name="userId">The identifier of the user creating the entity</param>
    public void SetCreationInfo(string userId)
    {
        CreatedBy = userId;
    }
}