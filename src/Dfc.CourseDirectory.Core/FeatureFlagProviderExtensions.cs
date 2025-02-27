﻿using System;
using System.Linq;

namespace Dfc.CourseDirectory.Core
{
    public static class FeatureFlagProviderExtensions
    {
        public static bool HaveFeature(this IFeatureFlagProvider featureFlagProvider, string feature) =>
            featureFlagProvider.GetFeatureFlags().Contains(feature, StringComparer.OrdinalIgnoreCase);
    }
}
