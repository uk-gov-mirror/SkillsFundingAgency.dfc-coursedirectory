﻿using System.Collections.Generic;
using Dfc.CourseDirectory.FindAnApprenticeshipApi.Models.DAS;
using Dfc.CourseDirectory.FindAnApprenticeshipApi.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Dfc.CourseDirectory.FindAnApprenticeshipApi.Controller
{
    [Produces("application/json")]
    [Route("api")]
    [ApiController]
    public class DocController : ControllerBase
    {
        [Route("bulk/generate")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GenerateProviderExport()
        {
            return Ok();
        }

        [Route("bulk/providers")]
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DasProvider>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetApprenticeshipsAsProvider()
        {
            return Ok();
        }

        [Route("GetApprenticeshipsAsProviderByUkprn")]
        [HttpGet]
        [ProducesResponseType(typeof(DasProviderResultViewModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(DasProviderResultViewModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(DasProviderResultViewModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetApprenticeshipsAsProviderByUkprn()
        {
            return Ok();
        }
    }
}