﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Core.Models;
using Dfc.CourseDirectory.Core.Search;
using Dfc.CourseDirectory.Core.Search.Models;
using Dfc.CourseDirectory.Core.Validation;
using Dfc.CourseDirectory.Core.Validation.VenueValidation;
using FluentValidation;
using FluentValidation.Results;
using FormFlow;
using MediatR;
using OneOf;
using OneOf.Types;

namespace Dfc.CourseDirectory.WebV2.Features.Venues.AddVenue.Address
{
    public class Query : IRequest<Command>
    {
    }

    public class Command : IRequest<OneOf<ModelWithErrors<Command>, Success>>
    {
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Postcode { get; set; }
    }

    public class Handler :
        IRequestHandler<Query, Command>,
        IRequestHandler<Command, OneOf<ModelWithErrors<Command>, Success>>
    {
        private readonly JourneyInstanceProvider _journeyInstanceProvider;
        private readonly ISearchClient<Onspd> _onspdSearchClient;

        public Handler(JourneyInstanceProvider journeyInstanceProvider, ISearchClient<Onspd> onspdSearchClient)
        {
            _journeyInstanceProvider = journeyInstanceProvider;
            _onspdSearchClient = onspdSearchClient;
        }

        public Task<Command> Handle(Query request, CancellationToken cancellationToken)
        {
            var journeyInstance = _journeyInstanceProvider.GetInstance<AddVenueJourneyModel>();
            journeyInstance.ThrowIfCompleted();

            return Task.FromResult(new Command()
            {
                AddressLine1 = journeyInstance.State.AddressLine1,
                AddressLine2 = journeyInstance.State.AddressLine2,
                Town = journeyInstance.State.Town,
                County = journeyInstance.State.County,
                Postcode = journeyInstance.State.Postcode
            });
        }

        public async Task<OneOf<ModelWithErrors<Command>, Success>> Handle(Command request, CancellationToken cancellationToken)
        {
            var journeyInstance = _journeyInstanceProvider.GetInstance<AddVenueJourneyModel>();
            journeyInstance.ThrowIfCompleted();

            // Normalize the postcode; validation accepts postcodes with no spaces but ONSPD lookup requires spaces.
            // Also ensures we have postcodes consistently capitalized.
            if (Postcode.TryParse(request.Postcode, out var postcode))
            {
                request.Postcode = postcode;
            }

            var validator = new CommandValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                return new ModelWithErrors<Command>(request, validationResult);
            }

            var onspdSearchResult = await _onspdSearchClient.Search(new OnspdSearchQuery() { Postcode = request.Postcode });
            var onspdPostcodeRecord = onspdSearchResult.Items.SingleOrDefault();

            if (onspdPostcodeRecord == null)
            {
                validationResult = new ValidationResult(new[]
                {
                    new ValidationFailure(nameof(request.Postcode), "Enter a valid postcode")
                });

                return new ModelWithErrors<Command>(request, validationResult);
            }

            journeyInstance.UpdateState(state =>
            {
                state.AddressLine1 = request.AddressLine1;
                state.AddressLine2 = request.AddressLine2;
                state.Town = request.Town;
                state.County = request.County;
                state.Postcode = request.Postcode;
                state.Latitude = onspdPostcodeRecord.Record.lat;
                state.Longitude = onspdPostcodeRecord.Record.@long;
                state.AddressIsOutsideOfEngland = !onspdPostcodeRecord.Record.IsInEngland;
                state.ValidStages |= AddVenueCompletedStages.Address;
            });

            return new Success();
        }

        private class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(c => c.AddressLine1).AddressLine1();
                RuleFor(c => c.AddressLine2).AddressLine2();
                RuleFor(c => c.Town).Town();
                RuleFor(c => c.County).County();
                RuleFor(c => c.Postcode).Postcode();
            }
        }
    }
}
