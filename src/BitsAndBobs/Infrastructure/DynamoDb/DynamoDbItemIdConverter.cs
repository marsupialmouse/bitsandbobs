using System.Globalization;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using BitsAndBobs.Features.Auctions;

namespace BitsAndBobs.Infrastructure.DynamoDb;


public class DynamoConverter : Amazon.DynamoDBv2.DataModel.IPropertyConverter
{
    public Amazon.DynamoDBv2.DocumentModel.DynamoDBEntry ToEntry(object value)
    {
        if (value is not AuctionImageId idValue)
            throw new ArgumentException($"Value must be of type {nameof(AuctionImageId)}", nameof(value));

        if (idValue  == AuctionImageId.Empty)
            throw new ArgumentException("ID value cannot be empty", nameof(value));

        return new Amazon.DynamoDBv2.DocumentModel.Primitive(idValue.Value, false);
    }

    public object FromEntry(Amazon.DynamoDBv2.DocumentModel.DynamoDBEntry entry)
    {
        var stringValue = entry.AsString();

        if (!string.IsNullOrEmpty(stringValue)
            && AuctionImageId.TryParse(stringValue, null, out var value))
        {
            return value;
        }

        throw new ArgumentException($"Serialized value is not a valid {nameof(AuctionImageId)}", nameof(entry));
    }
}
