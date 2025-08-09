using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;
using StronglyTypedIds;

namespace BitsAndBobs.Features.Email;

[StronglyTypedId]
public readonly partial struct EmailId
{
    private static partial string Prefix => "email#";
}

// This class only uses the DynamoDBHashKey / DynamoDBRangeKey / DynamoDBGlobalSecondaryIndexRangeKey attributes
// because there's a bug in the SDK that manifests itself when using RegisterTableDefinition and a GSI uses the
// table's range key as one of the index keys.
[DynamoDBTable(BitsAndBobsTable.Name)]
public class EmailMessage : BitsAndBobsTable.Item
{
    public const string SortKey = "Email";

    [Obsolete("This constructor is for DynamoDB only and is none of your business.")]
    public EmailMessage()
    {
    }

    public EmailMessage(User user, string recipientEmail, string type, string body, DateTimeOffset? sentAt = null)
    {
        Id = EmailId.Create();
        SentAt = sentAt ?? DateTimeOffset.Now;
        RecipientEmail = recipientEmail;
        NormalizedRecipientEmail = recipientEmail.ToUpperInvariant();
        RecipientUserId = user.Id;
        Type = type;
        Body = body;
    }

    [DynamoDBIgnore]
    public EmailId Id { get; private set; }

    protected override string PK
    {
        get => Id.Value;
        set => Id = EmailId.Parse(value);
    }

   protected override string SK
   {
       get => SortKey;
       set { }
   }

    public string RecipientEmail { get; protected set; } = "";
    public string NormalizedRecipientEmail { get; protected set; } = "";

    [DynamoDBProperty(typeof(UserId.DynamoConverter))]
    public UserId RecipientUserId { get; protected set; }

    public string Body { get; protected set; } = null!;

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset SentAt { get; protected set; }

    // ReSharper disable once UnusedMember.Global
    protected long SentAtUtcTimeStamp
    {
        get => SentAt.UtcTicks;
        // This is settable only so the AWS SDK recognises the property
        // ReSharper disable once ValueParameterNotUsed
        set { }
    }

    [DynamoDBProperty("EmailType")]
    public string Type { get; protected set; } = null!;
}
