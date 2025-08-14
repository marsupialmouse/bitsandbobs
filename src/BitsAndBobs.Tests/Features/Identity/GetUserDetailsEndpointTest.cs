using System.Net.Http.Json;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

[TestFixture]
public class GetUserDetailsEndpointTest : IdentityTestBase
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

        var response = await HttpClient.GetFromJsonAsync<GetUserDetailsEndpoint.GetUserDetailsResponse>("/api/identity/details");

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

        var response = await HttpClient.GetFromJsonAsync<GetUserDetailsEndpoint.GetUserDetailsResponse>("/api/identity/details");

        response.ShouldNotBeNull();
        response.DisplayName.ShouldBe("bagcarrier");
    }
}
