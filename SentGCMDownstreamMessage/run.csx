#r "Newtonsoft.Json"

using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.Text;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    log.Info("Sent GCM Downstream Message function starts.");

    var query = req.GetQueryNameValuePairs()
        .GroupBy(pair => pair.Key.ToLower())
        .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToArray());

    var title = query["title"][0];
    log.Info("Title: " + title);
    var information = query["information"][0];
    log.Info("Information: " + title);
    var tos = query["to"];
    log.Info("To: [" + "]");
    var result = await SentGCMDownstreamMessage(title, information, tos, log);

    log.Info("Sent GCM Downstream Message function ends.");
    return result
    ? req.CreateResponse(HttpStatusCode.OK, "Sent.")
    : req.CreateResponse(HttpStatusCode.NotAcceptable, "Sending failed.");
}

public static async Task<bool> SentGCMDownstreamMessage(string title, string information, string[] tos, TraceWriter log)
{
    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", GetEnvironmentVariable("GCMServiceKey"));
        var data = new
        {
            data = new
            {
                title = title,
                information = information
            },
            to = tos
        };
        var dataString = JsonConvert.SerializeObject(data);
        var content = new StringContent(dataString, Encoding.Default, "application/json");

        log.Info("GCM Post Start");

        var response = await client.PostAsync("https://gcm-http.googleapis.com/gcm/send", content);

        log.Info("GCM Post Status: " + response.StatusCode);

        return response.StatusCode == HttpStatusCode.OK;
    }
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}