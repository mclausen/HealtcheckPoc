using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;

namespace HealthChecks
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHealthChecks().AddCheck<SqlCheck>("sql", null, new List<string>()
            {
                "Database",
                "External"
            });
            services.AddHealthChecks().AddCheck<LetsEncryptCheck>("bus", null, new List<string>()
            {
                "External"
            });
            services.AddHealthChecks().AddCheck<ServiceQueueCheck>("le", null, new List<string>()
            {
                "Database",
            });
            services.AddSingleton<IHealthCheckPublisher, Publisher>();
            services.Configure<HealthCheckPublisherOptions>(o =>
            {
                o.Period = TimeSpan.FromSeconds(10);
            });
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseHealthChecks(new PathString("/healthcheck"));
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }

    public class Publisher : IHealthCheckPublisher
    {
        private readonly IHttpClientFactory _clientFactory;

        public Publisher(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
        {
            var _client = _clientFactory.CreateClient();
            await _client.PostAsync("https://hookb.in/oXQWPg83JptWlWL0XLKP",
                new StringContent(JsonConvert.SerializeObject(report)));
        }
    }

    public class SqlCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var addOrUpdate = new ConcurrentDictionary<string, object>();
            addOrUpdate.TryAdd("key", "value");
            return Task.FromResult(HealthCheckResult.Degraded("Sql connection down", new InvalidTimeZoneException(), addOrUpdate));
        }
    }

    public class ServiceQueueCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    public class LetsEncryptCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Lets encrypt is down"));
        }
    }
}
