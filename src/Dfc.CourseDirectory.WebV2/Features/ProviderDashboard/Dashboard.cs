﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core;
using Dfc.CourseDirectory.Core.BinaryStorageProvider;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb;
using Dfc.CourseDirectory.Core.DataStore.Sql;
using Dfc.CourseDirectory.Core.DataStore.Sql.Queries;
using Dfc.CourseDirectory.Core.Models;
using MediatR;
using CosmosDbQueries = Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;

namespace Dfc.CourseDirectory.WebV2.Features.ProviderDashboard.Dashboard
{
    public class Query : IRequest<ViewModel>
    {
        public Guid ProviderId { get; set; }
    }

    public class ViewModel
    {
        public string ProviderName { get; set; }
        public int Ukprn { get; set; }
        public bool ShowCourses { get; set; }
        public bool ShowApprenticeships { get; set; }
        public int LiveCourseRunCount { get; set; }
        public int MigrationPendingCourseRunCount { get; set; }
        public int LarslessCourseCount { get; set; }
        public int ApprenticeshipCount { get; set; }
        public int VenueCount { get; set; }
        public int BulkUploadFileCount { get; set; }
    }

    public class Handler : IRequestHandler<Query, ViewModel>
    {
        private readonly ISqlQueryDispatcher _sqlQueryDispatcher;
        private readonly ICosmosDbQueryDispatcher _cosmosDbQueryDispatcher;
        private readonly IBinaryStorageProvider _binaryStorageProvider;

        public Handler(ISqlQueryDispatcher sqlQueryDispatcher, ICosmosDbQueryDispatcher cosmosDbQueryDispatcher, IBinaryStorageProvider binaryStorageProvider)
        {
            _sqlQueryDispatcher = sqlQueryDispatcher;
            _cosmosDbQueryDispatcher = cosmosDbQueryDispatcher;
            _binaryStorageProvider = binaryStorageProvider;
        }

        public async Task<ViewModel> Handle(Query request, CancellationToken cancellationToken)
        {
            var provider = await _sqlQueryDispatcher.ExecuteQuery(
                new GetProviderById() { ProviderId = request.ProviderId });

            if (provider == null)
            {
                throw new ResourceDoesNotExistException(ResourceType.Provider, request.ProviderId);
            }

            var (courseRunCounts, apprenticeshipCount, venueCount) = await _sqlQueryDispatcher.ExecuteQuery(
                new GetProviderDashboardCounts() { ProviderId = request.ProviderId });

            var courseMigrationReport = await _cosmosDbQueryDispatcher.ExecuteQuery(new CosmosDbQueries.GetCourseMigrationReportForProvider { ProviderUkprn = provider.Ukprn });

            var bulkUploadFiles = await _binaryStorageProvider.ListFiles($"{provider.Ukprn}/Bulk Upload/Files/");

            var vm = new ViewModel()
            {
                ProviderName = provider.ProviderName,
                Ukprn = provider.Ukprn,
                ShowCourses = provider.ProviderType.HasFlag(ProviderType.FE),
                ShowApprenticeships = provider.ProviderType.HasFlag(ProviderType.Apprenticeships) && provider.ApprenticeshipQAStatus == ApprenticeshipQAStatus.Passed,
                LiveCourseRunCount = courseRunCounts.GetValueOrDefault(CourseStatus.Live),
                MigrationPendingCourseRunCount = courseRunCounts.GetValueOrDefault(CourseStatus.MigrationPending) + courseRunCounts.GetValueOrDefault(CourseStatus.MigrationReadyToGoLive),
                LarslessCourseCount = courseMigrationReport?.LarslessCourses?.Count ?? 0,
                ApprenticeshipCount = apprenticeshipCount,
                VenueCount = venueCount,
                BulkUploadFileCount = bulkUploadFiles.Count()
            };

            return vm;
        }
    }
}
