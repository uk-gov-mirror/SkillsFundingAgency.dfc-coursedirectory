﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Dfc.CourseDirectory.Services.Models.Courses;

namespace Dfc.CourseDirectory.Web.Helpers
{
    public static class VenueHelper
    {
        public static async Task<Dictionary<Guid, string>> GetVenueNames(IEnumerable<Course> courses, ICosmosDbQueryDispatcher cosmosDbQueryDispatcher)
        {
            var venueNames = new Dictionary<Guid, string>();
            foreach (var course in courses)
            {
                foreach (var courseRun in course.CourseRuns)
                {
                    if (courseRun.VenueId != Guid.Empty && courseRun.VenueId != null)
                    {
                        if (!venueNames.ContainsKey((Guid)courseRun.VenueId))
                        {
                            var venue = await cosmosDbQueryDispatcher.ExecuteQuery(
                                new GetVenueById {VenueId = courseRun.VenueId.Value});

                            venueNames.Add(courseRun.VenueId.Value, venue.VenueName);
                        }
                    }
                }
            }
            return venueNames;
        }
    }
}
