using System.Net.Http.Json;
using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Identity;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Identity;

public class UpdateUserDetailsEndpointTest : IdentityTestBase
{
    [Test]
    public async Task ShouldGet401ResponseWhenPostingDetailsWithoutAuthentication()
    {
        await CreateUser(displayName: "Big Keith", firstName: "Keith", lastName: "Conch");

        var response = await HttpClient.PostAsJsonAsync(
                           "/api/identity/details",
                           new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest("Dave")
                       );

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task ShouldGet404ResponseWhenPostingDetailsOfMissingUser()
    {
        SetClaimsPrincipal(new User { Username = "thatsdeliciousalady@cannedsoup.com"});

        var response = await HttpClient.PostAsJsonAsync(
                           "/api/identity/details",
                           new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest("Woodrow")
                       );

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ShouldReturnValidationProblemForEmptyDisplayName()
    {
        await CreateAuthenticatedUser(displayName: "Bjarne");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(DisplayName: "");

        var response = await HttpClient.PostAsJsonAsync("/api/identity/details", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ShouldUpdateUser()
    {
        var user = await CreateAuthenticatedUser(displayName: "Big Keith", firstName: "Keith", lastName: "Conch");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "Big Karen",
            FirstName: "Karen",
            LastName: "Cowrie"
        );

        await HttpClient.PostAsJsonAsync("/api/identity/details", request);
        var updatedUser = await GetUser(user.Id);

        updatedUser.ShouldNotBeNull();
        updatedUser.DisplayName.ShouldBe(request.DisplayName);
        updatedUser.EmailAddress.ShouldBe(user.EmailAddress);
        updatedUser.FirstName.ShouldBe(request.FirstName);
        updatedUser.LastName.ShouldBe(request.LastName);
    }

    [Test]
    public async Task ShouldPublishEventWhenDisplayNameChanges()
    {
        var user = await CreateAuthenticatedUser(displayName: "Video Evidence", firstName: "Barry", lastName: "Football");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "Apologist",
            FirstName: "Barry",
            LastName: "Football"
        );

        await HttpClient.PostAsJsonAsync("/api/identity/details", request);

        var message = await GetPublishedMessage<UserDisplayNameChanged>();
        message.ShouldNotBeNull();
        message.UserId.ShouldBe(user.Id.Value);
        message.OldDisplayName.ShouldBe("Video Evidence");
        message.NewDisplayName.ShouldBe("Apologist");
    }

    [Test]
    public async Task ShouldNotPublishNamedChangedEventWhenNameDoesNotChange()
    {
        var user = await CreateAuthenticatedUser(displayName: "FNTSP", firstName: "Puffy", lastName: "Dog");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "FNTSP",
            FirstName: "Puff",
            LastName: "Doggy"
        );

        await HttpClient.PostAsJsonAsync("/api/identity/details", request);

        (await Messaging.Published.Any<UserDisplayNameChanged>()).ShouldBeFalse();
    }
}
