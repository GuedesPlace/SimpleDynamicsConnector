using Newtonsoft.Json;

namespace GuedesPlace.SimpleDynamicsConnector.Models;
public class MultipleRecordsResponse<T>
{
    [JsonProperty("value")]
    public ICollection<T>? Entities { get; set; }
    [JsonProperty("@odata.nextLink")]
    public string? NextLink { get; set; }

}