using System.Net.Http.Json;
using BitsAndBobs.Features.Identity;
using NUnit.Framework;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

[TestFixture]
public class DetailsEndpointTest : TestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenRequestingDetailsWithoutAuthentication()
    {
        await CreateUser("Keith", "Conch", "Big Keith");

        var response = await HttpClient.GetAsync("/api/identity/details");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldGet404ResponseWhenGettingDetailsOfMissingUser()
    {
        SetClaimsPrincipal(new User { Username = "thatsdeliciousalady@cannedsoup.com"});

        var response = await HttpClient.GetAsync("/api/identity/details");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldGetUserDetails()
    {
        var user = await CreateUser("Keith", "Conch", "Big Keith");
        SetClaimsPrincipal(user);

        var response = await HttpClient.GetFromJsonAsync<IdentityEndpoints.DetailsResponse>("/api/identity/details");

        response.ShouldNotBeNull();
        response.DisplayName.ShouldBe(user.DisplayName);
        response.EmailAddress.ShouldBe(user.EmailAddress);
        response.FirstName.ShouldBe(user.FirstName);
        response.LastName.ShouldBe(user.LastName);
    }

    [Test]
    public async Task ShouldReturnNameFromEmailAsDisplayNameWhenDisplayNameNotSet()
    {
        var user = await CreateUser(emailAddress: $"bagcarrier@{Guid.NewGuid():n}.com");
        SetClaimsPrincipal(user);

        var response = await HttpClient.GetFromJsonAsync<IdentityEndpoints.DetailsResponse>("/api/identity/details");

        response.ShouldNotBeNull();
        response.DisplayName.ShouldBe("bagcarrier");
    }

    [Test]
    public async Task ShouldGet401ResponseWhenPostingDetailsWithoutAuthentication()
    {
        await CreateUser("Keith", "Conch", "Big Keith");

        var response = await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(new IdentityEndpoints.DetailsRequest()));

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldGet404ResponseWhenPostingDetailsOfMissingUser()
    {
        SetClaimsPrincipal(new User { Username = "thatsdeliciousalady@cannedsoup.com"});

        var response = await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(new IdentityEndpoints.DetailsRequest()));

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldUpdateUser()
    {
        var user = await CreateUser("Keith", "Conch", "Big Keith");
        var request = new IdentityEndpoints.DetailsRequest("Big Karen", "Karen", "Cowrie");
        SetClaimsPrincipal(user);

        await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(request));
        var updatedUser = await GetUser(user.Id);

        updatedUser.ShouldNotBeNull();
        updatedUser.DisplayName.ShouldBe(request.DisplayName);
        updatedUser.EmailAddress.ShouldBe(user.EmailAddress);
        updatedUser.FirstName.ShouldBe(request.FirstName);
        updatedUser.LastName.ShouldBe(request.LastName);
    }

    [Test]
    public async Task ShouldEchoDetailsOfUpdatedUser()
    {
        var user = await CreateUser("Keith", "Conch", "Big Keith");
        var request = new IdentityEndpoints.DetailsRequest("Big Karen", "Karen", "Cowrie");
        SetClaimsPrincipal(user);

        var httpResponse = await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(request));
        var response = await httpResponse.Content.ReadFromJsonAsync<IdentityEndpoints.DetailsResponse>();

        response.ShouldNotBeNull();
        response.DisplayName.ShouldBe(request.DisplayName);
        response.EmailAddress.ShouldBe(user.EmailAddress);
        response.FirstName.ShouldBe(request.FirstName);
        response.LastName.ShouldBe(request.LastName);
    }

    private static async Task<User> CreateUser(string? firstName = null, string? lastName = null, string? displayName = null, string? emailAddress = null)
    {
        emailAddress ??= $"test-{Guid.NewGuid()}@example.com";

        var user = new User
        {
            EmailAddress = emailAddress,
            NormalizedEmailAddress = emailAddress.ToUpperInvariant(),
            Username = emailAddress,
            NormalizedUsername = emailAddress.ToUpperInvariant(),
            EmailAddressConfirmed = true,
            Version = Guid.NewGuid().ToString("n"),
            FirstName = firstName,
            LastName = lastName,
            DisplayName = displayName!,
        };
        await new UserStore(Testing.DynamoClient, Testing.DynamoContext).
            CreateAsync(user, TestContext.CurrentContext.CancellationToken).ConfigureAwait(false);

        return user;
    }

    private Task<User?> GetUser(string id) => new UserStore(Testing.DynamoClient, Testing.DynamoContext).FindByIdAsync(
        id,
        TestContext.CurrentContext.CancellationToken
    );
}
