using Newtonsoft.Json.Linq;

namespace GuedesPlace.SimpleDynamicsConnector.Models;
public class BatchInstruction
{
    public string Command { set; get; } = string.Empty;
    public string UrlSegment { set; get; } = string.Empty;
    public JObject Payload { set; get; } = new JObject();
}