namespace GuedesPlace.SimpleDynamicsConnector.Models;
public class DynamicsConnectionConfiguration
{
    required public string CrmUrl { set; get; }
    required public string TenantId { set; get; }
    required public string ApplicationId { set; get; }
    required public string ApplicationSecret { set; get; }
    public Dictionary<string,string> CustomTablePluralMapping { set; get; } = new Dictionary<string,string>();
}