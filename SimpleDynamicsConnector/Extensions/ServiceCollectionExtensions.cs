using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using GuedesPlace.SimpleDynamicsConnector.Models;
using System.Net.Http.Headers;
using Polly;
using Polly.Retry;
using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Extensions;

public static class ServiceCollectionExtensions
{
    private const string KeyedServiceKey = "SimpleDynamicsConnector";

    public static IServiceCollection AddSimpleDynamicsConnector(
        this IServiceCollection services,
        string crmUrl,
        string tenantId,
        string applicationId,
        string applicationSecret,
        Dictionary<string, string>? customPluralTableNames = null, int standardTimeoutSeconds = 60)
    {
        // Validate input parameters
        ArgumentException.ThrowIfNullOrWhiteSpace(crmUrl, nameof(crmUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId, nameof(applicationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationSecret, nameof(applicationSecret));

        // Register DynamicsConnectionConfiguration
        services.Configure<DynamicsConnectionConfiguration>(config =>
        {
            config.CrmUrl = crmUrl;
            config.TenantId = tenantId;
            config.ApplicationId = applicationId;
            config.ApplicationSecret = applicationSecret;
            config.CustomTablePluralMapping = customPluralTableNames ?? [];
        });

        // Register IConfidentialClientApplication as keyed singleton
        services.AddKeyedSingleton<IConfidentialClientApplication>(KeyedServiceKey, (serviceProvider, key) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;
            string authority = $"https://login.microsoftonline.com/{configuration.TenantId}";

            var clientApp = ConfidentialClientApplicationBuilder
                .Create(configuration.ApplicationId)
                .WithClientSecret(configuration.ApplicationSecret)
                .WithAuthority(authority)
                .Build();

            clientApp.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            return clientApp;
        });

        // Register HttpClient for SimpleDynamicsConnector with resilience
        services.AddHttpClient<SimpleDynamicsConnector>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;

            // Configure base address
            client.BaseAddress = new Uri(configuration.CrmUrl + SimpleDynamicsConnector.APIPATH);

            // Configure headers
            client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"*\"");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
        })
        .AddResilienceHandler("DynamicsResilienceHandler", (builder, context) =>
        {
            // Add retry policy with RetryAfter header support
            builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
            {
                MaxRetryAttempts = 3,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                Delay = TimeSpan.FromMilliseconds(100),
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .HandleResult(response =>
                        response.StatusCode == HttpStatusCode.RequestTimeout ||
                        response.StatusCode == HttpStatusCode.TooManyRequests ||
                        response.StatusCode >= HttpStatusCode.InternalServerError),
                DelayGenerator = args =>
                {
                    var response = args.Outcome.Result;
                    // Respect RetryAfter header if present (especially for 429)
                    if (response?.Headers.RetryAfter?.Delta.HasValue ?? false)
                    {
                        var retryAfter = response.Headers.RetryAfter.Delta.Value;
                        // Cap at 60 seconds maximum
                        var delay = retryAfter > TimeSpan.FromSeconds(120) 
                            ? TimeSpan.FromSeconds(120) 
                            : retryAfter;
                        return new ValueTask<TimeSpan?>(delay);
                    }
                    // Fall back to default exponential backoff
                    return new ValueTask<TimeSpan?>((TimeSpan?)null);
                }
            });

            // Add timeout policy
            builder.AddTimeout(TimeSpan.FromSeconds(standardTimeoutSeconds));
        });

        return services;
    }
}
