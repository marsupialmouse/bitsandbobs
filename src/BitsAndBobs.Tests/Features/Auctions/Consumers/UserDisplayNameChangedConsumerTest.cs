using BitsAndBobs.Contracts;
using BitsAndBobs.Features.Auctions.Consumers;
using BitsAndBobs.Features.Identity;
using BitsAndBobs.Infrastructure.DynamoDb;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.Core;
using Shouldly;

namespace BitsAndBobs.Tests.Features.Auctions.Consumers;

public class UserDisplayNameChangedConsumerTest : AuctionTestBase
{
    [Test]
    public async Task ShouldFailWhenCurrentUserDisplayNameDoesNotMatchNewDisplayName()
    {
        ConfigureMessaging(m => m.AddConsumer<UserDisplayNameChangedConsumer>());
        var user = await CreateUser(u => u.DisplayName = "Diving Bell");
        user.DisplayName = "Fred";  // This is just to set SellerDisplayName on the auction
        var auction = await CreateAuction(user);

        await Messaging.Bus.Publish(
            new UserDisplayNameChanged(UserId: user.Id.Value, OldDisplayName: "Fred", NewDisplayName: "The Bends")
        );
        var wasConsumedWithError =
            await
                WaitForMessageToBeConsumed<UserDisplayNameChangedConsumer, UserDisplayNameChanged>(x =>
                    x.Exception is ArgumentException
                );

        wasConsumedWithError.ShouldBeTrue();
        var dbAuction = await GetAuctionFromDb(auction);
        dbAuction!.SellerDisplayName.ShouldBe("Fred");
    }

    [Test]
    public async Task ShouldUpdateSellerAuctionsWithNewDisplayNameAndVersion()
    {
        ConfigureMessaging(m => m.AddConsumer<UserDisplayNameChangedConsumer>());
        var seller = await CreateUser(u => u.DisplayName = "The Bottom");
        seller.DisplayName = "The Top";  // This is just to set SellerDisplayName on the auction
        var sellerAuction1 = await CreateAuction(seller);
        var sellerAuction2 = await CreateAuction(seller, endDate: DateTimeOffset.Now.AddMinutes(-10), configure: x => x.Complete());
        seller.DisplayName = "The Side";
        var sellerAuction3 = await CreateAuction(seller, configure: x => x.Cancel());

        await Messaging.Bus.Publish(
            new UserDisplayNameChanged(
                UserId: seller.Id.Value,
                OldDisplayName: "Underneath",
                NewDisplayName: "The Bottom"
            )
        );
        await WaitForMessageToBeConsumed<UserDisplayNameChangedConsumer, UserDisplayNameChanged>();

        var dbSellerAuction1 = await GetAuctionFromDb(sellerAuction1);
        var dbSellerAuction2 = await GetAuctionFromDb(sellerAuction2);
        var dbSellerAuction3 = await GetAuctionFromDb(sellerAuction3);
        dbSellerAuction1!.SellerDisplayName.ShouldBe("The Bottom");
        dbSellerAuction1.Version.ShouldNotBeNull(sellerAuction1.Version);
        dbSellerAuction2!.SellerDisplayName.ShouldBe("The Bottom");
        dbSellerAuction2.Version.ShouldNotBeNull(sellerAuction2.Version);
        dbSellerAuction3!.SellerDisplayName.ShouldBe("The Bottom");
        dbSellerAuction3.Version.ShouldNotBeNull(sellerAuction3.Version);
    }

    [Test]
    public async Task ShouldNotUpdateNonSellerAuctions()
    {
        ConfigureMessaging(m => m.AddConsumer<UserDisplayNameChangedConsumer>());
        var seller = await CreateUser(u => u.DisplayName = "Cold");
        seller.DisplayName = "Hot";  // This is just to set SellerDisplayName on the auction
        await CreateAuction(seller);
        var bidderAuction = await CreateAuction();
        await AddBidToAuction(bidderAuction, seller.Id, 1000m);
        var otherAuction = await CreateAuction();

        await Messaging.Bus.Publish(
            new UserDisplayNameChanged(UserId: seller.Id.Value, OldDisplayName: "Hot", NewDisplayName: "Cold")
        );
        await WaitForMessageToBeConsumed<UserDisplayNameChangedConsumer, UserDisplayNameChanged>();

        var dbBidderAuction = await GetAuctionFromDb(bidderAuction);
        var dbOtherAuction = await GetAuctionFromDb(otherAuction);
        dbBidderAuction!.SellerDisplayName.ShouldBe(bidderAuction.SellerDisplayName);
        dbBidderAuction.Version.ShouldBe(bidderAuction.Version);
        dbOtherAuction!.SellerDisplayName.ShouldBe(otherAuction.SellerDisplayName);
        dbOtherAuction.Version.ShouldBe(otherAuction.Version);
    }

    [Test]
    public async Task ShouldNotUpdateAuctionsIfUserUpdatedAfterRetrieval()
    {
        ConfigureMessaging(m => m.AddConsumer<UserDisplayNameChangedConsumer>());
        var seller = await CreateUser(u => u.DisplayName = "Lord");
        seller.DisplayName = "Edge";  // This is just to set SellerDisplayName on the auction
        var auction = await CreateAuction(seller);
        AppFactory.ConfiguringServices += s =>
        {
            // Release the user store with a stub that updates the version in the database after retrieving it
            s.Replace(
                ServiceDescriptor.Scoped<IUserStore<User>>(x =>
                    {
                        var userStore = x.GetRequiredService<UserStore>();
                        var fakeStore = Substitute.For<IUserStore<User>>();
                        fakeStore
                            .FindByIdAsync("", CancellationToken.None)
                            .ReturnsForAnyArgs(async c =>
                                {
                                    var id = c.Arg<string>();
                                    var token = c.Arg<CancellationToken>();
                                    var user = await userStore.FindByIdAsync(id, token);
                                    var badUser = await userStore.FindByIdAsync(id, token);
                                    badUser!.FirstName = "BAD";
                                    badUser.LastName = "MAN";
                                    await userStore.UpdateAsync(badUser, token);
                                    return user;
                                }
                            );
                        return fakeStore;
                    }
                )
            );
        };

        await Messaging.Bus.Publish(
            new UserDisplayNameChanged(UserId: seller.Id.Value, OldDisplayName: "Edge", NewDisplayName: "Lord")
        );
        var wasConsumedWithError =
            await WaitForMessageToBeConsumed<UserDisplayNameChangedConsumer, UserDisplayNameChanged>(x =>
                x.Exception is DynamoDbConcurrencyException
            );

        wasConsumedWithError.ShouldBeTrue();
        var dbAuction = await GetAuctionFromDb(auction);
        dbAuction!.SellerDisplayName.ShouldBe("Edge");
    }


}
