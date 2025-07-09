using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Email;

// This class only uses the DynamoDBHashKey / DynamoDBRangeKey / DynamoDBGlobalSecondaryIndexRangeKey attributes
// because there's a bug in the SDK that manifests itself when using RegisterTableDefinition and a GSI uses the
// table's range key as one of the index keys.
[DynamoDBTable(BitsAndBobsTable.Name)]
public class EmailMessage
{
    [DynamoDBHashKey]
    public string PK { get; protected set; } = null!;

    [DynamoDBRangeKey]
    [DynamoDBGlobalSecondaryIndexRangeKey("EmailsByUserId")]
    public string SK { get; protected set; } = null!;
    public string RecipientEmail { get; protected set; } = null!;

    [DynamoDBGlobalSecondaryIndexHashKey("EmailsByUserId")]
    public string RecipientUserId { get; protected set; } = null!;
    public string Body { get; protected set; } = null!;

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset SentAt { get; protected set; }

    [DynamoDBProperty("EmailType")]
    public string Type { get; protected set; } = null!;

    public EmailMessage()
    {
    }

    public EmailMessage(User user, string recipientEmail, string type, string body, DateTimeOffset? sentAt = null)
    {
        SentAt = sentAt ?? DateTimeOffset.Now;
        PK = $"email#{recipientEmail.ToUpperInvariant()}";
        SK = SentAt.UtcDateTime.ToString("O");
        RecipientEmail = recipientEmail;
        RecipientUserId = user.PK;
        Type = type;
        Body = body;
    }
}
