﻿namespace Dfc.CourseDirectory.Core.DataStore.CosmosDb.Models
{
    public class BulkUploadError
    {
        public int LineNumber { get; set; }
        public string Header { get; set; }
        public string Error { get; set; }
    }
}
