#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;

public static HttpResponseMessage Run(HttpRequestMessage req,
    TraceWriter log,
    ICollector<Subscription> outputSubscriptions)
{
    log.Info("C# HTTP trigger function processed a request.");

    var query = req.GetQueryNameValuePairs()
        .GroupBy(pair => pair.Key.ToLower())
        .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToArray());

    var gcmToken = query["gcmtoken"][0];
    var category = query["category"][0];
    var subCategory = query["subcategory"][0];
    var dateNumbers = query["date"][0].Split('/');

    throw new NotImplementedException();

    // Fetching the name from the path parameter in the request URL
    return req.CreateResponse(HttpStatusCode.OK, "Success");
}


public class Subscription : TableEntity
{
    public string GCMToken { get; set; }
    public char Category { get; set; }
    public char SubCategory { get; set; }
    public DateTime Date { get; set; }
}