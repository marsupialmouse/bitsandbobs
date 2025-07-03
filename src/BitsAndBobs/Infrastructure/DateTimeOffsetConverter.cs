using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

namespace BitsAndBobs.Infrastructure;

/// <summary>
/// Handles conversion of <see cref="DateTimeOffset"/> values to and from DynamoDB entries.
/// </summary>
public class DateTimeOffsetConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is not DateTimeOffset dateTimeValue)
            throw new ArgumentException("Value must be of type DateTimeOffset", nameof(value));

        return new Primitive(dateTimeValue.ToString("o", CultureInfo.InvariantCulture), false);
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        var stringValue = entry.AsString();

        if (!string.IsNullOrEmpty(stringValue)
            && DateTimeOffset.TryParseExact(
                stringValue,
                "o",
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var value
            ))
        {
            return value;
        }

        throw new ArgumentException("Serialized value is not a valid DateTimeOffset", nameof(entry));
    }
}
