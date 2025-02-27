﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Testing;
using Dfc.CourseDirectory.WebV2.Features.Venues.EditVenue;
using FluentAssertions;
using FormFlow;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Dfc.CourseDirectory.WebV2.Tests.FeatureTests.Venues.EditVenue
{
    public class WebsiteTests : MvcTestBase
    {
        public WebsiteTests(CourseDirectoryApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task Get_ValidRequest_RendersExpectedOutput()
        {
            // Arrange
            var provider = await TestData.CreateProvider();
            var venueId = (await TestData.CreateVenue(provider.ProviderId, website: "provider.com")).Id;

            var request = new HttpRequestMessage(HttpMethod.Get, $"venues/{venueId}/website");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var doc = await response.GetDocument();
            doc.GetElementById("Website").GetAttribute("value").Should().Be("provider.com");
        }

        [Theory]
        [InlineData("new-provider.com")]
        [InlineData("")]
        public async Task Get_NewWebsiteAlreadySetInJourneyInstance_RendersExpectedOutput(string existingValue)
        {
            // Arrange
            var provider = await TestData.CreateProvider();
            var venueId = (await TestData.CreateVenue(provider.ProviderId, website: "provider.com")).Id;

            var journeyInstance = await CreateJourneyInstance(venueId);
            journeyInstance.UpdateState(state => state.Website = existingValue);

            var request = new HttpRequestMessage(HttpMethod.Get, $"venues/{venueId}/website");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var doc = await response.GetDocument();
            doc.GetElementById("Website").GetAttribute("value").Should().Be(existingValue);
        }

        [Theory]
        [InlineData(TestUserType.ProviderSuperUser)]
        [InlineData(TestUserType.ProviderUser)]
        public async Task Post_UserCannotAccessVenue_ReturnsForbidden(TestUserType userType)
        {
            // Arrange
            var provider = await TestData.CreateProvider();
            var venueId = (await TestData.CreateVenue(provider.ProviderId)).Id;

            var anotherProvider = await TestData.CreateProvider();

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add("Website", "new-provider.com")
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"venues/{venueId}/website")
            {
                Content = requestContent
            };

            await User.AsTestUser(userType, anotherProvider.ProviderId);

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Post_VenueDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var venueId = Guid.NewGuid();

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add("Website", "new-provider.com")
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"venues/{venueId}/website")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Post_InvalidWebsite_RendersError()
        {
            // Arrange
            var provider = await TestData.CreateProvider();
            var venueId = (await TestData.CreateVenue(provider.ProviderId)).Id;

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add("Website", ":bad/website")
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"venues/{venueId}/website")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var doc = await response.GetDocument();
            doc.AssertHasError("Website", "Enter a website in the correct format");
        }

        [Theory]
        [InlineData("new-provider.com")]
        [InlineData("http://new-provider.com")]
        [InlineData("https://new-provider.com")]
        [InlineData("")]
        public async Task Post_ValidRequest_UpdatesJourneyInstanceAndRedirects(string website)
        {
            // Arrange
            var provider = await TestData.CreateProvider();
            var venueId = (await TestData.CreateVenue(provider.ProviderId)).Id;

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add("Website", website)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"venues/{venueId}/website")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Found);
            response.Headers.Location.OriginalString.Should().Be($"/venues/{venueId}");

            var journeyInstance = GetJourneyInstance(venueId);
            journeyInstance.State.Website.Should().Be(website);
        }

        private async Task<JourneyInstance<EditVenueJourneyModel>> CreateJourneyInstance(Guid venueId)
        {
            var state = await Factory.Services.GetRequiredService<EditVenueJourneyModelFactory>()
                .CreateModel(venueId);

            return CreateJourneyInstance(
                journeyName: "EditVenue",
                configureKeys: keysBuilder => keysBuilder.With("venueId", venueId),
                state);
        }

        private JourneyInstance<EditVenueJourneyModel> GetJourneyInstance(Guid venueId) =>
            GetJourneyInstance<EditVenueJourneyModel>(
                journeyName: "EditVenue",
                configureKeys: keysBuilder => keysBuilder.With("venueId", venueId));
    }
}
