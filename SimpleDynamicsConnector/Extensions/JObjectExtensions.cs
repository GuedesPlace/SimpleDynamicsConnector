using Newtonsoft.Json.Linq;

namespace GuedesPlace.SimpleDynamicsConnector.Extensions;

public static class JObjectExtensions
{
    public static void AddSingleReference(this JObject json, string field, string table, string id)
    {
        json[field + "@odata.bind"] = string.IsNullOrEmpty(id) ? null : "/" + table + "(" + id + ")";
    }

    public static JObject RemoveDirectReference(this JObject json)
    {
        JObject result = [];
        foreach (var property in json.Properties())
        {

            if (!(property.Name.StartsWith("_") && property.Name.EndsWith("_value")))
            {
                result.Add(property);
            }
        }
        return result;
    }
}