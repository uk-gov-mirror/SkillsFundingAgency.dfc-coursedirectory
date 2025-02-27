﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Dfc.CourseDirectory.Core.DataStore.Sql.Queries;
using Dfc.CourseDirectory.Core.Models;
using Dfc.CourseDirectory.Testing;
using Moq;
using OneOf;
using OneOf.Types;
using Xunit;

namespace Dfc.CourseDirectory.WebV2.Tests.FeatureTests.ApprenticeshipQA
{
    public class CompleteTests : MvcTestBase
    {
        public CompleteTests(CourseDirectoryApplicationFactory factory)
            : base(factory)
        {
        }

        [Theory]
        [InlineData(TestUserType.ProviderSuperUser)]
        [InlineData(TestUserType.ProviderUser)]
        public async Task Post_ProviderUserCannotAccess(TestUserType testUserType)
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerName: "Provider 1",
                apprenticeshipQAStatus: ApprenticeshipQAStatus.InProgress);

            var providerUser = await TestData.CreateUser(providerId: provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");

            var apprenticeshipId = (await TestData.CreateApprenticeship(provider.ProviderId, standard, createdBy: User.ToUserInfo())).Id;

            var submissionId = await TestData.CreateApprenticeshipQASubmission(
                provider.ProviderId,
                submittedOn: Clock.UtcNow,
                submittedByUserId: providerUser.UserId,
                providerMarketingInformation: "The overview",
                apprenticeshipIds: new[] { apprenticeshipId });

            await TestData.UpdateApprenticeshipQASubmission(
                submissionId,
                providerAssessmentPassed: true,
                apprenticeshipAssessmentsPassed: true,
                passed: true,
                lastAssessedByUserId: User.UserId,
                lastAssessedOn: Clock.UtcNow);

            await User.AsTestUser(testUserType, provider.ProviderId);

            var requestContent = new FormUrlEncodedContentBuilder().ToContent();

            // Act
            var response = await HttpClient.PostAsync($"apprenticeship-qa/{provider.ProviderId}/complete", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Post_ProviderDoesNotExistReturnsBadRequest()
        {
            // Arrange
            var providerId = Guid.NewGuid();

            await User.AsHelpdesk();

            var requestContent = new FormUrlEncodedContentBuilder().ToContent();

            // Act
            var response = await HttpClient.PostAsync($"apprenticeship-qa/{providerId}/complete", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(ApprenticeshipQAStatus.Failed)]
        [InlineData(ApprenticeshipQAStatus.NotStarted)]
        [InlineData(ApprenticeshipQAStatus.Passed)]
        [InlineData(ApprenticeshipQAStatus.Submitted)]
        [InlineData(ApprenticeshipQAStatus.UnableToComplete | ApprenticeshipQAStatus.NotStarted)]
        public async Task Post_StatusNotInProgressReturnsBadRequest(ApprenticeshipQAStatus status)
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerName: "Provider 1",
                apprenticeshipQAStatus: status);

            var providerUser = await TestData.CreateUser(providerId: provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");

            var apprenticeshipId = (await TestData.CreateApprenticeship(provider.ProviderId, standard, createdBy: User.ToUserInfo())).Id;

            if (status != ApprenticeshipQAStatus.NotStarted)
            {
                var submissionId = await TestData.CreateApprenticeshipQASubmission(
                    provider.ProviderId,
                    submittedOn: Clock.UtcNow,
                    submittedByUserId: providerUser.UserId,
                    providerMarketingInformation: "The overview",
                    apprenticeshipIds: new[] { apprenticeshipId });

                if (status != ApprenticeshipQAStatus.Submitted && !status.HasFlag(ApprenticeshipQAStatus.UnableToComplete))
                {
                    var passed = status == ApprenticeshipQAStatus.Failed ? false : true;

                    await TestData.UpdateApprenticeshipQASubmission(
                        submissionId,
                        providerAssessmentPassed: passed,
                        apprenticeshipAssessmentsPassed: passed,
                        passed: passed,
                        lastAssessedByUserId: User.UserId,
                        lastAssessedOn: Clock.UtcNow);
                }
            }

            await User.AsHelpdesk();

            var requestContent = new FormUrlEncodedContentBuilder().ToContent();

            // Act
            var response = await HttpClient.PostAsync($"apprenticeship-qa/{provider.ProviderId}/complete", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Post_SubmissionOutcomeNotKnownReturnsBadRequest()
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerName: "Provider 1",
                apprenticeshipQAStatus: ApprenticeshipQAStatus.InProgress);

            var providerUser = await TestData.CreateUser(providerId: provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");

            var apprenticeshipId = (await TestData.CreateApprenticeship(provider.ProviderId, standard, createdBy: User.ToUserInfo())).Id;

            await TestData.CreateApprenticeshipQASubmission(
                provider.ProviderId,
                submittedOn: Clock.UtcNow,
                submittedByUserId: providerUser.UserId,
                providerMarketingInformation: "The overview",
                apprenticeshipIds: new[] { apprenticeshipId });

            await User.AsHelpdesk();

            var requestContent = new FormUrlEncodedContentBuilder().ToContent();

            // Act
            var response = await HttpClient.PostAsync($"apprenticeship-qa/{provider.ProviderId}/complete", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData(true, ApprenticeshipQAStatus.Passed, true)]
        [InlineData(false, ApprenticeshipQAStatus.Failed, false)]
        public async Task Post_ValidSetsCorrectStatusAndRequestRendersExpectedOutput(
            bool passed,
            ApprenticeshipQAStatus expectedStatus,
            bool expectApprenticeshipToBeMadeLive)
        {
            // Arrange
            var provider = await TestData.CreateProvider(
                providerName: "Provider 1",
                apprenticeshipQAStatus: ApprenticeshipQAStatus.InProgress);

            var providerUser = await TestData.CreateUser(providerId: provider.ProviderId);

            var standard = await TestData.CreateStandard(standardCode: 1234, version: 1, standardName: "Test Standard");

            var apprenticeshipId = (await TestData.CreateApprenticeship(provider.ProviderId, standard, createdBy: User.ToUserInfo())).Id;

            var submissionId = await TestData.CreateApprenticeshipQASubmission(
                provider.ProviderId,
                submittedOn: Clock.UtcNow,
                submittedByUserId: providerUser.UserId,
                providerMarketingInformation: "The overview",
                apprenticeshipIds: new[] { apprenticeshipId });

            await TestData.UpdateApprenticeshipQASubmission(
                submissionId,
                providerAssessmentPassed: passed,
                apprenticeshipAssessmentsPassed: passed,
                passed: passed,
                lastAssessedByUserId: User.UserId,
                lastAssessedOn: Clock.UtcNow);

            await User.AsHelpdesk();

            var requestContent = new FormUrlEncodedContentBuilder().ToContent();

            // Act
            var response = await HttpClient.PostAsync($"apprenticeship-qa/{provider.ProviderId}/complete", requestContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var doc = await response.GetDocument();
            var providerName = doc.GetElementById("pttcd-apprenticeship-qa-complete-provider-name").TextContent.Trim();
            Assert.Equal("Provider 1", providerName);

            var newStatus = await WithSqlQueryDispatcher(dispatcher => dispatcher.ExecuteQuery(new GetProviderApprenticeshipQAStatus()
            {
                ProviderId = provider.ProviderId
            }));
            Assert.Equal(expectedStatus, newStatus);

            CosmosDbQueryDispatcher.VerifyExecuteQuery<UpdateApprenticeshipStatus, OneOf<NotFound, Success>>(
                q => q.ApprenticeshipId == apprenticeshipId && q.ProviderUkprn == provider.Ukprn && q.Status == 1,
                expectApprenticeshipToBeMadeLive ? Times.Once() : Times.Never());
        }
    }
}
