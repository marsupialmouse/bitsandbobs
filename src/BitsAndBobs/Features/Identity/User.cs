using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

[DynamoDBTable(BitsAndBobsTable.Name)]
public class User
{
    public const string SortKey = "Profile";

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBProperty("PK")]
    public string Id { get; protected set; } = $"user#{Guid.NewGuid():n}";

    public string SK
    {
        get => SortKey;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
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
    public string? NormalizedUsername { get; set; }

    /// <summary>
    /// Gets or sets the email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized email address for this user.
    /// </summary>
    public string? NormalizedEmailAddress { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating if a user has confirmed their email address.
    /// </summary>
    /// <value>True if the email address has been confirmed, otherwise false.</value>
    [PersonalData]
    public bool EmailAddressConfirmed { get; set; }

    /// <summary>
    /// Gets or sets the user's first name
    /// </summary>
    [ProtectedPersonalData]
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the user's last name
    /// </summary>
    [ProtectedPersonalData]
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets a display name, which is shown in auctions.
    /// </summary>
    [DynamoDBIgnore]
    public string DisplayName
    {
        get => DisplayNameInternal ?? EmailAddress.Split('@')[0];
        set => DisplayNameInternal = value;
    }

    [DynamoDBProperty("DisplayName")]
    protected string? DisplayNameInternal { get; set; }

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

    // This is here as the property is the hash key of a GSI and the AWS Document Model gets upset without it.
    // ReSharper disable once UnusedMember.Global
    protected string? RecipientUserId { get; set; }

    /// <summary>
    /// Returns the username for this user.
    /// </summary>
    public override string ToString() => Username;
}
