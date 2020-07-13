using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using Web.Core.Infrastructure;
using Web.Core.Mvc;
using Web.Core.WebApi.DependencyInjection;
using Web.Core.WebApi.Middleware;

namespace Acme.WebApi
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            Environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            ConfigureJsonSerializerOptions(services);
            services.AddTransient<ProblemDetailsFactory, ErrorDetailsProblemDetailsFactory>(); // must be called after `services.AddControllers();` as that is where the default factory is registered.            

            services.AddLogging();
            services.ConfigureApiVersioning();
            services.ConfigureSwaggerDocWithVersioning("Acme.WebApi - API Documentation");

            services.Configure<ExceptionProblemDetailsOptions>(Configuration.GetSection(ExceptionProblemDetailsOptions.ExceptionProblemDetails));
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseMiddleware<ErrorHandlingMiddleware>();

            if (!Environment.IsProduction())
            {
                app.UseSwaggerWithDocumentation(new []
                {
                    Assembly.GetEntryAssembly(),
                });
            }

            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        private void ConfigureJsonSerializerOptions(IServiceCollection services)
        {
            var jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
            };

            jsonSerializerOptions.Converters.Add(new ErrorProblemDetailsJsonConverterFactory());
            jsonSerializerOptions.Converters.Add(
                new ExceptionProblemDetailsJsonConverter(
                    Configuration.GetSection(ExceptionProblemDetailsOptions.ExceptionProblemDetails).Get<ExceptionProblemDetailsOptions>()
                ));

            services.AddControllersWithViews(mvcOptions => mvcOptions.RespectBrowserAcceptHeader = true)
                .AddJsonOptions(jsonOptions =>
                {
                    jsonOptions.JsonSerializerOptions.IgnoreNullValues = jsonSerializerOptions.IgnoreNullValues;
                    jsonOptions.JsonSerializerOptions.WriteIndented = jsonSerializerOptions.WriteIndented;
                    jsonOptions.JsonSerializerOptions.PropertyNameCaseInsensitive = jsonSerializerOptions.PropertyNameCaseInsensitive;
                    foreach (var converter in jsonSerializerOptions.Converters)
                    {
                        jsonOptions.JsonSerializerOptions.Converters.Add(converter);
                    }
                });

            services.AddTransient(_ => Options.Create(jsonSerializerOptions));
        }
    }
}
