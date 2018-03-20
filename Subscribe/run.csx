using System.Net;

public static HttpResponseMessage Run(HttpRequestMessage req,
    IEnumerable<dynamic> currentSubscriptions,
    out dynamic newSubscription,
    TraceWriter log)
{
    log.Info("Subscribe function processed a request.");

    var queries = req.GetQueryNameValuePairs()
        .GroupBy(pair => pair.Key.ToLower())
        .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToArray());
    
    log.Info("Http queries grouped");
    var subscription = null;

    newSubscription = null;
    // Fetching the name from the path parameter in the request URL
    return req.CreateResponse(HttpStatusCode.OK, "OK");
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}