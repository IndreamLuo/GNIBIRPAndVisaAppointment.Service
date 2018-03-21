#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

public class Subscription : TableEntity
{
    public char? Type { get; set; }
    public char? Category { get; set; }
    public char? SubCategory { get; set; }
}

public class Queries
{
    public Queries(HttpRequestMessage req, TraceWriter log)
    {
        var nameValuePairs = req.GetQueryNameValuePairs();
        log.Info($"Queries({nameValuePairs.Count()})");
        Dictionary = nameValuePairs
            .GroupBy(pair => pair.Key.ToLower())
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToArray());
    }

    public readonly IDictionary<string, string[]> Dictionary;

    public string[] this[string key]
    {
        get
        {
            string[] result;

            if (!Dictionary.TryGetValue(key, out result))
            {
                result = null;
            }

            return result;
        }
        set
        {
            Dictionary[key] = value;
        }
    }
}