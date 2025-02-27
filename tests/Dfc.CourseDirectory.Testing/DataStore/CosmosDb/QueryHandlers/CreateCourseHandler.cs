﻿using System.Linq;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Models;
using Dfc.CourseDirectory.Testing.DataStore.CosmosDb.Queries;
using OneOf.Types;

namespace Dfc.CourseDirectory.Testing.DataStore.CosmosDb.QueryHandlers
{
    public class CreateCourseHandler : ICosmosDbQueryHandler<CreateCourse, Success>
    {
        public Success Execute(InMemoryDocumentStore inMemoryDocumentStore, CreateCourse request)
        {
            var course = new Course()
            {
                Id = request.CourseId,
                ProviderId = request.ProviderId,
                ProviderUKPRN = request.ProviderUkprn,
                QualificationCourseTitle = request.QualificationCourseTitle,
                LearnAimRef = request.LearnAimRef,
                NotionalNVQLevelv2 = request.NotionalNVQLevelv2,
                AwardOrgCode = request.AwardOrgCode,
                QualificationType = request.QualificationType,
                CourseDescription = request.CourseDescription,
                EntryRequirements = request.EntryRequirements,
                WhatYoullLearn = request.WhatYoullLearn,
                HowYoullLearn = request.HowYoullLearn,
                WhatYoullNeed = request.WhatYoullNeed,
                HowYoullBeAssessed = request.HowYoullBeAssessed,
                WhereNext = request.WhereNext,
                AdultEducationBudget = request.AdultEducationBudget,
                AdvancedLearnerLoan = request.AdvancedLearnerLoan,
                CourseRuns = request.CourseRuns.Select(cr => new CourseRun()
                {
                    Id = cr.CourseRunId,
                    VenueId = cr.VenueId,
                    CourseName = cr.CourseName,
                    DeliveryMode = cr.DeliveryMode,
                    FlexibleStartDate = cr.FlexibleStartDate,
                    StartDate = cr.StartDate,
                    CourseURL = cr.CourseUrl,
                    Cost = cr.Cost,
                    CostDescription = cr.CostDescription,
                    DurationUnit = cr.DurationUnit,
                    DurationValue = cr.DurationValue,
                    StudyMode = cr.StudyMode,
                    AttendancePattern = cr.AttendancePattern,
                    National = cr.National,
                    Regions = cr.Regions,
                    RecordStatus = request.CourseStatus,
                    CreatedDate = request.CreatedDate,
                    CreatedBy = request.CreatedByUser.UserId,
                    UpdatedDate = request.CreatedDate,
                    UpdatedBy = request.CreatedByUser.UserId,
                    ProviderCourseID = cr.ProviderCourseId
                }),
                CourseStatus = request.CourseStatus,
                CreatedBy = request.CreatedByUser.UserId,
                CreatedDate = request.CreatedDate,
                UpdatedBy = request.CreatedByUser.UserId,
                UpdatedDate = request.CreatedDate
            };

            inMemoryDocumentStore.Courses.Save(course);

            return new Success();
        }
    }
}
