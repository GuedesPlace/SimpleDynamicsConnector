# GitHub Copilot Instructions for SimpleDynamicsConnector

## Project Overview
SimpleDynamicsConnector is a lightweight .NET library for interacting with Microsoft Dynamics 365 / Dataverse API. It provides an injectable HTTP client-based connector inspired by Microsoft's XRM.webApi, enabling intuitive CRUD operations with minimal boilerplate.

## Target Framework & Version
- **Current Version**: 2.0.0
- **Target Framework**: .NET 10
- **Legacy Support**: Version 1.2.1 supports .NET 8 and .NET 9

## Core Architecture Principles

### Dependency Injection Pattern
- Use constructor injection for all services
- Leverage `IOptions<T>` pattern for configuration
- Register services via extension methods on `IServiceCollection`
- Use keyed services for specialized dependencies (e.g., `IConfidentialClientApplication`)

### HttpClient Configuration
- Always use typed HttpClient pattern (`AddHttpClient<TClient>`)
- Configure base address and headers in service registration, not in constructors
- Include built-in resilience handlers for transient fault handling

### Authentication
- Use `IConfidentialClientApplication` from Microsoft.Identity.Client
- Token acquisition with retry logic for resilience
- Cache tokens using `EnableSharedCacheOptions`
- Never expose credentials in logs or exceptions

## Coding Standards

### Naming Conventions
- **Entity Methods**: Follow XRM.webApi naming (e.g., `CreateRecordAsync`, `RetrieveRecordAsync`, `UpdateRecordAsync`, `DeleteRecordAsync`)
- **Entity Names**: Use lowercase logical names (e.g., "account", "contact")
- **Query Parameters**: Support OData query strings with `?` prefix (e.g., `?$select=name,address1_city`)
- **Pluralization**: Implement `BuildPluralNameForEntity()` with custom mapping support

### Async/Await Patterns
- All I/O operations must be async (suffix with `Async`)
- Use `ConfigureAwait(false)` is NOT required (modern .NET recommendation)
- Return `Task<T>` or `Task` for all async methods
- Use `IAsyncEnumerable<T>` for streaming large datasets when appropriate

### Error Handling
- Use descriptive exception messages including method, path, status code, and payload
- Include server error content in exceptions for debugging
- Handle rate limiting (429) with RetryAfter header respect
- Log retry attempts for observability

### Resilience Strategy (.NET 10+)
- Use `Microsoft.Extensions.Http.Resilience` (Polly V8)
- **Primary Strategy**: Respect `RetryAfter` header from Dynamics/Dataverse (429 responses)
- **Fallback**: Exponential backoff with jitter for other transient errors
- **Retry Count**: 3 attempts maximum
- **Timeout**: 30 seconds per request (Dynamics can be slow)
- **Handled Errors**: 408, 429, 500-504, network failures
- **RetryAfter Cap**: Maximum 60 seconds wait time

### JSON Serialization
- Use Newtonsoft.Json (project standard)
- Apply `NullValueHandling.Ignore` for serialization
- Support both JObject and POCO payloads
- Deserialize responses to strongly-typed models when possible

### Testing Considerations
- Mock `HttpClient` using `IHttpClientFactory` and test handlers
- Mock `IConfidentialClientApplication` for authentication tests
- Test retry logic with various HTTP status codes
- Validate OData query string construction

## Key Components

### SimpleDynamicsConnector Class
Main connector class with:
- CRUD operations: Create, Retrieve, Update, Delete
- Batch operations: `ExecuteBatchAsync`
- Relationship management: `AddRelationship`, `RemoveRelationship`
- Custom queries: `GetAsync<T>`, `PostAsync<T>`
- Binary data: `GetBinaryAsync`
- Paging support: `RetrieveMultipleRecordsAsync`, `RetrieveAllMultipleRecordsAsync`

### ServiceCollectionExtensions
Extension methods for DI registration:
- `AddSimpleDynamicsConnector()` - Main registration method
- Configures HttpClient with headers and base address
- Registers `DynamicsConnectionConfiguration` via `IOptions<T>`
- Registers `IConfidentialClientApplication` as keyed singleton
- Applies resilience handler with RetryAfter support

### DynamicsConnectionConfiguration
Configuration model containing:
- `CrmUrl`: Base URL for Dynamics/Dataverse instance
- `TenantId`: Azure AD tenant identifier
- `ApplicationId`: Azure AD application (client) ID
- `ApplicationSecret`: Azure AD application secret
- `CustomTablePluralMapping`: Dictionary for custom entity plural names

### Models
- `EntityReference`: Represents a Dynamics entity reference with LogicalName and Id
- `MultipleRecordsResponse<T>`: Container for paged query results with NextLink
- `BatchInstruction`: Payload structure for batch operations

## Common Patterns

### Entity Pluralization
```csharp
// Automatically handles pluralization rules
// account → accounts
// activity → activities
// address → addresses
// Use CustomTablePluralMapping for exceptions
```

### OData Query Construction
```csharp
// Always include ? prefix
var options = "?$select=name,address1_city&$filter=statecode eq 0";
// For expand with select
var options = "?$expand=primarycontactid($select=fullname)";
```

### Relationship Operations
```csharp
// Many-to-many relationships use special methods
// GetM2NChildrenWithAllColumns for reading
// AddRelationship for associating
// RemoveRelationship for disassociating
```

## Important Notes for Copilot

1. **Never suggest manual HttpClient instantiation** - always use DI
2. **Never hardcode credentials** - use configuration
3. **Always respect RetryAfter headers** - built into resilience handler
4. **Use appropriate timeouts** - Dynamics operations can be slow
5. **Support custom plural mappings** - not all entities follow standard rules
6. **Include proper error context** - method, path, payload, status code
7. **Version awareness** - v2.0.0+ has breaking changes from v1.x
8. **Legacy compatibility** - document differences between versions

## Dependencies
- Microsoft.Extensions.DependencyInjection (10.0.0)
- Microsoft.Extensions.Http (10.0.0)
- Microsoft.Extensions.Http.Resilience (10.0.0)
- Microsoft.Extensions.Options (10.0.0)
- Microsoft.Identity.Client (4.67.0+)
- Newtonsoft.Json (13.0.3+)

## Documentation Standards
- Include XML documentation comments for public APIs
- Provide usage examples in README
- Document breaking changes in version updates
- Include parameter descriptions for extension methods
- Note version-specific features clearly

## Security Guidelines
- Never log sensitive data (tokens, secrets, credentials)
- Use secure string handling for secrets
- Validate input parameters (null/empty checks)
- Sanitize data in exception messages
- Follow least privilege principle for Azure AD app permissions
