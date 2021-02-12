﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Testing;
using Dfc.CourseDirectory.WebV2.Features.Venues.AddVenue;
using FluentAssertions;
using FluentAssertions.Execution;
using FormFlow;
using Xunit;

namespace Dfc.CourseDirectory.WebV2.Tests.FeatureTests.Venues.AddVenue
{
    public class DetailsTests : MvcTestBase
    {
        public DetailsTests(CourseDirectoryApplicationFactory factory)
            : base(factory)
        {
        }

        [Fact]
        public async Task Get_ValidRequest_ReturnsExpectedOutput()
        {
            // Arrange
            var providerId = await TestData.CreateProvider();

            var name = "My Venue";
            var email = "email@example.com";
            var phoneNumber = "020 7946 0000";
            var website = "example.com";

            var journeyInstance = CreateJourneyInstance(
                providerId,
                new AddVenueJourneyModel()
                {
                    Name = name,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    Website = website,
                    ValidStages = AddVenueCompletedStages.Address
                });

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/venues/add/details?providerId={providerId}&ffiid={journeyInstance.InstanceId.UniqueKey}");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            using (new AssertionScope())
            {
                var doc = await response.GetDocument();
                doc.GetElementById("Name").GetAttribute("value").Should().Be(name);
                doc.GetElementById("Email").GetAttribute("value").Should().Be(email);
                doc.GetElementById("PhoneNumber").GetAttribute("value").Should().Be(phoneNumber);
                doc.GetElementById("Website").GetAttribute("value").Should().Be(website);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidAddressData))]
        public async Task Post_InvalidDetails_RendersErrorMessage(
            string name,
            string email,
            string phoneNumber,
            string website,
            string expectedErrorInputId,
            string expectedErrorMessage)
        {
            // Arrange
            var providerId = await TestData.CreateProvider();

            // Create another venue for the provider for testing the unique venue name error case
            await TestData.CreateVenue(providerId, venueName: "Existing Venue");

            var journeyInstance = CreateJourneyInstance(
                providerId,
                new AddVenueJourneyModel()
                {
                    ValidStages = AddVenueCompletedStages.Address
                });

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/venues/add/details?providerId={providerId}&ffiid={journeyInstance.InstanceId.UniqueKey}")
            {
                Content = new FormUrlEncodedContentBuilder()
                    .Add("Name", name)
                    .Add("Email", email)
                    .Add("PhoneNumber", phoneNumber)
                    .Add("Website", website)
                    .ToContent()
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var doc = await response.GetDocument();
            doc.AssertHasError(expectedErrorInputId, expectedErrorMessage);
        }

        [Fact]
        public async Task Post_ValidRequest_UpdatesJourneyStateAndRedirects()
        {
            // Arrange
            var providerId = await TestData.CreateProvider();

            var name = "My Venue";
            var email = "email@example.com";
            var phoneNumber = "020 7946 0000";
            var website = "example.com";

            var journeyInstance = CreateJourneyInstance(
                providerId,
                new AddVenueJourneyModel()
                {
                    ValidStages = AddVenueCompletedStages.Address
                });

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"/venues/add/details?providerId={providerId}&ffiid={journeyInstance.InstanceId.UniqueKey}")
            {
                Content = new FormUrlEncodedContentBuilder()
                    .Add("Name", name)
                    .Add("Email", email)
                    .Add("PhoneNumber", phoneNumber)
                    .Add("Website", website)
                    .ToContent()
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Found);

            response.Headers.Location.OriginalString.Should()
                .Be($"/venues/add/check-publish?providerId={providerId}&ffiid={journeyInstance.InstanceId.UniqueKey}");

            using (new AssertionScope())
            {
                journeyInstance = GetJourneyInstance<AddVenueJourneyModel>(journeyInstance.InstanceId);
                journeyInstance.State.Name.Should().Be(name);
                journeyInstance.State.Email.Should().Be(email);
                journeyInstance.State.PhoneNumber.Should().Be(phoneNumber);
                journeyInstance.State.Website.Should().Be(website);
            }
        }

        public static IEnumerable<object[]> InvalidAddressData { get; } =
            new[]
            {
                // Name is not unique for provider
                (
                    Name: "Existing Venue",
                    Email: "",
                    PhoneNumber: "",
                    Website: "",
                    ExpectedErrorInputId: "Name",
                    ExpectedErrorMessage: "Location name must not already exist"
                ),
                // Invalid Email
                (
                    Name: "My Venue",
                    Email: "xxx",
                    PhoneNumber: "",
                    Website: "",
                    ExpectedErrorInputId: "Email",
                    ExpectedErrorMessage: "Enter an email address in the correct format"
                ),
                // Invalid PhoneNumber
                (
                    Name: "My Venue",
                    Email: "",
                    PhoneNumber: "xxx",
                    Website: "",
                    ExpectedErrorInputId: "PhoneNumber",
                    ExpectedErrorMessage: "Enter a telephone number in the correct format"
                ),
                // Invalid Website
                (
                    Name: "My Venue",
                    Email: "",
                    PhoneNumber: "",
                    Website: ":bad/website",
                    ExpectedErrorInputId: "Website",
                    ExpectedErrorMessage: "Enter a website in the correct format"
                )
            }
            .Select(t => new object[] { t.Name, t.Email, t.PhoneNumber, t.Website, t.ExpectedErrorInputId, t.ExpectedErrorMessage })
            .ToArray();

        private JourneyInstance<AddVenueJourneyModel> CreateJourneyInstance(Guid providerId, AddVenueJourneyModel state = null)
        {
            state ??= new AddVenueJourneyModel();

            var uniqueKey = Guid.NewGuid().ToString();

            return CreateJourneyInstance(
                journeyName: "AddVenue",
                configureKeys: keysBuilder => keysBuilder.With("providerId", providerId),
                state,
                uniqueKey: uniqueKey);
        }
    }
}
