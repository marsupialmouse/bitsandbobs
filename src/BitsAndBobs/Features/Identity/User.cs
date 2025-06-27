using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public class User : BitsAndBobsTableItem
{
    public const string SortKey = "Profile";

    /// <summary>
    /// Gets or sets the primary key for this user.
    /// </summary>
    public string Id { get; private set;  } = Guid.NewGuid().ToString("n");

    protected override string PK
    {
        get => GetPk(Id);
        set => Id = value[5..];
    }

    protected override string SK
    {
        get => SortKey;
        set { }
    }

    /// <summary>
    /// Gets or sets the username for this user.
    /// </summary>
    [ProtectedPersonalData]
    public required string Username { get; set; }

    /// <summary>
    /// Gets or sets the normalized username for this user.
    /// </summary>
    public string? NormalizedUsername { get; set; }

    /// <summary>
    /// Gets or sets the email address for this user.
    /// </summary>
    [ProtectedPersonalData]
    public required string EmailAddress { get; set; }

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
    /// Gets or sets a salted and hashed representation of the password for this user.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// A random value that must change whenever a users credentials change (password changed, login removed)
    /// </summary>
    public string? SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// A random value that must change whenever a user is persisted to the store
    /// </summary>
    public string? ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    internal static string GetPk(string id) => $"user#{id}";

    /// <summary>
    /// Returns the username for this user.
    /// </summary>
    public override string ToString() => Username;
}

