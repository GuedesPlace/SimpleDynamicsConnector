# SimpleDynamicsConnector

Inspired by WebGates [DynamicsConnector](https://github.com/WebGateConsultingAG/DynamicsConnector) the SimpleDynamicsConnector is a lightweight implementation, following the naming and attributes of Microsofts XRM.webApi. The main goal is to provide an connector, which is injectable in any service, to do the main CRUD operations.
As the call signature is comparable to Microsofts XRM.webApi, the query can be exchange between the two frameworks. This should help to establish a common conversation on the same calls and topics.

As we are talking Dynamics, we could also say Dataverse.

# How to use

## Prepare to use as Connector
The SimpleDynamicsConnector is as a HttpClient defined. The following example show how to define the DynamicsConnectionConfiguration (as IOptions) and wrap the client with Polly to extend retry capabilities.

Add the following stuff to the Program.cs (Adapt if your are using IHostBuilder instead)

```c#
//Builder can be any Type of IHostApplicationBuilder
var builder = FunctionsApplication.CreateBuilder(args);

var applicationID = builder.Configuration["crmApplicationId"];
var applicationSecret = builder.Configuration["crmApplicationSecret"];
var tenantId = builder.Configuration["tenantId"];
var crmUrl = builder.Configuration["crmUrl"];

//Initializing DynamicsConnectionConfiguration
builder.Services.AddOptions<DynamicsConnectionConfiguration>().Configure(c =>
{
    c.ApplicationId = applicationID;
    c.ApplicationSecret = applicationSecret;
    c.CrmUrl = crmUrl;
    c.TenantId = tenantId;

});
//Adding the SimpleDynamicsConnetor as HTTP Client and Wrapping with retry Policy
builder.Services.AddHttpClient<SimpleDynamicsConnector>().AddPolicyHandler(Policy.HandleResult<HttpResponseMessage>
        (msg => msg.Headers.RetryAfter is not null)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (_, response, _) => response.Result.Headers.RetryAfter.Delta.Value,
            onRetryAsync: (_, _, _, _) => Task.CompletedTask
        ));
```
The SimpleDynamicsConnector can now be injected in any Service.

## Using SimpleDynamicsConnector
For the samples we are talking about Accounts in Dynamics. Assume that you have defined an Account POCO that looks a bit like this:
```c#
namespace Example.DynamicsModel;

class Account {
    public string accountid {set;get;}
    public string name {set;get;}
    public string address1_line1 {set;get;}
    public string address1_postalcode {set;get;}
    public string address1_city {set;get;}
}
```
### CreateRecordAsync(string entityName, object payload)
The function CreateRecordAsync(string entityName, object payload) creates a new entry in Dynamics / Dataverse and returns the GUID of the created record.
```C#
var payload = new JObject() {
    ["name"] = "ACME Company",
    ["address1_line1"] = "Paradeplatz",
    ["address1_postalcode"] = "8000",
    ["address1_city"] = "Zurich"
};
var id = await _simpleDynamicsConnector.CreateRecordAsync("account",payload);
console.log($"Record with Id:{id} created.");
```
This code creates the ACME Company in Zurich, Paradeplatz. A beautiful place (but very expensive).  
*Sidenote: We are using Newtonsofts JObject in this sample, but any Poco should work, as long it generates the correct properties when serialized to Json*

### RetrieveRecordAsync\<T\>(string entityNamen, Guid id, string options="")
Lets retrieve the data for a company by a given Dynamics ID.
```C#
var id = new Guid(myIdAsString);
var account = await _simpleDynamicsConnector.RetrieveRecordAsync<Account>("account",id);
console.log($"Name; {account.name}, City: {account.address1_city}");
```
With the optional options is it possible to use all the capabilities of Dataverse API, like "?$select=name,address1_city". Be aware that you have to include the ? sign in the options statement.

### UpdateRecordAsync(string entityNamen, Guid id, object payload)
Lets relocate the ACME Company to Effretikon. We still have the given Dynamics ID. We will do it again with a JObject.
```C#
var id = new Guid(myIdAsString);
var payload = new JObject() {
    ["address1_line1"] = "Industriestrasse 3",
    ["address1_postalcode"] = "8307",
    ["address1_city"] = "Effretikon"
};
await _simpleDynamicsConnector.UpdateRecordAsync("account",id, payload);
console.log($"Update executed");
```

### RetrieveMultipleRecordsAsync\<T\>(string entityNamen, string options, int pagesize=5000)
What other companies are also allocated under the postal code 8307. Lets find them.
```C#
var query = "'$filter=address1_postalcode eq '8307'";
var result = await _simpleDynamicsConnector.RetrieveMultipleRecordsAsync<Account>("account",query);
console.log($"{result?.Entities?.length} Companies found!");
```
The result object is a MultipleRecordsResponse:
```C#
public class MultipleRecordsResponse<T>
{
    public ICollection<T>? Entities { get; set; }
    public string? NextLink { get; set; }
}
```
The property NextLink is filled with an URL for the next set of results and can be accessed via the Function _simpleDynamicsConnect.GetAsync\<MultipleRecordsResponse\<T\>\>(pathToProcess, pagsize);

### RetrieveAllMultipleRecordsAsync\<T\>(string entityNamen, string options, int pagesize=5000)
This is a wrapper functionality to get all Results for a given call, without any paging behavior. The result is a IEnumerable of the expected Type T
```C#
var query = "'$filter=address1_postalcode eq '8307'";
var result = await _simpleDynamicsConnector.RetrieveMultipleRecordsAsync<Account>("account",query);
console.log($"{result?.length} Companies found!");
```
The result object is a MultipleRecordsResponse:
```C#
public class MultipleRecordsResponse<T>
{
    public ICollection<T>? Entities { get; set; }
    public string? NextLink { get; set; }
}
```

### RetrieveAllMultipleRecordsAsync\<T\>(string entityNamen, string options, int pagesize=5000)
This is a wrapper functionality to get all Results for a given call, without any paging behavior. The result is a IEnumerable of the expected Type T
```C#
var query = "'$filter=address1_postalcode eq '8307'";
var result = await _simpleDynamicsConnector.RetrieveMultipleRecordsAsync<Account>("account",query);
console.log($"{result?.length} Companies found!");
```
The result object is a MultipleRecordsResponse:
```C#
public class MultipleRecordsResponse<T>
{
    public ICollection<T>? Entities { get; set; }
    public string? NextLink { get; set; }
}
```

### DeleteRecordAsync(string entityNamen, Guid id)
As the name says. This let you delete a record.
```C#
var id = new Guid(myIdAsString);
await _simpleDynamicsConnector.DeleteRecordAsync("account",id);
console.log($"Delete executed");
```


## Advanced UseCases
The following functions are implemented to support advanced UseCases. They require a good understanding of the Dataverse and Dynamics API and goes beyond typical CRUD functions.

### GetAsync<T>(string path)
The submitted path will be used and executed. The result will be serialized to the specified type.
### GetAsync<T>(string path, int pagesize)
The submitted path will be used and executed. The result will be serialized to the specified type.  
Additionally a header for the max count of returning entities is added.
### GetBinaryAsync(string path)
The submitted path will be used and executed. The result will be returned as Stream. This function can be used to grab entity images or attached files.
### PostAsync<T>(string path, object postData)
The submitted path will be used to post the postData as serialized Json payload. The result will be serialized to the specified type. This function can be used to execute Actions and Functions.

# LICENSE
The project is under [Apache V2.0](LICENSE)  
(c) 2025 by Christian Güdemann aka GuedeByte