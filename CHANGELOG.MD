# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-04-30

### ⚠️ Breaking Changes

#### .NET 10 Required

- **v1.2.1:** Supports .NET 8 and .NET 9
- **v2.0.0:** Requires .NET 10 or higher
- **Migration:** Update your project to .NET 10 and adjust the `TargetFramework` in your `.csproj` file

**Important:** Users who need to use .NET 8 or .NET 9 should stay on version 1.2.1. This version will continue to be supported as a legacy version.

#### Dependency Injection Registration (Breaking Change)

Manual configuration has been replaced with a simplified extension method.

**v1.2.1 (Old - No longer supported):**
```csharp
builder.Services.AddOptions<DynamicsConnectionConfiguration>().Configure(c =>
{
    c.ApplicationId = applicationID;
    c.ApplicationSecret = applicationSecret;
    c.CrmUrl = crmUrl;
    c.TenantId = tenantId;
});

builder.Services.AddHttpClient<SimpleDynamicsConnector>()
    .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>
        (msg => msg.Headers.RetryAfter is not null)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (_, response, _) => response.Result.Headers.RetryAfter.Delta.Value,
            onRetryAsync: (_, _, _, _) => Task.CompletedTask
        ));
```

**v2.0.0 (New - Required):**
```csharp
builder.Services.AddSimpleDynamicsConnector(
    crmUrl: crmUrl,
    tenantId: tenantId,
    applicationId: applicationId,
    applicationSecret: applicationSecret,
    customPluralTableNames: null  // Optional: Dictionary<string, string>
);
```

**Migration required:**
- Remove all manual `AddOptions<DynamicsConnectionConfiguration>` calls
- Remove manual `AddHttpClient<SimpleDynamicsConnector>` registrations
- Remove manual `AddPolicyHandler` configurations
- Use the new `AddSimpleDynamicsConnector()` extension method instead
- The new method automatically configures: HttpClient, Headers, Resilience Handler, Token Management

#### Constructor Signature Changed

**v1.2.1:**
```csharp
public SimpleDynamicsConnector(
    HttpClient client, 
    IOptions<DynamicsConnectionConfiguration> configuration)
```

**v2.0.0:**
```csharp
public SimpleDynamicsConnector(
    HttpClient client, 
    IOptions<DynamicsConnectionConfiguration> configuration,
    [FromKeyedServices("SimpleDynamicsConnector")] IConfidentialClientApplication clientAuthApp)
```

**Impact:**
- Direct instantiation of `SimpleDynamicsConnector` no longer works
- Use Dependency Injection via the extension method
- `IConfidentialClientApplication` is registered as a keyed service and automatically injected

#### Configuration Model Uses `required` Keyword

The `DynamicsConnectionConfiguration` class now uses the `required` keyword (C# 11+):

```csharp
public class DynamicsConnectionConfiguration
{
    required public string CrmUrl { set; get; }
    required public string TenantId { set; get; }
    required public string ApplicationId { set; get; }
    required public string ApplicationSecret { set; get; }
    public Dictionary<string,string> CustomTablePluralMapping { set; get; } = new Dictionary<string,string>();
}
```

**Impact:**
- All required fields must be initialized during configuration
- Code that relies on `null` initialization will not compile
- Better compile-time safety

### ✨ New Features

#### Built-in Resilience Handler with RetryAfter Support

Version 2.0.0 includes a fully integrated Resilience Handler based on Microsoft.Extensions.Http.Resilience (Polly V8).

**Automatic handling of:**
- **429 Too Many Requests:** Respects the `RetryAfter` header from Dynamics/Dataverse (max 120 seconds wait time)
- **408 Request Timeout:** Exponential backoff with jitter
- **500-504 Server Errors:** Exponential backoff with jitter
- **Network failures (HttpRequestException):** Automatic retry attempts

**Default configuration:**
- **Max retry attempts:** 3 attempts
- **Timeout per request:** 60 seconds
- **Backoff strategy:** Exponential with jitter
- **Base delay:** 100ms
- **RetryAfter cap:** Maximum 120 seconds wait time

**Benefit:** No manual Polly configuration needed - works automatically after registration with `AddSimpleDynamicsConnector()`.

#### AddSimpleDynamicsConnector Extension Method

A new, central extension method simplifies the entire service registration:

```csharp
public static IServiceCollection AddSimpleDynamicsConnector(
    this IServiceCollection services,
    string crmUrl,
    string tenantId,
    string applicationId,
    string applicationSecret,
    Dictionary<string, string>? customPluralTableNames = null)
```

**The method automatically configures:**
- `DynamicsConnectionConfiguration` via `IOptions<T>` pattern
- HttpClient with base address and all required headers
- `IConfidentialClientApplication` as keyed singleton
- Resilience handler with RetryAfter support
- Parameter validation at startup

**Example with custom plural mappings:**
```csharp
var customMappings = new Dictionary<string, string> 
{ 
    { "custom_entity", "custom_entities_custom" },
    { "my_address", "my_addresses_plural" }
};

builder.Services.AddSimpleDynamicsConnector(
    crmUrl: crmUrl,
    tenantId: tenantId,
    applicationId: applicationId,
    applicationSecret: applicationSecret,
    customPluralTableNames: customMappings
);
```

#### Automatic HttpClient Header Configuration

The extension method automatically configures all required HTTP headers:

- `Prefer: odata.include-annotations="*"`
- `Accept: application/json`
- `Accept-Charset: utf-8`
- `OData-MaxVersion: 4.0`
- `OData-Version: 4.0`
- `Base Address: {CrmUrl}/api/data/v9.2/`

**Benefit:** No manual header configuration required.

### 🔧 Improvements

#### Enhanced Exception Handling

All Dynamics-specific exceptions now contain structured error information:

**New exception properties:**
- `ErrorContent` - Server error content for debugging
- `HttpMethod` - HTTP method of the failed request
- `Path` - API path of the failed request
- `StatusCode` - HTTP status code
- `RequestPayload` - Request payload for debugging

**Exception types:**
- `DynamicsConnectorException` - Base exception with full context
- `DynamicsRateLimitException` - 429 responses with RetryAfter timing
- `DynamicsAuthenticationException` - 401/403 responses
- `DynamicsEntityNotFoundException` - 404 responses with entity details
- `DynamicsMissingEntityIdException` - Missing response headers

**Benefit:** Better error diagnostics and programmatic exception handling.

#### Shared Token Cache for Better Performance

The token cache now uses `EnableSharedCacheOptions` for better performance in multi-instance deployments:

```csharp
clientApp.AppTokenCache.SetCacheOptions(CacheOptions.EnableSharedCacheOptions);
```

**Benefit:** Better token reuse, especially in cloud scenarios (Azure App Service, containers).

#### Configuration Validation at Startup

The extension method validates all parameters at startup:

```csharp
ArgumentException.ThrowIfNullOrWhiteSpace(crmUrl, nameof(crmUrl));
ArgumentException.ThrowIfNullOrWhiteSpace(tenantId, nameof(tenantId));
ArgumentException.ThrowIfNullOrWhiteSpace(applicationId, nameof(applicationId));
ArgumentException.ThrowIfNullOrWhiteSpace(applicationSecret, nameof(applicationSecret));
```

**Benefit:** Configuration errors are detected immediately at startup, not at runtime.

#### Token Acquisition with Retry Logic

The `BuildRequestMessageAsync()` method includes built-in retry logic for token acquisition:

- **Max retry attempts:** 3 attempts
- **Backoff:** Exponential (100ms, 200ms, 400ms)
- **Error handling:** Automatic retry on `HttpRequestException`

**Benefit:** More robust authentication even with temporary network issues.

### 📦 Dependencies

#### New Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
|---------|---------|-------|
| **Microsoft.Extensions.Http.Resilience** | **10.5.0** | **NEW** - Polly V8 integration for resilience handler |

#### Updated Dependencies

| Package | v1.2.1 | v2.0.0 | Change |
|---------|---------|--------|----------|
| Microsoft.Extensions.DependencyInjection | 8.x/9.x | **10.0.7** | Updated for keyed services support |
| Microsoft.Extensions.Http | 8.x/9.x | **10.0.7** | Updated |
| Microsoft.Extensions.Options | 8.x/9.x | **10.0.7** | Updated |
| Microsoft.Identity.Client | 4.x | **4.83.3** | Token cache improvements |
| Newtonsoft.Json | 13.x | **13.0.4** | Maintained |

#### Removed Dependencies

- **Manual Polly installation no longer needed:** v1.2.1 required a direct `Polly` package reference. v2.0.0 uses `Microsoft.Extensions.Http.Resilience`, which already includes Polly V8.

**Migration:** If you manually referenced `Polly`, remove this reference.

### 📖 Migration Guide: v1.2.1 → v2.0.0

#### Step-by-Step Checklist

- [ ] **Update .NET version to 10.0 or higher**
  - [ ] Update `<TargetFramework>` in `.csproj` from `net8.0`/`net9.0` to `net10.0`
  - [ ] Ensure your runtime environment supports .NET 10

- [ ] **Update package references**
  - [ ] Add: `<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0" />` (automatically included transitively)
  - [ ] Remove: Manual `Polly` package reference (if present)
  - [ ] Update: `GuedesPlace.SimpleDynamicsConnector` to version 2.0.0

- [ ] **Refactor Program.cs / Startup.cs**
  - [ ] Remove old registration:
    ```csharp
    // REMOVE:
    builder.Services.AddOptions<DynamicsConnectionConfiguration>().Configure(c => { ... });
    builder.Services.AddHttpClient<SimpleDynamicsConnector>().AddPolicyHandler(...);
    ```
  - [ ] Add new registration:
    ```csharp
    // NEW:
    builder.Services.AddSimpleDynamicsConnector(
        crmUrl: crmUrl,
        tenantId: tenantId,
        applicationId: applicationId,
        applicationSecret: applicationSecret,
        customPluralTableNames: null  // or your custom mappings
    );
    ```

- [ ] **Migrate custom plural mappings** (if used)
  ```csharp
  var customMappings = new Dictionary<string, string> 
  { 
      { "your_entity", "your_entities_plural" }
  };
  
  builder.Services.AddSimpleDynamicsConnector(
      crmUrl: crmUrl,
      tenantId: tenantId,
      applicationId: applicationId,
      applicationSecret: applicationSecret,
      customPluralTableNames: customMappings
  );
  ```

- [ ] **Remove manual IConfidentialClientApplication registration** (if present)
  - The extension method registers this automatically as a keyed service

- [ ] **Test your application**
  - [ ] Test CRUD operations
  - [ ] Test resilience with rate-limiting scenarios (429 responses)
  - [ ] Verify token caching behavior
  - [ ] Test exception handling and error messages

- [ ] **Update integration tests** (if present)
  - Adjust tests that need to mock keyed services
  - Verify resilience handler behavior

- [ ] **Optional: Use new exception properties**
  ```csharp
  try 
  {
      await connector.CreateRecordAsync("account", payload);
  }
  catch (DynamicsConnectorException ex)
  {
      // New in v2.0.0: Structured error information
      Console.WriteLine($"Method: {ex.HttpMethod}");
      Console.WriteLine($"Path: {ex.Path}");
      Console.WriteLine($"Status: {ex.StatusCode}");
      Console.WriteLine($"Error: {ex.ErrorContent}");
  }
  ```

#### Code Example: Complete Migration

**Before (v1.2.1):**
```csharp
var builder = WebApplication.CreateBuilder(args);

var applicationID = builder.Configuration["crmApplicationId"];
var applicationSecret = builder.Configuration["crmApplicationSecret"];
var tenantId = builder.Configuration["tenantId"];
var crmUrl = builder.Configuration["crmUrl"];

// Old manual configuration
builder.Services.AddOptions<DynamicsConnectionConfiguration>().Configure(c =>
{
    c.ApplicationId = applicationID;
    c.ApplicationSecret = applicationSecret;
    c.CrmUrl = crmUrl;
    c.TenantId = tenantId;
});

builder.Services.AddHttpClient<SimpleDynamicsConnector>()
    .AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>
        (msg => msg.Headers.RetryAfter is not null)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (_, response, _) => response.Result.Headers.RetryAfter.Delta.Value,
            onRetryAsync: (_, _, _, _) => Task.CompletedTask
        ));

var app = builder.Build();
```

**After (v2.0.0):**
```csharp
var builder = WebApplication.CreateBuilder(args);

var applicationId = builder.Configuration["crmApplicationId"];
var applicationSecret = builder.Configuration["crmApplicationSecret"];
var tenantId = builder.Configuration["tenantId"];
var crmUrl = builder.Configuration["crmUrl"];

// New simplified registration
builder.Services.AddSimpleDynamicsConnector(
    crmUrl: crmUrl,
    tenantId: tenantId,
    applicationId: applicationId,
    applicationSecret: applicationSecret,
    customPluralTableNames: null
);

var app = builder.Build();
```

### 📝 What Stays the Same (Backward Compatible)

✅ **All CRUD operations** - Method signatures and behavior identical  
✅ **Model classes** - `EntityReference`, `MultipleRecordsResponse`, `BatchInstruction` unchanged  
✅ **Exception types** - All exception types available (with enhanced properties)  
✅ **OData query support** - Query parameter handling unchanged  
✅ **Relationship management** - `AddRelationship()`, `RemoveRelationship()` unchanged  
✅ **Batch operations** - `ExecuteBatchAsync()` behavior unchanged  
✅ **Binary data handling** - `GetBinaryAsync()` unchanged  
✅ **Pagination support** - `RetrieveMultipleRecordsAsync()` and variants unchanged  

**Important:** All public API methods remain backward compatible. Existing code that calls these methods will work without changes once the DI registration is migrated.

### 🎯 Best Practices for v2.0.0

1. **Use the extension method** - Don't configure manually
2. **Leverage the built-in resilience handler** - No custom retry policies needed
3. **Define custom plural mappings** - For non-standard entity names
4. **Use structured exception properties** - For better error handling
5. **Token cache is already enabled** - By default for better performance
6. **Test rate-limiting scenarios** - Resilience handler works automatically

### ℹ️ Notes

- **v1.2.1 remains available:** Users who need .NET 8 or .NET 9 can continue to use version 1.2.1. This version will continue to be supported as a legacy version.
- **Recommended version:** For new projects and all .NET 10+ environments, version 2.0.0 is recommended.
- **Migration relatively easy:** Despite breaking changes, migration is manageable as all API methods remain unchanged - only the DI registration needs to be adapted.
- **Automatic resilience:** The integrated resilience logic makes your application more robust against rate-limiting and temporary network issues without additional code.

---

## [1.2.1] - Legacy Version

**Note:** Version 1.2.1 is the last version with .NET 8 and .NET 9 support. For details on this version, see the package documentation or Git tag `v1.2.1`.

**Supported frameworks:**
- .NET 8.0
- .NET 9.0

**Key features:**
- CRUD operations for Dynamics 365 / Dataverse
- OData query support
- Batch operations
- Relationship management
- Manual Polly integration required
- Manual DI configuration

---

[2.0.0]: https://github.com/GuedesPlace/SimpleDynamicsConnector/releases/tag/v2.0.0
[1.2.1]: https://github.com/GuedesPlace/SimpleDynamicsConnector/releases/tag/v1.2.1
