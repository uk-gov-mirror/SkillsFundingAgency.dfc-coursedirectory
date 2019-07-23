﻿using System;
using Dfc.CourseDirectory.Common.Interfaces;
using Dfc.CourseDirectory.Models.Interfaces.Apprenticeships;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dfc.CourseDirectory.Services.Interfaces.ApprenticeshipService
{
    public interface IApprenticeshipService
    {
        Task<IResult<IEnumerable<IStandardsAndFrameworks>>> StandardsAndFrameworksSearch(string criteria);
        Task<IResult<IApprenticeshipProvider>> AddApprenticeship(IApprenticeshipProvider apprenticeship);
        Task<IResult<IEnumerable<IApprenticeship>>> GetApprenticeshipByUKPRN(string criteria);

        Task<IResult<IApprenticeship>> GetApprenticeshipByIdAsync(string Id);

        Task<IResult<IApprenticeship>> UpdateApprenticeshipAsync(IApprenticeship apprenticeship);
    }
}
