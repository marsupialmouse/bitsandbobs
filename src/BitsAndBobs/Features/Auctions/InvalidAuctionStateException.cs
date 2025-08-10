namespace BitsAndBobs.Features.Auctions;

public class InvalidAuctionStateException : Exception
{
    public InvalidAuctionStateException()
    {
    }

    public InvalidAuctionStateException(string message) : base(message)
    {
    }
}
