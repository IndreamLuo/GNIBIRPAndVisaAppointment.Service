#load "..\shared.csx"

#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;

public static HttpResponseMessage Run(HttpRequestMessage req,
    TraceWriter log,
    IQueryable<Subscription> subscriptions,
    CloudTable outputSubscriptions)
{
    log.Info("Subscribe function processed a request.");

    var query = new Queries(req);

    var gcmToken = query["gcmtoken"]?[0];
    var type = query["type"]?[0];
    var category = query["category"]?[0];
    var subCategory = query["subcategory"]?[0];
    
    var currentSubscription = subscriptions.FirstOrDefault(subscription => subscription.PartitionKey == "GCM" && subscription.RowKey == gcmToken);
    var newSubscription = currentSubscription
    ?? new Subscription
    {
        PartitionKey = "GCM",
        RowKey = gcmToken,
    };

    newSubscription.Type = type?[0];
    newSubscription.Category = category?[0];
    newSubscription.SubCategory = subCategory?[0];

    var operation = currentSubscription != null
    ? TableOperation.Replace(newSubscription)
    : TableOperation.Insert(newSubscription);

    outputSubscriptions.Execute(operation);

    // Fetching the name from the path parameter in the request URL
    return req.CreateResponse(HttpStatusCode.OK, "Success");
}

public class Queries
{
    public Queries(HttpRequestMessage req)
    {
        Dictionary = req.GetQueryNameValuePairs()
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