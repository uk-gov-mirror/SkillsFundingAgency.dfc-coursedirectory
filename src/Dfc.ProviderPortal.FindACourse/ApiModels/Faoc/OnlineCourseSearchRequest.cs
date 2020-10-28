﻿using System;
using Dfc.ProviderPortal.FindACourse.Models;

namespace Dfc.ProviderPortal.FindACourse.ApiModels.Faoc
{
    public class OnlineCourseSearchRequest : IPagedRequest
    {
        public string SubjectKeyword { get; set; }
        public string ProviderName { get; set; }
        public string[] QualificationLevels { get; set; }
        public CourseSearchSortBy? SortBy { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public int? Limit { get; set; }
        public int? Start { get; set; }
    }
}
