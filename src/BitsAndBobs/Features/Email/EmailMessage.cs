using Amazon.DynamoDBv2.DataModel;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure;

namespace BitsAndBobs.Features.Email;

public class EmailMessage : BitsAndBobsTableItem
{
    public override string PK { get; protected set; } = null!;

    [DynamoDBGlobalSecondaryIndexRangeKey("EmailsByUserId")]
    public override string SK { get; protected set; } = null!;
    public string Recipient { get; set; } = null!;
    public string Body { get; set; } = null!;

    [DynamoDBGlobalSecondaryIndexHashKey("EmailsByUserId")]
    [DynamoDBProperty("SentToUserId")]
    public string UserId { get; set; } = null!;

    [DynamoDBProperty(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset SentAt { get; set; }

    [DynamoDBProperty("EmailType")]
    public string Type { get; set; } = null!;

    public EmailMessage()
    {
    }

    public EmailMessage(User user, string recipient, string type, string body, DateTimeOffset? sentAt = null)
    {
        SentAt = sentAt ?? DateTimeOffset.Now;
        PK = $"email#{recipient.ToUpperInvariant()}";
        SK = SentAt.UtcDateTime.ToString("O");
        Recipient = recipient;
        Type = type;
        Body = body;
        UserId = user.PK;
    }
}
