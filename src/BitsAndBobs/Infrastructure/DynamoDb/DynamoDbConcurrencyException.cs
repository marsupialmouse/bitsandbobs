namespace BitsAndBobs.Infrastructure.DynamoDb;

public class DynamoDbConcurrencyException : Exception
{
    public DynamoDbConcurrencyException()
    {
    }

    public DynamoDbConcurrencyException(string message) : base(message)
    {
    }
}
