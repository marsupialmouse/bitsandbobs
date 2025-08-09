using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Identity;
using StronglyTypedIds;

namespace BitsAndBobs.Features.Identity;

[StronglyTypedId]
public readonly partial struct UserId
{
    private static partial string Prefix => "user#";
}

public class User : BitsAndBobsTable.VersionedEntity
{
    public const string SortKey = "Profile";

    /// <summary>
    /// Gets the user ID.
    /// </summary>
    [DynamoDBIgnore]
    public UserId Id { get; protected set; } = UserId.Create();

    protected override string PK
    {
        get => Id.Value;
        set => Id = UserId.Parse(value);
    }

    protected override string SK
    {
        get => SortKey;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        set { }
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
    /// Returns the username for this user.
    /// </summary>
    public override string ToString() => Username;

    public void UpdateConcurrency() => UpdateVersion();
}
