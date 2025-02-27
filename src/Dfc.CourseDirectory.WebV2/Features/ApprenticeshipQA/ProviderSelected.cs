﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.Sql;
using Dfc.CourseDirectory.Core.DataStore.Sql.Queries;
using Dfc.CourseDirectory.Core.Models;
using MediatR;

namespace Dfc.CourseDirectory.WebV2.Features.ApprenticeshipQA.ProviderSelected
{
    public struct NoSubmission
    {
    }

    public class Query : IRequest<ViewModel>
    {
        public Guid ProviderId { get; set; }
    }

    public class ViewModel
    {
        public Guid ProviderId { get; set; }
        public string ProviderName { get; set; }
        public ApprenticeshipQAStatus ApprenticeshipQAStatus { get; set; }
        public bool HaveSubmission { get; set; }
        public bool ProviderAssessmentCompleted { get; set; }
        public IReadOnlyCollection<ViewModelApprenticeshipSubmission> ApprenticeshipAssessments { get; set; }
        public bool CanComplete { get; set; }
        public bool? SubmissionPassed { get; set; }
    }

    public class ViewModelApprenticeshipSubmission
    {
        public Guid ApprenticeshipId { get; set; }
        public string ApprenticeshipTitle { get; set; }
        public bool AssessmentCompleted { get; set; }
    }

    public class QueryHandler : IRequestHandler<Query, ViewModel>
    {
        private readonly ISqlQueryDispatcher _sqlQueryDispatcher;

        public QueryHandler(ISqlQueryDispatcher sqlQueryDispatcher)
        {
            _sqlQueryDispatcher = sqlQueryDispatcher;
        }

        public async Task<ViewModel> Handle(Query request, CancellationToken cancellationToken)
        {
            var provider = await _sqlQueryDispatcher.ExecuteQuery(
                new GetProviderById()
                {
                    ProviderId = request.ProviderId
                });

            if (provider == null)
            {
                throw new InvalidStateException(InvalidStateReason.ProviderDoesNotExist);
            }

            var qaStatus = await _sqlQueryDispatcher.ExecuteQuery(
                new GetProviderApprenticeshipQAStatus()
                {
                    ProviderId = request.ProviderId
                });

            var latestSubmission = await _sqlQueryDispatcher.ExecuteQuery(
                new GetLatestApprenticeshipQASubmissionForProvider()
                {
                    ProviderId = request.ProviderId
                });

            var canComplete = qaStatus == ApprenticeshipQAStatus.InProgress && latestSubmission?.Passed != null;

            return new ViewModel()
            {
                ApprenticeshipAssessments = latestSubmission?.Apprenticeships
                    .Select(a => new ViewModelApprenticeshipSubmission()
                    {
                        ApprenticeshipId = a.ApprenticeshipId,
                        ApprenticeshipTitle = a.ApprenticeshipTitle,
                        AssessmentCompleted = latestSubmission?.ApprenticeshipAssessmentsPassed != null
                    })
                    .ToList(),
                ApprenticeshipQAStatus = qaStatus.ValueOrDefault(),
                HaveSubmission = latestSubmission != null,
                CanComplete = canComplete,
                ProviderAssessmentCompleted = latestSubmission?.ProviderAssessmentPassed != null,
                ProviderId = request.ProviderId,
                ProviderName = provider.ProviderName,
                SubmissionPassed = latestSubmission?.Passed
            };
        }
    }
}
