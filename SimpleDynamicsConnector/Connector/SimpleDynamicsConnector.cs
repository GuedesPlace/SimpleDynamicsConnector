using Microsoft.Identity.Client;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using GuedesPlace.SimpleDynamicsConnector.Models;
using Microsoft.Extensions.Options;

namespace GuedesPlace.SimpleDynamicsConnector;

public class SimpleDynamicsConnector
{
    public static readonly string MEDIAJSON = "application/json";
    public static readonly string APIPATH = "/api/data/v9.2/";
    private readonly IConfidentialClientApplication _clientAuthApp;
    private readonly HttpClient _client;
    private readonly DynamicsConnectionConfiguration _configuration;
    public SimpleDynamicsConnector(HttpClient client, IOptions<DynamicsConnectionConfiguration> configuration)
    {
        _configuration = configuration.Value;
        string authority = $"https://login.microsoftonline.com/{_configuration.TenantId}";

        _clientAuthApp = ConfidentialClientApplicationBuilder.Create(_configuration.ApplicationId).WithClientSecret(_configuration.ApplicationSecret).WithAuthority(authority).Build();
        _client = client;
        _client.BaseAddress = new Uri(_configuration.CrmUrl + APIPATH);
        _client.DefaultRequestHeaders.Add("Prefer", "odata.include-annotations=\"*\"");
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
        _client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
        _client.DefaultRequestHeaders.Add("OData-Version", "4.0");
    }

    public HttpClient GetClient()
    {
        return _client;
    }
    public string GetFullUrl()
    {
        return _client.BaseAddress!.AbsoluteUri;
    }

    public async Task<HttpRequestMessage> BuildRequestMessageAsync(HttpMethod method, string path) {
        var request = new HttpRequestMessage(method,path);
        var authResult = await _clientAuthApp.AcquireTokenForClient([$"{_configuration.CrmUrl}/.default"]).ExecuteAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
        return request;
    }

    public async Task<Guid> CreateRecordAsync(string entityName, object payload)
    {
        string payloadAsString = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        StringContent content = new StringContent(payloadAsString, Encoding.UTF8, MEDIAJSON);
        var path = BuildPluralNameForEntity(entityName);
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Post, path);
        request.Content = content;
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            HttpResponseHeaders headers = response.Headers;
            if (headers.TryGetValues("OData-EntityId", out IEnumerable<string>? values))
            {
                return new Guid(values.First<string>().Split('(')[1].Split(')')[0]);
            }
            throw await BuildException("POST", path, response, payloadAsString, "NO OData-EntityId in Header found!");
        }
        else
        {
            throw await BuildException("POST", path, response, payloadAsString);
        }
    }
    public async Task UpdateRecordAsync(string entityName, Guid id, object payload)
    {
        string payloadAsString = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        StringContent content = new StringContent(payloadAsString, Encoding.UTF8, MEDIAJSON);
        var path = $"{BuildPluralNameForEntity(entityName)}({id})";
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Patch, path);
        request.Content = content;
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("PATCH", path, response, payloadAsString);
        }
    }

    public async Task DeleteRecordAsync(string entityName, Guid id)
    {
        var path = $"{BuildPluralNameForEntity(entityName)}({id})";
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Delete, path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("DELETE", path, response, "");
        }
    }
    public async Task<T?> RetrieveRecordAsync<T>(string entityName, Guid id, string options = "")
    {
        var path = $"{BuildPluralNameForEntity(entityName)}({id})" + options;
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Get, path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("GET", path, response, "");
        }
        return await BuildResultObject<T>(response);
    }

    public async Task<MultipleRecordsResponse<T>?> RetrieveMultipleRecordsAsync<T>(string entityName, string options = "", int maxPageSize = 5000)
    {
        var path = BuildPluralNameForEntity(entityName) + options;
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Get, path);
        request.Headers.Add("Prefer", $"odata.maxpagesize={maxPageSize},odata.include-annotations=\"*\"");
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("GET", path, response, "");
        }
        return await BuildResultObject<MultipleRecordsResponse<T>>(response);
    }

    public async Task<ICollection<T>> RetrieveAllMultipleRecordsAsync<T>(string entityName, string options, int maxPageSize = 5000)
    {
        var path = BuildPluralNameForEntity(entityName) + options;
        return await GetAllAsync<T>(path, maxPageSize);
    }
    public async Task<ICollection<T>> GetAllAsync<T>(string path, int pagsize = 5000)
    {
        IEnumerable<T> result = [];
        string? pathToProcess = path;
        while (pathToProcess != null)
        {
            var response = await GetAsync<MultipleRecordsResponse<T>>(pathToProcess, pagsize);
            pathToProcess = null;
            if (response != null)
            {
                if (response.Entities != null)
                {
                    result = result.Concat(response.Entities);
                }
                if (!string.IsNullOrEmpty(response.NextLink))
                {
                    pathToProcess = response.NextLink;
                }
            }
        }
        return [.. result];
    }

    public async Task<T?> GetAsync<T>(string path)
    {
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Get, path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("GET", path, response, "");
        }
        return await BuildResultObject<T>(response);
    }
    public async Task<T?> GetAsync<T>(string path, int maxPageSize)
    {
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Get, path);
        request.Headers.Add("Prefer", $"odata.maxpagesize={maxPageSize},odata.include-annotations=\"*\"");
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("GET", path, response, "");
        }
        return await BuildResultObject<T>(response);
    }

    public async Task<Stream> GetBinaryAsync(string path)
    {
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Get, path);
        using HttpResponseMessage response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var memoryStream = new MemoryStream();
        var httpsContentStream = await response.Content.ReadAsStreamAsync();
        await httpsContentStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        return memoryStream;
    }
    public async Task<T?> PostAsync<T>(string path, object postData)
    {
        string payload = JsonConvert.SerializeObject(postData, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
        StringContent content = new StringContent(payload, Encoding.UTF8, MEDIAJSON);
        using HttpRequestMessage request = await BuildRequestMessageAsync(HttpMethod.Post, path);
        request.Content = content;
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw await BuildException("POST", path, response, payload);
        }
        return await BuildResultObject<T>(response);
    }

    private static async Task<T?> BuildResultObject<T>(HttpResponseMessage response)
    {
        string result = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(result);
    }

    public async Task<string> ExecuteBatchAsync(ICollection<BatchInstruction> instructions)
    {
        using HttpRequestMessage request =await BuildRequestMessageAsync(HttpMethod.Post, "$batch");
        var batchToken = $"--batch_{DateTime.Now.ToString("yyyyMMddHMMss")}";

        var batchPayloadElements = instructions.Select(instruction =>
        {
            var jsonPayload = instruction.Payload.ToString(Formatting.None);
            return $@"Content-Type: application/http
Content-Transfer-Encoding: binary

{instruction.Command} {instruction.UrlSegment} HTTP/1.1
Accept: application/json
Content-Type: application/json;type=entry

{jsonPayload}";
        });
        var batchHeader = BuildBatchMediaType(batchToken.Substring(2));
        var batchPayload = batchToken + "\r\n" + string.Join(batchToken + "\r\n", batchPayloadElements) + $"{batchToken}--\r\n";
        StringContent content = new StringContent(batchPayload, Encoding.UTF8);
        content.Headers.ContentType = batchHeader;
        request.Content = content;
        using HttpResponseMessage response = await _client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }
        throw await BuildException("POST", "batch$", response, batchPayload);

    }

    private static async Task<Exception> BuildException(string methode, string path, HttpResponseMessage response, string payload, string? optionalMessage = null)
    {
        string errorContent = await response.Content.ReadAsStringAsync();
        var msg = string.IsNullOrEmpty(optionalMessage) ? "" : optionalMessage;
        throw new Exception($"Error {msg} during {methode} - Path: {path} stateCode: {response.StatusCode}  message: {response.ReasonPhrase} errorContent: {errorContent} |Data: {payload}");
    }
    private static MediaTypeHeaderValue BuildBatchMediaType(string batchIdentifier)
    {
        var header = new MediaTypeHeaderValue("multipart/mixed");
        header.Parameters.Add(
          new NameValueHeaderValue(
            "boundary",
            batchIdentifier
          ));
        return header;
    }
    private string BuildPluralNameForEntity(string entityName)
    {
        if (_configuration.CustomTablePluralMapping.ContainsKey(entityName))
        {
            return _configuration.CustomTablePluralMapping[entityName];
        }
        return (entityName.EndsWith("ch") || entityName.EndsWith("s") || entityName.EndsWith("sh") || entityName.EndsWith("x") || entityName.EndsWith("z")) ?
               entityName + "es" :
               entityName.EndsWith("y") ? entityName.Substring(0, entityName.Length - 1) + "ies" :
               entityName.EndsWith("f") ? entityName.Substring(0, entityName.Length - 1) + "ves" :
               entityName + "s";
    }
}