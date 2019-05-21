﻿
using Dfc.CourseDirectory.Models.Models.Courses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Dfc.CourseDirectory.Services.Interfaces.CourseService;
using Dfc.CourseDirectory.Web.ViewModels.YourCourses;
using System.ComponentModel.DataAnnotations;
using Dfc.CourseDirectory.Models.Enums;
using Dfc.CourseDirectory.Web.ViewComponents.Courses.ChooseRegion;

namespace Dfc.CourseDirectory.Web.ViewModels.Apprenticeships
{
    public class ApprenticeshipRegionsViewModel
    {

        public ChooseRegionModel ChooseRegion { get; set; }


    }
}
