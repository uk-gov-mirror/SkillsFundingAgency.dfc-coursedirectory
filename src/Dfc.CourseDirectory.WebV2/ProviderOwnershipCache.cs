﻿using System;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Dfc.CourseDirectory.Core.DataStore.Sql;
using Dfc.CourseDirectory.Core.DataStore.Sql.Queries;
using Microsoft.Extensions.Caching.Memory;

namespace Dfc.CourseDirectory.WebV2
{
    public class ProviderOwnershipCache : IProviderOwnershipCache
    {
        private readonly ICosmosDbQueryDispatcher _cosmosDbQueryDispatcher;
        private readonly ISqlQueryDispatcher _sqlQueryDispatcher;
        private readonly IMemoryCache _cache;

        public ProviderOwnershipCache(
            ICosmosDbQueryDispatcher cosmosDbQueryDispatcher,
            ISqlQueryDispatcher sqlQueryDispatcher,
            IMemoryCache cache)
        {
            _cosmosDbQueryDispatcher = cosmosDbQueryDispatcher;
            _sqlQueryDispatcher = sqlQueryDispatcher;
            _cache = cache;
        }

        public async Task<Guid?> GetProviderForCourse(Guid courseId)
        {
            var cacheKey = GetCourseCacheKey(courseId);

            if (!_cache.TryGetValue<Guid?>(cacheKey, out var providerId))
            {
                var ukprn = await _cosmosDbQueryDispatcher.ExecuteQuery(
                    new GetProviderUkprnForCourse() { CourseId = courseId });

                if (ukprn.HasValue)
                {
                    providerId = await GetProviderIdByUkprn(ukprn);
                    _cache.Set(cacheKey, providerId);
                }
                else
                {
                    providerId = null;
                }
            }

            return providerId;
        }

        public async Task<Guid?> GetProviderForApprenticeship(Guid apprenticeshipId)
        {
            var cacheKey = GetApprenticeshipCacheKey(apprenticeshipId);

            if (!_cache.TryGetValue<Guid?>(cacheKey, out var providerId))
            {
                var ukprn = await _cosmosDbQueryDispatcher.ExecuteQuery(
                    new GetProviderUkprnForApprenticeship() { ApprenticeshipId = apprenticeshipId });

                if (ukprn.HasValue)
                {
                    providerId = await GetProviderIdByUkprn(ukprn);
                    _cache.Set(cacheKey, providerId);
                }
                else
                {
                    providerId = null;
                }
            }

            return providerId;
        }

        public async Task<Guid?> GetProviderForTLevel(Guid tLevelId)
        {
            var cacheKey = GetTLevelCacheKey(tLevelId);

            if (!_cache.TryGetValue<Guid?>(cacheKey, out var providerId))
            {
                var tLevel = await _sqlQueryDispatcher.ExecuteQuery(
                    new GetTLevel() { TLevelId = tLevelId });

                if (tLevel != null)
                {
                    providerId = tLevel.ProviderId;
                    _cache.Set(cacheKey, providerId);
                }
                else
                {
                    providerId = null;
                }
            }

            return providerId;
        }

        public async Task<Guid?> GetProviderForVenue(Guid venueId)
        {
            var cacheKey = GetVenueCacheKey(venueId);

            if (!_cache.TryGetValue<Guid?>(cacheKey, out var providerId))
            {
                var venue = await _cosmosDbQueryDispatcher.ExecuteQuery(
                    new GetVenueById() { VenueId = venueId });

                if (venue != null)
                {
                    providerId = await GetProviderIdByUkprn(venue.Ukprn);
                    _cache.Set(cacheKey, providerId);
                }
                else
                {
                    providerId = null;
                }
            }

            return providerId;
        }

        public void OnApprenticeshipDeleted(Guid apprenticeshipId)
        {
            var cacheKey = GetApprenticeshipCacheKey(apprenticeshipId);
            _cache.Remove(cacheKey);
        }

        public void OnCourseDeleted(Guid courseId)
        {
            var cacheKey = GetCourseCacheKey(courseId);
            _cache.Remove(cacheKey);
        }

        public void OnTLevelDeleted(Guid tLevelId)
        {
            var cacheKey = GetTLevelCacheKey(tLevelId);
            _cache.Remove(cacheKey);
        }

        public void OnVenueDeleted(Guid venueId)
        {
            var cacheKey = GetVenueCacheKey(venueId);
            _cache.Remove(cacheKey);
        }

        private static string GetApprenticeshipCacheKey(Guid apprenticeshipId) =>
            $"apprenticeship-providers:{apprenticeshipId}";

        private static string GetCourseCacheKey(Guid courseId) =>
            $"course-providers:{courseId}";

        private static string GetTLevelCacheKey(Guid tLevelId) =>
            $"tlevel-providers:{tLevelId}";

        private static string GetVenueCacheKey(Guid venueId) =>
            $"venue-providers:{venueId}";

        private async Task<Guid> GetProviderIdByUkprn(int? ukprn) =>
            (await _cosmosDbQueryDispatcher.ExecuteQuery(new GetProviderByUkprn() { Ukprn = ukprn.Value })).Id;
    }
}
