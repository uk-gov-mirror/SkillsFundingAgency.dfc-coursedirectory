﻿using System.Threading.Tasks;
using Dfc.CourseDirectory.WebV2.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using OneOf.Types;

namespace Dfc.CourseDirectory.WebV2.Features.Providers
{
    [Route("providers")]
    [AssignLegacyProviderContext]
    public class ProvidersController : Controller
    {
        private readonly IMediator _mediator;

        public ProvidersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("display-name")]
        public async Task<IActionResult> DisplayName(ProviderContext providerContext)
        {
            var query = new DisplayName.Query() { ProviderId = providerContext.ProviderInfo.ProviderId };
            return await _mediator.SendAndMapResponse(query, vm => View(vm));
        }

        [HttpPost("display-name")]
        public async Task<IActionResult> DisplayName(DisplayName.Command command, ProviderContext providerContext)
        {
            command.ProviderId = providerContext.ProviderInfo.ProviderId;
            return await _mediator.SendAndMapResponse(
                command,
                success => RedirectToAction(nameof(ProviderDetails)).WithProviderContext(providerContext));
        }

        [HttpGet("info")]
        public async Task<IActionResult> EditProviderInfo(ProviderContext providerContext)
        {
            var query = new EditProviderInfo.Query() { ProviderId = providerContext.ProviderInfo.ProviderId };
            return await _mediator.SendAndMapResponse(
                query,
                command => View(command));
        }

        [HttpPost("info")]
        public async Task<IActionResult> EditProviderInfo(
            EditProviderInfo.Command command,
            ProviderContext providerContext)
        {
            command.ProviderId = providerContext.ProviderInfo.ProviderId;
            return await _mediator.SendAndMapResponse(
                command,
                response => response.Match<IActionResult>(
                    errors => this.ViewFromErrors(errors),
                    success => RedirectToAction("Details", "Provider").WithProviderContext(providerContext)));
        }

        [HttpGet("")]
        public async Task<IActionResult> ProviderDetails(ProviderContext providerContext)
        {
            var request = new ProviderDetails.Query() { ProviderId = providerContext.ProviderInfo.ProviderId };
            return await _mediator.SendAndMapResponse(request, vm => View(vm));
        }
    }
}
