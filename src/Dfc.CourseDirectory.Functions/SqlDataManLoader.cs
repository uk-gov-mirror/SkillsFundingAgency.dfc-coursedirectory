using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Dfc.CourseDirectory.Functions
{
    public class SqlDataManLoader
    {
        private readonly IConfiguration _configuration;

        public SqlDataManLoader(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [FunctionName(nameof(SqlDataManLoader))]
        public async Task<IActionResult> Execute([HttpTrigger(methods: "POST", Route = "SqlDataManLoader")] HttpRequest request)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var ukprn = 10001309;

            Request paramz;

            using (StreamReader streamReader = new StreamReader(request.Body))
            {
                var requestBody = await streamReader.ReadToEndAsync();
                paramz = JsonConvert.DeserializeObject<Request>(requestBody);
            }

            var results = new ConcurrentBag<long>();

            await Task.WhenAll(Enumerable.Range(0, paramz.Concurrency).Select(async _ =>
            {
                for (int i = 0; i < paramz.Iterations; i++)
                {
                    results.Add(await Task.Run(RunIteration));
                }
            }));

            var min = results.Min();
            var max = results.Max();
            var avg = results.Average();

            var response = new Response()
            {
                Rows = paramz.Rows,
                Iterations = paramz.Iterations,
                Concurrency = paramz.Concurrency,
                Min = min,
                Max = max,
                Average = avg
            };

            return new JsonResult(response);

            async Task<long> RunIteration()
            {
                var sw = Stopwatch.StartNew();

                using var connection = new SqlConnection(connectionString);
                connection.Open();

                var transaction = connection.BeginTransaction(IsolationLevel.Snapshot);

                var coursesTable = new DataTable();
                coursesTable.Columns.Add("CourseId", typeof(Guid));
                coursesTable.Columns.Add("CourseStatus", typeof(int));
                coursesTable.Columns.Add("CreatedOn", typeof(DateTime));
                coursesTable.Columns.Add("CreatedBy", typeof(string));
                coursesTable.Columns.Add("UpdatedOn", typeof(DateTime));
                coursesTable.Columns.Add("UpdatedBy", typeof(string));
                coursesTable.Columns.Add("LearnAimRef", typeof(string));
                coursesTable.Columns.Add("ProviderUkprn", typeof(int));
                coursesTable.Columns.Add("CourseDescription", typeof(string));
                coursesTable.Columns.Add("EntryRequirements", typeof(string));
                coursesTable.Columns.Add("WhatYoullLearn", typeof(string));
                coursesTable.Columns.Add("HowYoullLearn", typeof(string));
                coursesTable.Columns.Add("WhatYoullNeed", typeof(string));
                coursesTable.Columns.Add("HowYoullBeAssessed", typeof(string));
                coursesTable.Columns.Add("WhereNext", typeof(string));

                var courseRunsTable = new DataTable();
                courseRunsTable.Columns.Add("CourseRunId", typeof(Guid));
                courseRunsTable.Columns.Add("CourseId", typeof(Guid));
                courseRunsTable.Columns.Add("CourseRunStatus", typeof(int));
                courseRunsTable.Columns.Add("CreatedOn", typeof(DateTime));
                courseRunsTable.Columns.Add("CreatedBy", typeof(string));
                courseRunsTable.Columns.Add("UpdatedOn", typeof(DateTime));
                courseRunsTable.Columns.Add("UpdatedBy", typeof(string));
                courseRunsTable.Columns.Add("CourseName", typeof(string));
                courseRunsTable.Columns.Add("DeliveryMode", typeof(int));
                courseRunsTable.Columns.Add("FlexibleStartDate", typeof(bool));
                courseRunsTable.Columns.Add("StartDate", typeof(DateTime));
                courseRunsTable.Columns.Add("CourseWebsite", typeof(string));
                courseRunsTable.Columns.Add("Cost", typeof(int));
                courseRunsTable.Columns.Add("CostDescription", typeof(string));
                courseRunsTable.Columns.Add("DurationUnit", typeof(int));
                courseRunsTable.Columns.Add("DurationValue", typeof(int));
                courseRunsTable.Columns.Add("StudyMode", typeof(int));
                courseRunsTable.Columns.Add("AttendancePattern", typeof(int));
                courseRunsTable.Columns.Add("National", typeof(bool));

                for (int i = 0; i < paramz.Rows; i++)
                {
                    var courseId = Guid.NewGuid();

                    coursesTable.Rows.Add(new object[]
                    {
                        courseId,
                        1,
                        DateTime.UtcNow,
                        "SqlDataManPoc",
                        DateTime.UtcNow,
                        "SqlDataManPoc",
                        "12345",  // No FK on LearnAimRef
                        ukprn,
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                        Faker.Lorem.Paragraphs(4),
                    });

                    var courseRunId = Guid.NewGuid();

                    courseRunsTable.Rows.Add(new object[]
                    {
                        courseRunId,
                        courseId,
                        1,
                        DateTime.UtcNow,
                        "SqlDataManPoc",
                        DateTime.UtcNow,
                        "SqlDataManPoc",
                        Faker.Lorem.Sentence(),
                        1,
                        false,
                        DateTime.UtcNow.AddYears(1),
                        Faker.Internet.Url(),
                        10,
                        null,
                        1,
                        1,
                        1,
                        1,
                        false
                    });
                }

                using (var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.ColumnMappings.Add("CourseId", "CourseId");
                    bulkCopy.ColumnMappings.Add("CourseStatus", "CourseStatus");
                    bulkCopy.ColumnMappings.Add("CreatedOn", "CreatedOn");
                    bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");
                    bulkCopy.ColumnMappings.Add("UpdatedBy", "UpdatedBy");
                    bulkCopy.ColumnMappings.Add("LearnAimRef", "LearnAimRef");
                    bulkCopy.ColumnMappings.Add("ProviderUkprn", "ProviderUkprn");
                    bulkCopy.ColumnMappings.Add("CourseDescription", "CourseDescription");
                    bulkCopy.ColumnMappings.Add("EntryRequirements", "EntryRequirements");
                    bulkCopy.ColumnMappings.Add("WhatYoullLearn", "WhatYoullLearn");
                    bulkCopy.ColumnMappings.Add("HowYoullLearn", "HowYoullLearn");
                    bulkCopy.ColumnMappings.Add("WhatYoullNeed", "WhatYoullNeed");
                    bulkCopy.ColumnMappings.Add("HowYoullBeAssessed", "HowYoullBeAssessed");
                    bulkCopy.ColumnMappings.Add("WhereNext", "WhereNext");

                    bulkCopy.BulkCopyTimeout = 0;
                    bulkCopy.DestinationTableName = "Pttcd.Courses";
                    await bulkCopy.WriteToServerAsync(coursesTable);
                }

                using (var bulkCopy = new SqlBulkCopy(transaction.Connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.ColumnMappings.Add("CourseRunId", "CourseRunId");
                    bulkCopy.ColumnMappings.Add("CourseId", "CourseId");
                    bulkCopy.ColumnMappings.Add("CourseRunStatus", "CourseRunStatus");
                    bulkCopy.ColumnMappings.Add("CreatedOn", "CreatedOn");
                    bulkCopy.ColumnMappings.Add("CreatedBy", "CreatedBy");
                    bulkCopy.ColumnMappings.Add("UpdatedOn", "UpdatedOn");
                    bulkCopy.ColumnMappings.Add("UpdatedBy", "UpdatedBy");
                    bulkCopy.ColumnMappings.Add("CourseName", "CourseName");
                    bulkCopy.ColumnMappings.Add("DeliveryMode", "DeliveryMode");
                    bulkCopy.ColumnMappings.Add("FlexibleStartDate", "FlexibleStartDate");
                    bulkCopy.ColumnMappings.Add("StartDate", "StartDate");
                    bulkCopy.ColumnMappings.Add("CourseWebsite", "CourseWebsite");
                    bulkCopy.ColumnMappings.Add("Cost", "Cost");
                    bulkCopy.ColumnMappings.Add("CostDescription", "CostDescription");
                    bulkCopy.ColumnMappings.Add("DurationUnit", "DurationUnit");
                    bulkCopy.ColumnMappings.Add("DurationValue", "DurationValue");
                    bulkCopy.ColumnMappings.Add("StudyMode", "StudyMode");
                    bulkCopy.ColumnMappings.Add("AttendancePattern", "AttendancePattern");
                    bulkCopy.ColumnMappings.Add("National", "National");

                    bulkCopy.BulkCopyTimeout = 0;
                    bulkCopy.DestinationTableName = "Pttcd.CourseRuns";
                    await bulkCopy.WriteToServerAsync(courseRunsTable);
                }

                transaction.Rollback();

                sw.Stop();

                return sw.ElapsedMilliseconds;
            }
        }

        private class Request
        {
            public int Rows { get; set; } = 1000;
            public int Iterations { get; set; } = 5;
            public int Concurrency { get; set; } = 5;
        }

        private class Response
        {
            public int Rows { get; set; }
            public int Iterations { get; set; }
            public int Concurrency { get; set; }
            public long Min { get; set; }
            public long Max { get; set; }
            public double Average { get; set; }
        }
    }
}
