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
        await CreateUser(firstName: "Keith", lastName: "Conch", displayName: "Big Keith");

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
        var user = await CreateUser(firstName: "Keith", lastName: "Conch", displayName: "Big Keith");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "Big Karen",
            FirstName: "Karen",
            LastName: "Cowrie"
        );
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
    public async Task ShouldPublishEventWhenDisplayNameChanges()
    {
        var user = await CreateUser(firstName: "Barry", lastName: "Football", displayName: "Video Evidence");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "Apologist",
            FirstName: "Barry",
            LastName: "Football"
        );
        SetClaimsPrincipal(user);

        await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(request));

        var message = await GetPublishedMessage<UserDisplayNameChanged>();
        message.ShouldNotBeNull();
        message.UserId.ShouldBe(user.Id.Value);
        message.OldDisplayName.ShouldBe("Video Evidence");
        message.NewDisplayName.ShouldBe("Apologist");
    }

    [Test]
    public async Task ShouldNotPublishNamedChangedEventWhenNameDoesNotChange()
    {
        var user = await CreateUser(firstName: "Puffy", lastName: "Dog", displayName: "FNTSP");
        var request = new UpdateUserDetailsEndpoint.UpdateUserDetailsRequest(
            DisplayName: "FNTSP",
            FirstName: "Puff",
            LastName: "Doggy"
        );
        SetClaimsPrincipal(user);

        await HttpClient.PostAsync("/api/identity/details", JsonContent.Create(request));

        (await Messaging.Published.Any<UserDisplayNameChanged>()).ShouldBeFalse();
    }
}
