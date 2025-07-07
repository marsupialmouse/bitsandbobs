using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public class User : BitsAndBobsTableItem
{
    public const string SortKey = "Profile";

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBIgnore]
    public string Id => PK;

    public override string PK { get; protected set; } = $"user#{Guid.NewGuid():n}";

    public override string SK
    {
        get => SortKey;
        protected set { }
    }

    /// <summary>
    /// Gets or sets the username for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized username for this user.
    /// </summary>
    [DynamoDBGlobalSecondaryIndexHashKey("UsersByNormalizedUsername")]
    public string? NormalizedUsername { get; set; }

    /// <summary>
    /// Gets or sets the email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized email address for this user.
    /// </summary>
    [DynamoDBGlobalSecondaryIndexHashKey("UsersByNormalizedEmailAddress")]

    public string? NormalizedEmailAddress { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool EmailAddressConfirmed { get; set; }

    /// <summary>
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// A random value that must change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public string? SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The number of consecutive failed access attempts for this user.
    /// </summary>
    public int FailedAccessAttempts { get; set; }

    /// <summary>
    /// A flag indicating whether the user is locked out of their account due to too many failed access attempts.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// The date and time when the user will be unlocked from their account.
    /// </summary>
    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset? LockoutEndDate { get; set; }

    /// <summary>
    /// A random value that must change whenever a user is persisted to the store
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// Returns the username for this user.
    /// </summary>
    public override string ToString() => Username;
}
