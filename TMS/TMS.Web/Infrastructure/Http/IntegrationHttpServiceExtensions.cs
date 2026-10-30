using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TMS.ViewModels.Integration;

namespace TMS.Web.Infrastructure.Http
{
    public static class IntegrationHttpServiceExtensions
    {
        public static IServiceCollection AddIntegrationHttpInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<IntegrationHttpOptions>(
                configuration.GetSection(IntegrationHttpOptions.SectionName));

            services.AddTransient<CorrelationIdDelegatingHandler>();
            services.AddTransient<SanitizedLoggingDelegatingHandler>();
            services.AddTransient<ResilientRetryDelegatingHandler>();

            return services;
        }

        public static IHttpClientBuilder AddIntegrationResilienceHandlers(this IHttpClientBuilder builder)
        {
            return builder
                .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
                .AddHttpMessageHandler<SanitizedLoggingDelegatingHandler>()
                .AddHttpMessageHandler<ResilientRetryDelegatingHandler>();
        }
    }
}
