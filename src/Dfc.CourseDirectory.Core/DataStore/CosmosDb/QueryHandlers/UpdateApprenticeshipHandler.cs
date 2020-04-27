﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Models;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb.Queries;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using OneOf.Types;

namespace Dfc.CourseDirectory.Core.DataStore.CosmosDb.QueryHandlers
{
    public class UpdateApprenticeshipHandler : ICosmosDbQueryHandler<UpdateApprenticeship, Success>
    {
        public async Task<Success> Execute(DocumentClient client,
            Configuration configuration, UpdateApprenticeship request)
        {
            var documentUri = UriFactory.CreateDocumentUri(
                configuration.DatabaseId,
                configuration.ApprenticeshipCollectionName,
                request.Id.ToString());

            var partitionKey = new PartitionKey(request.ProviderUkprn);

            var query = await client.ReadDocumentAsync<Apprenticeship>(
                documentUri,
                new RequestOptions() { PartitionKey = partitionKey });

            var apprenticeship = query.Document;

            apprenticeship.ApprenticeshipTitle = request.ApprenticeshipTitle;
            apprenticeship.ApprenticeshipType = request.ApprenticeshipType;
            apprenticeship.MarketingInformation = request.MarketingInformation;
            apprenticeship.Url = request.Url;
            apprenticeship.ContactTelephone = request.ContactTelephone;
            apprenticeship.ContactEmail = request.ContactEmail;
            apprenticeship.ContactWebsite = request.ContactWebsite;
            apprenticeship.ApprenticeshipLocations = request.ApprenticeshipLocations.Select(l => new ApprenticeshipLocation()
            {
                Address = l.Address,
                ApprenticeshipLocationType = l.ApprenticeshipLocationType,
                CreatedBy = request.UpdatedBy.Email,
                CreatedDate = request.UpdatedDate,
                DeliveryModes = EnumHelper.SplitFlags(l.ApprenticeshipLocationType).Cast<int>().ToList(),
                Id = Guid.NewGuid(),
                LocationType = l.LocationType,
                Name = l.Name,
                National = l.National,
                Phone = l.Phone,
                ProviderUKPRN = apprenticeship.ProviderUKPRN,
                Radius = l.Radius,
                RecordStatus = 1,
                Regions = l.Regions,
                UpdatedBy = request.UpdatedBy.Email,
                UpdatedDate = request.UpdatedDate,
                VenueId = l.VenueId
            }).ToList();
            apprenticeship.UpdatedDate = request.UpdatedDate;
            apprenticeship.UpdatedBy = request.UpdatedBy.Email;

            request.StandardOrFramework.Switch(
                standard =>
                {
                    apprenticeship.StandardId = standard.CosmosId;
                    apprenticeship.StandardCode = standard.StandardCode;
                    apprenticeship.Version = standard.Version;
                    apprenticeship.NotionalNVQLevelv2 = standard.NotionalNVQLevelv2;
                },
                framework =>
                {
                    apprenticeship.FrameworkId = framework.CosmosId;
                    apprenticeship.FrameworkCode = framework.FrameworkCode;
                    apprenticeship.ProgType = framework.ProgType;
                    apprenticeship.PathwayCode = framework.PathwayCode;
                });

            await client.ReplaceDocumentAsync(
                documentUri,
                apprenticeship,
                new RequestOptions() { PartitionKey = partitionKey });

            return new Success();
        }
    }
}
