using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Microsoft.AspNetCore.Identity;

namespace BitsAndBobs.Features.Identity;

public class UserStore : IUserStore<User>, IUserEmailStore<User>, IUserPasswordStore<User>
{
    private readonly IAmazonDynamoDB _db;
    private readonly IDynamoDBContext _context;

    public UserStore(IAmazonDynamoDB db, IDynamoDBContext context)
    {
        _db = db;
        _context = context;
    }

    public async Task<IdentityResult> CreateAsync(User user, CancellationToken cancellationToken)
    {
        await _context.SaveAsync(user, cancellationToken);

        return IdentityResult.Success;
    }

    public Task<IdentityResult> UpdateAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<IdentityResult> DeleteAsync(User user, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<User?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<User?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => throw new NotImplementedException();

    public Task<string> GetUserIdAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.Username)!;
    public Task<string?> GetNormalizedUserNameAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedUsername);
    public Task<string?> GetEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.EmailAddress)!;
    public Task<string?> GetNormalizedEmailAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.NormalizedEmailAddress);
    public Task<bool> GetEmailConfirmedAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.EmailAddressConfirmed);
    public Task<string?> GetPasswordHashAsync(User user, CancellationToken cancellationToken) => Task.FromResult(user.PasswordHash);
    public Task<bool> HasPasswordAsync(User user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task SetUserNameAsync(User user, string? userName, CancellationToken cancellationToken)
    {
        user.Username = userName ?? throw new ArgumentNullException(nameof(userName), "User name cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetNormalizedUserNameAsync(User user, string? normalizedName, CancellationToken cancellationToken)
    {
        user.NormalizedUsername = normalizedName ?? throw new ArgumentNullException(nameof(normalizedName), "NormalizedUserName cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetEmailAsync(User user, string? email, CancellationToken cancellationToken)
    {
        user.EmailAddress = email ?? throw new ArgumentNullException(nameof(email), "Email cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetEmailConfirmedAsync(User user, bool confirmed, CancellationToken cancellationToken)
    {
        user.EmailAddressConfirmed = confirmed;
        return Task.CompletedTask;
    }

    public Task SetNormalizedEmailAsync(User user, string? normalizedEmail, CancellationToken cancellationToken)
    {
        user.NormalizedEmailAddress = normalizedEmail ?? throw new ArgumentNullException(nameof(normalizedEmail), "Normalized email cannot be null.");
        return Task.CompletedTask;
    }

    public Task SetPasswordHashAsync(User user, string? passwordHash, CancellationToken cancellationToken)
    {
        user.PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash), "Password hash cannot be null.");
        return Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}
