using System.Net.Http.Json;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

public class UpdateUserDetailsEndpointTest : IdentityTestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenPostingDetailsWithoutAuthentication()
    {
        await CreateUser("Keith", "Conch", "Big Keith");

        var response = await HttpClient.PostAsync(
                           "/api/identity/details",
                           JsonContent.Create(new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest())
                       );

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldGet404ResponseWhenPostingDetailsOfMissingUser()
    {
        SetClaimsPrincipal(new User { Username = "thatsdeliciousalady@cannedsoup.com"});

        var response = await HttpClient.PostAsync(
                           "/api/identity/details",
                           JsonContent.Create(new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest())
                       );

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldUpdateUser()
    {
        var user = await CreateUser("Keith", "Conch", "Big Keith");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest("Big Karen", "Karen", "Cowrie");
        SetClaimsPrincipal(user);

        await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(request));
        var updatedUser = await GetUser(user.Id);

        updatedUser.ShouldNotBeNull();
        updatedUser.DisplayName.ShouldBe(request.DisplayName);
        updatedUser.EmailAddress.ShouldBe(user.EmailAddress);
        updatedUser.FirstName.ShouldBe(request.FirstName);
        updatedUser.LastName.ShouldBe(request.LastName);
    }
}
