﻿using System;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Logging;

namespace Dfc.CourseDirectory.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddConfiguration(context.Configuration.GetSection("Logging"));
                    var appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                    builder.AddApplicationInsights(appInsightsKey);
                })
                .ConfigureAppConfiguration(builder =>
                {
                    var environmentVariablesConfigurationSource = builder.Sources.OfType<EnvironmentVariablesConfigurationSource>().Single();
                    builder.Sources.Remove(environmentVariablesConfigurationSource);

                    var environmentName = Environment.GetEnvironmentVariable("EnvironmentSettings__EnvironmentName");

                    if (!string.IsNullOrEmpty(environmentName))
                    {
                        builder.AddJsonFile($"appsettings.Environment.{environmentName}.json", optional: true);
                    }

                    builder.Sources.Add(environmentVariablesConfigurationSource);
                });
    }
}
