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
    string irpCategory = request.irpCategory;
    log.Info($"IRP Category({irpCategory})");
    string irpSubCategory = request.irpSubCategory;
    log.Info($"IRP Sub Category({irpSubCategory})");
    string visaCategory = request.visaCategory;
    log.Info($"Visa Category({visaCategory})");
    
    var currentSubscription = subscriptions.Where(subscription => subscription.PartitionKey == "GCM" && subscription.RowKey == gcmToken).FirstOrDefault();
    var newSubscription = currentSubscription
    ?? new Subscription
    {
        PartitionKey = "GCM",
        RowKey = gcmToken,
    };

    newSubscription.IRPCategory = irpCategory;
    newSubscription.IRPSubCategory = irpSubCategory;
    newSubscription.VisaCategory = visaCategory;

    var operation = currentSubscription != null
    ? TableOperation.Replace(newSubscription)
    : TableOperation.Insert(newSubscription);

    outputSubscriptions.Execute(operation);

    // Fetching the name from the path parameter in the request URL
    return req.CreateResponse(HttpStatusCode.OK, "Success");
}