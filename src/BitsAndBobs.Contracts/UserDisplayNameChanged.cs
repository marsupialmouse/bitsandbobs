namespace BitsAndBobs.Contracts;

public sealed record UserDisplayNameChanged(string UserId, string OldDisplayName, string NewDisplayName);
