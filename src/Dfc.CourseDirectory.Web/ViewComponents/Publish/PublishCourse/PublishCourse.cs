﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dfc.CourseDirectory.Models.Models.Courses;

namespace Dfc.CourseDirectory.Web.ViewComponents.Publish.PublishCourse
{
    public class PublishCourse : ViewComponent
    {
        public IViewComponentResult Invoke(IEnumerable<Course> model)
        {
            return View("~/ViewComponents/Publish/PublishCourse/Default.cshtml", model);
        }
    }
}
