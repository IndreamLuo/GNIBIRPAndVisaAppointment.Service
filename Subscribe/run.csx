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

    var request = req.Content.ReadAsAsync<dynamic>().Result;
    log.Info("Request queries organized.");

    string gcmToken = request.gcmToken;
    log.Info($"GCM Token({gcmToken})");
    string type = request.type;
    log.Info($"Type({type})");
    string category = request.category;
    log.Info($"Category({category})");
    string subCategory = request.subCategory;
    log.Info($"Sub Category({subCategory})");
    
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