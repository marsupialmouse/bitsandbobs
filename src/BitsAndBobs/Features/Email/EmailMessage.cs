using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;

namespace BitsAndBobs.Features.Email;

// This class only uses the DynamoDBHashKey / DynamoDBRangeKey / DynamoDBGlobalSecondaryIndexRangeKey attributes
// because there's a bug in the SDK that manifests itself when using RegisterTableDefinition and a GSI uses the
// table's range key as one of the index keys.
[DynamoDBTable(BitsAndBobsTable.Name)]
public class EmailMessage //: BitsAndBobsTable.Item
{
    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    public EmailMessage()
    {
    }

    public EmailMessage(User user, string recipientEmail, string type, string body, DateTimeOffset? sentAt = null)
    {
        SentAt = sentAt ?? DateTimeOffset.Now;
        HashKey = $"email#{recipientEmail.ToUpperInvariant()}";
        RangeKey = SentAt.UtcDateTime.ToString("O");
        RecipientEmail = recipientEmail;
        RecipientUserId = user.Id;
        Type = type;
        Body = body;
    }

    [DynamoDBIgnore]
    public string HashKey { get; private set; } = "";

    protected string PK
    {
        get => HashKey;
        set => HashKey = value;
    }

    [DynamoDBIgnore]
    public string RangeKey { get; private set; } = "";

   [DynamoDBGlobalSecondaryIndexRangeKey("EmailsByUserId")]
   protected string SK
   {
       get => RangeKey;
       set => RangeKey = value;
   }

    public string RecipientEmail { get; protected set; } = null!;

    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    [DynamoDBGlobalSecondaryIndexHashKey("EmailsByUserId")]
    public UserId RecipientUserId { get; protected set; }

    public string Body { get; protected set; } = null!;

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset SentAt { get; protected set; }

    [DynamoDBProperty("EmailType")]
    public string Type { get; protected set; } = null!;
}
