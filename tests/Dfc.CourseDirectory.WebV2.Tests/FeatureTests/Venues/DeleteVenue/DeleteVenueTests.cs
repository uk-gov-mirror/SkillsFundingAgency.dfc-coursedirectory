﻿using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Dfc.CourseDirectory.Core.Models;
using Dfc.CourseDirectory.Testing;
using Dfc.CourseDirectory.WebV2.Features.Venues.DeleteVenue;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.AspNetCore.Http;
using Moq;
using OneOf;
using OneOf.Types;
using Xunit;

namespace Dfc.CourseDirectory.WebV2.Tests.FeatureTests.Venues.DeleteVenue
{
    public class DeleteVenueTests : MvcTestBase
    {
        public DeleteVenueTests(CourseDirectoryApplicationFactory factory)
            :base(factory)
        {
        }

        [Fact]
        public async Task DeleteVenue_Get_VenueDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{Guid.NewGuid()}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Theory]
        [InlineData(TestUserType.ProviderSuperUser)]
        [InlineData(TestUserType.ProviderUser)]
        public async Task DeleteVenue_Get_WithMismatchedProvider_ReturnsForbidden(TestUserType userType)
        {
            // Arrange
            var provider1 = await TestData.CreateProvider(providerType: ProviderType.FE);
            var provider2 = await TestData.CreateProvider(providerType: ProviderType.FE);
            var venue = await TestData.CreateVenue(provider1.ProviderId);

            await User.AsTestUser(userType, provider2.ProviderId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Fact]
        public async Task DeleteVenue_Get_WithExistingCourse_RendersExpectedOutputWithDeleteDisabled()
        {
            // Arrange
            var provider = await TestData.CreateProvider(providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var courseId = await TestData.CreateCourse(provider.ProviderId, User.ToUserInfo(), configureCourseRuns: builder =>
                builder.WithCourseRun(CourseDeliveryMode.ClassroomBased, CourseStudyMode.FullTime, CourseAttendancePattern.Daytime, venueId: venue.Id));

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-course-{courseId}"));
        }

        [Fact]
        public async Task DeleteVenue_Get_WithExistingApprenticeship_RendersExpectedOutputWithDeleteDisabled()
        {
            // Arrange
            var provider = await TestData.CreateProvider(providerType: ProviderType.Apprenticeships);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");
            var apprenticeship = await TestData.CreateApprenticeship(
                provider.ProviderId,
                standard,
                User.ToUserInfo(),
                ApprenticeshipStatus.Live,
                locations: new[] { CreateApprenticeshipLocation.CreateFromVenue(venue, 30, new[] { ApprenticeshipDeliveryMode.DayRelease }) });

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-apprenticeship-{apprenticeship.Id}"));
        }

        [Fact]
        public async Task DeleteVenue_Get_WithExistingTLevel_RendersExpectedOutputWithDeleteDisabled()
        {
            // Arrange
            var tLevelDefinitions = await TestData.CreateInitialTLevelDefinitions();

            var provider = await TestData.CreateProvider(
                providerType: ProviderType.TLevels,
                tLevelDefinitionIds: tLevelDefinitions.Select(d => d.TLevelDefinitionId).ToArray());

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var tLevel = await TestData.CreateTLevel(provider.ProviderId, tLevelDefinitions.OrderBy(_ => Guid.NewGuid()).First().TLevelDefinitionId, new[] { venue.Id }, User.ToUserInfo(), Clock.UtcNow.Date);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-tlevel-{tLevel.TLevelId}"));
        }

        [Fact]
        public async Task DeleteVenue_Get_WithNoAffectedCoursesApprenticeshipsOrTLevels_RendersExpectedOutputWithDeleteEnabled()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);

            var doc = await response.GetDocument();

            Assert.NotNull(doc.GetElementByTestId("delete-location-button"));
        }

        [Fact]
        public async Task DeleteVenue_Post_VenueDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{Guid.NewGuid()}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Theory]
        [InlineData(TestUserType.ProviderSuperUser)]
        [InlineData(TestUserType.ProviderUser)]
        public async Task DeleteVenue_Post_WithMismatchedProvider_ReturnsForbidden(TestUserType userType)
        {
            // Arrange
            var provider1 = await TestData.CreateProvider(providerType: ProviderType.FE);
            var provider2 = await TestData.CreateProvider(providerType: ProviderType.FE);
            var venue = await TestData.CreateVenue(provider1.ProviderId);

            await User.AsTestUser(userType, provider2.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider1)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(false)]
        public async Task DeleteVenue_Post_WithConfirmNotTrue_ReturnsExpectedOutputWithError(bool? confirm)
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), confirm)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var doc = await response.GetDocument();

            doc.GetElementByTestId("confirm-error-message").TextContent.Should().Be("Confirm you want to delete the location");
        }

        [Fact]
        public async Task DeleteVenue_Post_WithProviderContextProviderIdNotEqualToPostProviderId_ReturnsBadRequest()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), Guid.NewGuid())
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        }

        [Fact]
        public async Task DeleteVenue_Post_WithExistingCourse_RendersExpectedOutputWithErrorMessageAndDeleteDisabled()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var courseId = await TestData.CreateCourse(provider.ProviderId, User.ToUserInfo(), configureCourseRuns: builder =>
                builder.WithCourseRun(CourseDeliveryMode.ClassroomBased, CourseStudyMode.FullTime, CourseAttendancePattern.Daytime, venueId: venue.Id));

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-course-{courseId}"));
            doc.GetElementByTestId("affected-courses-error-message").TextContent.Should().Be("The affected courses have changed");
        }

        [Fact]
        public async Task DeleteVenue_Post_WithExistingApprenticeship_RendersExpectedOutputWithErrorMessageAndDeleteDisabled()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.Apprenticeships);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");
            var apprenticeship = await TestData.CreateApprenticeship(
                provider.ProviderId,
                standard,
                User.ToUserInfo(),
                ApprenticeshipStatus.Live,
                locations: new[] { CreateApprenticeshipLocation.CreateFromVenue(venue, 30, new[] { ApprenticeshipDeliveryMode.DayRelease }) });

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-apprenticeship-{apprenticeship.Id}"));
            doc.GetElementByTestId("affected-apprenticeships-error-message").TextContent.Should().Be("The affected apprenticeships have changed");
        }

        [Fact]
        public async Task DeleteVenue_Post_WithExistingTLevel_RendersExpectedOutputWithErrorMessageAndDeleteDisabled()
        {
            // Arrange
            var tLevelDefinitions = await TestData.CreateInitialTLevelDefinitions();

            var provider = await TestData.CreateProvider(
                providerType: ProviderType.TLevels,
                tLevelDefinitionIds: tLevelDefinitions.Select(d => d.TLevelDefinitionId).ToArray());

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var tLevel = await TestData.CreateTLevel(provider.ProviderId, tLevelDefinitions.OrderBy(_ => Guid.NewGuid()).First().TLevelDefinitionId, new[] { venue.Id }, User.ToUserInfo(), Clock.UtcNow.Date);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);

            var doc = await response.GetDocument();

            Assert.Null(doc.GetElementByTestId("delete-location-button"));
            Assert.NotNull(doc.GetElementByTestId($"affected-tlevel-{tLevel.TLevelId}"));
            doc.GetElementByTestId("affected-tlevels-error-message").TextContent.Should().Be("The affected T Levels have changed");
        }

        [Fact]
        public async Task DeleteVenue_Post_DeletesVenueAndRedirects()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var request = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status302Found);
            response.Headers.Location.OriginalString.Should().Be($"/venues/{venue.Id}/delete-success?providerId={provider.ProviderId}");

            CosmosDbQueryDispatcher.VerifyExecuteQuery<Core.DataStore.CosmosDb.Queries.DeleteVenue, OneOf<NotFound, Success>>(q => q.VenueId == venue.Id, Times.Once());
        }

        [Fact]
        public async Task VenueDeleted_Get_WithNoFormFlowJourney_ReturnsNotFound()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete-success");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        }

        [Fact]
        public async Task VenueDeleted_Get_WithExistingFormFlowJourney_RendersExpectedOutput()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerType: ProviderType.FE);

            await User.AsTestUser(TestUserType.ProviderUser, provider.ProviderId);

            var venue = await TestData.CreateVenue(provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder()
                .Add(nameof(Command.Confirm), true)
                .Add(nameof(Command.ProviderId), provider.ProviderId)
                .ToContent();

            var deleteRequest = new HttpRequestMessage(HttpMethod.Post, $"/venues/{venue.Id}/delete")
            {
                Content = requestContent
            };

            await HttpClient.SendAsync(deleteRequest);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/venues/{venue.Id}/delete-success");

            // Act
            var response = await HttpClient.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(StatusCodes.Status200OK);

            var doc = await response.GetDocument();

            var locationDeletedMessage = doc.GetElementByTestId("venue-deleted-message").TextContent;
            locationDeletedMessage.Should().NotBeNullOrEmpty();

            using (new AssertionScope())
            {
                locationDeletedMessage.Should().Contain($"Location deleted");
                locationDeletedMessage.Should().Contain(venue.VenueName);
            }
        }
    }
}
