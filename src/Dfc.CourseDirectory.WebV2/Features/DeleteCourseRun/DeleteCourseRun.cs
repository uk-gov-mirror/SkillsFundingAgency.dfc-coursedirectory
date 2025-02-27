﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Models;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Dfc.CourseDirectory.Core.Models;
using Dfc.CourseDirectory.Core.Validation;
using Dfc.CourseDirectory.WebV2.Security;
using FluentValidation.Results;
using FormFlow;
using MediatR;
using OneOf;
using DeleteCourseRunQuery = Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries.DeleteCourseRun;

namespace Dfc.CourseDirectory.WebV2.Features.DeleteCourseRun
{
    [JourneyState]
    public class JourneyModel
    {
        public string CourseName { get; set; }
        public Guid ProviderId { get; set; }
        public int ProviderUkprn { get; set; }
    }

    public class Request : IRequest<ViewModel>
    {
        public Guid CourseId { get; set; }
        public Guid CourseRunId { get; set; }
    }

    public class ViewModel : Command
    {
        public string CourseName { get; set; }
        public string YourReference { get; set; }
        public CourseDeliveryMode DeliveryMode { get; set; }
        public bool FlexibleStartDate { get; set; }
        public DateTime? StartDate { get; set; }
        public string VenueName { get; set; }
    }

    public class Command : IRequest<OneOf<ModelWithErrors<ViewModel>, SuccessViewModel>>
    {
        public Guid CourseId { get; set; }
        public Guid CourseRunId { get; set; }
        public bool Confirm { get; set; }
    }

    public class SuccessViewModel
    {
        public Guid CourseId { get; set; }
        public Guid CourseRunId { get; set; }
        public string CourseName { get; set; }
    }

    public class ConfirmedQuery : IRequest<ConfirmedViewModel>
    {
    }

    public class ConfirmedViewModel
    {
        public Guid ProviderId { get; set; }
        public string CourseName { get; set; }
        public bool HasOtherCourseRuns { get; set; }
    }

    public class Handler :
        IRequestHandler<Request, ViewModel>,
        IRequestHandler<Command, OneOf<ModelWithErrors<ViewModel>, SuccessViewModel>>,
        IRequestHandler<ConfirmedQuery, ConfirmedViewModel>
    {
        private readonly IProviderInfoCache _providerInfoCache;
        private readonly IProviderOwnershipCache _providerOwnershipCache;
        private readonly ICosmosDbQueryDispatcher _cosmosDbQueryDispatcher;
        private readonly JourneyInstance<JourneyModel> _journeyInstance;
        private readonly IClock _clock;
        private readonly ICurrentUserProvider _currentUserProvider;

        public Handler(
            IProviderInfoCache providerInfoCache,
            IProviderOwnershipCache providerOwnershipCache,
            ICosmosDbQueryDispatcher cosmosDbQueryDispatcher,
            JourneyInstance<JourneyModel> journeyInstance,
            IClock clock,
            ICurrentUserProvider currentUserProvider)
        {
            _providerInfoCache = providerInfoCache ?? throw new ArgumentNullException(nameof(providerInfoCache));
            _providerOwnershipCache = providerOwnershipCache ?? throw new ArgumentNullException(nameof(providerOwnershipCache));
            _cosmosDbQueryDispatcher = cosmosDbQueryDispatcher ?? throw new ArgumentNullException(nameof(cosmosDbQueryDispatcher));
            _journeyInstance = journeyInstance ?? throw new ArgumentNullException(nameof(journeyInstance));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _currentUserProvider = currentUserProvider ?? throw new ArgumentNullException(nameof(currentUserProvider));
        }

        public async Task<ViewModel> Handle(Request request, CancellationToken cancellationToken)
        {
            var ukprn = await GetUkprnForCourse(request.CourseId);
            var (course, courseRun) = await GetCourseAndCourseRun(request.CourseId, request.CourseRunId, ukprn);
            return await CreateViewModel(course, courseRun);
        }

        public async Task<OneOf<ModelWithErrors<ViewModel>, SuccessViewModel>> Handle(Command request, CancellationToken cancellationToken)
        {
            var ukprn = await GetUkprnForCourse(request.CourseId);
            var (course, courseRun) = await GetCourseAndCourseRun(request.CourseId, request.CourseRunId, ukprn);

            if (!request.Confirm)
            {
                var vm = await CreateViewModel(course, courseRun);
                var validationResult = new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(request.Confirm), "Confirm you want to delete the course")
                });
                return new ModelWithErrors<ViewModel>(vm, validationResult);
            }

            await _cosmosDbQueryDispatcher.ExecuteQuery(new DeleteCourseRunQuery()
            {
                CourseId = request.CourseId,
                CourseRunId = request.CourseRunId,
                ProviderUkprn = ukprn,
                UpdatedBy = _currentUserProvider.GetCurrentUser().UserId,
                UpdatedDate = _clock.UtcNow,
            });

            // The next page needs this info - stash it in the JourneyModel
            // since it will no longer able to query for it.
            _journeyInstance.UpdateState(new JourneyModel()
            {
                CourseName = courseRun.CourseName,
                ProviderId = (await _providerInfoCache.GetProviderIdForUkprn(ukprn)).Value,
                ProviderUkprn = ukprn
            });

            _journeyInstance.Complete();

            return new SuccessViewModel()
            {
                CourseId = request.CourseId,
                CourseRunId = request.CourseRunId,
                CourseName = courseRun.CourseName
            };
        }

        public async Task<ConfirmedViewModel> Handle(ConfirmedQuery request, CancellationToken cancellationToken)
        {
            var ukprn = _journeyInstance.State.ProviderUkprn;

            var liveCourses = await _cosmosDbQueryDispatcher.ExecuteQuery(new GetAllCoursesForProvider()
            {
                CourseStatuses = CourseStatus.Live,
                ProviderUkprn = ukprn
            });

            return new ConfirmedViewModel()
            {
                ProviderId = _journeyInstance.State.ProviderId,
                CourseName = _journeyInstance.State.CourseName,
                HasOtherCourseRuns = liveCourses.Any()
            };
        }

        private async Task<ViewModel> CreateViewModel(Course course, CourseRun courseRun)
        {
            Venue venue = default;
            if (courseRun.VenueId.HasValue)
            {
                venue = await _cosmosDbQueryDispatcher.ExecuteQuery(new GetVenueById() { VenueId = courseRun.VenueId.Value });
            }

            return new ViewModel()
            {
                CourseId = course.Id,
                CourseRunId = courseRun.Id,
                CourseName = courseRun.CourseName,
                DeliveryMode = courseRun.DeliveryMode,
                FlexibleStartDate = courseRun.FlexibleStartDate,
                StartDate = courseRun.StartDate,
                VenueName = venue?.VenueName,
                YourReference = courseRun.ProviderCourseID
            };
        }

        private async Task<(Course Course, CourseRun CourseRun)> GetCourseAndCourseRun(Guid courseId, Guid courseRunId, int ukprn)
        {
            (await _cosmosDbQueryDispatcher.ExecuteQuery(new GetCoursesByIds()
            {
                CourseIds = new[] { courseId },
                Ukprn = ukprn
            })).TryGetValue(courseId, out var course);

            if (course == null)
            {
                throw new ResourceDoesNotExistException(ResourceType.Course, courseId);
            }

            var courseRun = course.CourseRuns.SingleOrDefault(cr => cr.Id == courseRunId);

            if (courseRun == null)
            {
                throw new ResourceDoesNotExistException(ResourceType.CourseRun, courseRunId);
            }

            return (course, courseRun);
        }

        private async Task<int> GetUkprnForCourse(Guid courseId)
        {
            var providerId = await _providerOwnershipCache.GetProviderForCourse(courseId);

            if (!providerId.HasValue)
            {
                throw new ResourceDoesNotExistException(ResourceType.Course, courseId);
            }

            var providerInfo = await _providerInfoCache.GetProviderInfo(providerId.Value);
            return providerInfo.Ukprn;
        }
    }
}
