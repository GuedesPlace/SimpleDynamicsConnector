using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using GuedesPlace.SimpleDynamicsConnector.Abstractions;
using GuedesPlace.SimpleDynamicsConnector.Authentication;
using GuedesPlace.SimpleDynamicsConnector.Models;
using System.Net.Http.Headers;
using Polly;
using Polly.Retry;
using System.Net;

namespace GuedesPlace.SimpleDynamicsConnector.Extensions;

public static class ServiceCollectionExtensions
{
    private const string KeyedServiceKey = "SimpleDynamicsConnector";

    /// <summary>
    /// Registers SimpleDynamicsConnector using app-only authentication (client credentials).
    /// Suitable for server-to-server integrations, background services, and APIs.
    /// </summary>
    /// <param name="crmUrl">Base URL of the Dynamics/Dataverse instance (e.g. <c>https://contoso.crm.dynamics.com</c>).</param>
    /// <param name="tenantId">Azure AD tenant ID.</param>
    /// <param name="applicationId">Azure AD application (client) ID.</param>
    /// <param name="applicationSecret">Azure AD application client secret.</param>
    /// <param name="customPluralTableNames">Optional custom entity name → plural name mapping for non-standard entities.</param>
    /// <param name="standardTimeoutSeconds">Per-request timeout in seconds. Defaults to 60.</param>
    public static IServiceCollection AddSimpleDynamicsConnector(
        this IServiceCollection services,
        string crmUrl,
        string tenantId,
        string applicationId,
        string applicationSecret,
        Dictionary<string, string>? customPluralTableNames = null,
        int standardTimeoutSeconds = 60)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(crmUrl, nameof(crmUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId, nameof(applicationId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationSecret, nameof(applicationSecret));

        services.Configure<DynamicsConnectionConfiguration>(config =>
        {
            config.CrmUrl = crmUrl;
            config.TenantId = tenantId;
            config.ApplicationId = applicationId;
            config.ApplicationSecret = applicationSecret;
            config.AuthenticationMode = DynamicsAuthenticationMode.ClientCredentials;
            config.CustomTablePluralMapping = customPluralTableNames ?? [];
        });

        services.AddKeyedSingleton<IDynamicsTokenProvider>(KeyedServiceKey, (serviceProvider, _) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;
            string authority = $"https://login.microsoftonline.com/{configuration.TenantId}";

            var clientApp = ConfidentialClientApplicationBuilder
                .Create(configuration.ApplicationId)
                .WithClientSecret(configuration.ApplicationSecret!)
                .WithAuthority(authority)
                .Build();

            clientApp.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            return new ClientCredentialsTokenProvider(clientApp);
        });

        RegisterHttpClient(services, standardTimeoutSeconds);
        return services;
    }

    /// <summary>
    /// Registers SimpleDynamicsConnector using interactive user authentication (browser popup).
    /// Suitable for desktop CLIs and tools that have UI access.
    /// </summary>
    /// <remarks>
    /// The Entra ID app registration must be configured as a <b>Public Client</b> with a
    /// <c>http://localhost</c> redirect URI under <em>Mobile and desktop applications</em>.
    /// </remarks>
    /// <param name="crmUrl">Base URL of the Dynamics/Dataverse instance.</param>
    /// <param name="tenantId">Azure AD tenant ID.</param>
    /// <param name="applicationId">Azure AD application (client) ID of the public client app.</param>
    /// <param name="customPluralTableNames">Optional custom entity name → plural name mapping.</param>
    /// <param name="standardTimeoutSeconds">Per-request timeout in seconds. Defaults to 60.</param>
    public static IServiceCollection AddSimpleDynamicsConnectorWithInteractiveAuth(
        this IServiceCollection services,
        string crmUrl,
        string tenantId,
        string applicationId,
        Dictionary<string, string>? customPluralTableNames = null,
        int standardTimeoutSeconds = 60)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(crmUrl, nameof(crmUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId, nameof(applicationId));

        services.Configure<DynamicsConnectionConfiguration>(config =>
        {
            config.CrmUrl = crmUrl;
            config.TenantId = tenantId;
            config.ApplicationId = applicationId;
            config.AuthenticationMode = DynamicsAuthenticationMode.Interactive;
            config.CustomTablePluralMapping = customPluralTableNames ?? [];
        });

        services.AddKeyedSingleton<IDynamicsTokenProvider>(KeyedServiceKey, (serviceProvider, _) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;
            string authority = $"https://login.microsoftonline.com/{configuration.TenantId}";

            var clientApp = PublicClientApplicationBuilder
                .Create(configuration.ApplicationId)
                .WithAuthority(authority)
                .WithDefaultRedirectUri()
                .Build();

            clientApp.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            return new PublicClientTokenProvider(clientApp, DynamicsAuthenticationMode.Interactive);
        });

        RegisterHttpClient(services, standardTimeoutSeconds);
        return services;
    }

    /// <summary>
    /// Registers SimpleDynamicsConnector using the device code flow.
    /// Suitable for headless CLIs, SSH sessions, and environments without a browser.
    /// The user is prompted to visit <c>https://microsoft.com/devicelogin</c> and enter a code.
    /// </summary>
    /// <remarks>
    /// The Entra ID app registration must have <b>Allow public client flows</b> enabled.
    /// </remarks>
    /// <param name="crmUrl">Base URL of the Dynamics/Dataverse instance.</param>
    /// <param name="tenantId">Azure AD tenant ID.</param>
    /// <param name="applicationId">Azure AD application (client) ID of the public client app.</param>
    /// <param name="customPluralTableNames">Optional custom entity name → plural name mapping.</param>
    /// <param name="standardTimeoutSeconds">Per-request timeout in seconds. Defaults to 60.</param>
    /// <param name="deviceCodeCallback">
    /// Optional callback invoked with the device code instructions.
    /// Defaults to <see cref="Console.WriteLine"/> when <see langword="null"/>.
    /// </param>
    public static IServiceCollection AddSimpleDynamicsConnectorWithDeviceCodeAuth(
        this IServiceCollection services,
        string crmUrl,
        string tenantId,
        string applicationId,
        Dictionary<string, string>? customPluralTableNames = null,
        int standardTimeoutSeconds = 60,
        Func<DeviceCodeResult, Task>? deviceCodeCallback = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(crmUrl, nameof(crmUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationId, nameof(applicationId));

        services.Configure<DynamicsConnectionConfiguration>(config =>
        {
            config.CrmUrl = crmUrl;
            config.TenantId = tenantId;
            config.ApplicationId = applicationId;
            config.AuthenticationMode = DynamicsAuthenticationMode.DeviceCode;
            config.CustomTablePluralMapping = customPluralTableNames ?? [];
        });

        services.AddKeyedSingleton<IDynamicsTokenProvider>(KeyedServiceKey, (serviceProvider, _) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;
            string authority = $"https://login.microsoftonline.com/{configuration.TenantId}";

            var clientApp = PublicClientApplicationBuilder
                .Create(configuration.ApplicationId)
                .WithAuthority(authority)
                .WithDefaultRedirectUri()
                .Build();

            clientApp.UserTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);

            return new PublicClientTokenProvider(clientApp, DynamicsAuthenticationMode.DeviceCode, deviceCodeCallback);
        });

        RegisterHttpClient(services, standardTimeoutSeconds);
        return services;
    }

    private static void RegisterHttpClient(IServiceCollection services, int standardTimeoutSeconds)
    {
        services.AddHttpClient<SimpleDynamicsConnector>((serviceProvider, client) =>
        {
            var configuration = serviceProvider.GetRequiredService<IOptions<DynamicsConnectionConfiguration>>().Value;

            client.BaseAddress = new Uri(configuration.CrmUrl + SimpleDynamicsConnector.APIPATH);

            client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"*\"");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
            client.DefaultRequestHeaders.Add("OData-Version", "4.0");
        })
        .AddResilienceHandler("DynamicsResilienceHandler", (builder, context) =>
        {
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
                    if (response?.Headers.RetryAfter?.Delta.HasValue ?? false)
                    {
                        var retryAfter = response.Headers.RetryAfter.Delta.Value;
                        var delay = retryAfter > TimeSpan.FromSeconds(120)
                            ? TimeSpan.FromSeconds(120)
                            : retryAfter;
                        return new ValueTask<TimeSpan?>(delay);
                    }
                    return new ValueTask<TimeSpan?>((TimeSpan?)null);
                }
            });

            builder.AddTimeout(TimeSpan.FromSeconds(standardTimeoutSeconds));
        });
    }
}
