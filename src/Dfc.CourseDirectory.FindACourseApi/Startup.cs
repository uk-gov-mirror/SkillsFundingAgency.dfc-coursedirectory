﻿using System;
using System.Collections.Generic;
using Dfc.CourseDirectory.Core.DataStore.CosmosDb;
using Dfc.CourseDirectory.Core.Search.AzureSearch;
using Dfc.CourseDirectory.Core.Search.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;

namespace Dfc.CourseDirectory.FindACourseApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddNewtonsoftJson(jsonOptions => jsonOptions.SerializerSettings.Converters.Insert(0, new StringEnumConverter()));

            services.AddApplicationInsightsTelemetry(Configuration);

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Find a Course API", Version = "v1" });
            });

            services
                .AddMediatR(typeof(Startup).Assembly);

            if (Environment.EnvironmentName != "Testing")
            {
                services.AddCosmosDbDataStore(
                    endpoint: new Uri(Configuration["CosmosDbSettings:EndpointUri"]),
                    key: Configuration["CosmosDbSettings:PrimaryKey"]);

                services.AddAzureSearchClient<Onspd>(
                    new Uri(Configuration["AzureSearchUrl"]),
                    Configuration["AzureSearchQueryKey"],
                    indexName: "onspd");

                services.AddAzureSearchClient<Course>(
                    new Uri(Configuration["AzureSearchUrl"]),
                    Configuration["AzureSearchQueryKey"],
                    indexName: "course",
                    options =>
                    {
                        // The default options yield DateTime's with type Local;
                        // we need them to be Utc to maintain the behaviour with when the API was built upon
                        // the old SDK.
                        // Overriding the serializer here is sufficient to get Utc DateTimes.
                        options.Serializer = new Azure.Core.Serialization.JsonObjectSerializer();
                    });

                services.AddAzureSearchClient<Lars>(
                    new Uri(Configuration["AzureSearchUrl"]),
                    Configuration["AzureSearchQueryKey"],
                    "lars");
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseSwagger(c =>
            {
                c.SerializeAsV2 = true;

                c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
                {
                    swaggerDoc.Servers = new List<OpenApiServer>
                    {
                        new OpenApiServer { Url = $"{httpReq.Scheme}://{httpReq.Host.Value}" }
                    };
                });
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Find a Course API");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
