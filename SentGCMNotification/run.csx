#load "..\shared.csx"

#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;
using System.Net.Http;
using System.Text;

public static async Task Run(string eventMessage, TraceWriter log)
{
    log.Info($"Sent GCM Notification function starts for event({eventMessage}).");

    var parameters = eventMessage.Split('\n');

    var title = parameters[0];
    log.Info($"Title: {title}");
    var information = parameters[1];
    log.Info("Information: " + information);
    var to = parameters[2];
    log.Info($"To: [{string.Join(", ", to)}]");
    var result = await SentGCMDownstreamMessage(title, information, to, log);
    
    log.Info("Sent GCM Downstream Message function ends.");
}

public static async Task<bool> SentGCMDownstreamMessage(string title, string information, string to, TraceWriter log)
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
            to = to
        };
        var dataString = JsonConvert.SerializeObject(data);
        var content = new StringContent(dataString, Encoding.Default, "application/json");

        log.Info("GCM Post Start");

        var response = await client.PostAsync("https://gcm-http.googleapis.com/gcm/send", content);

        log.Info("GCM Post Status: " + response.StatusCode);
        log.Info(await response.Content.ReadAsStringAsync());

        return response.StatusCode == HttpStatusCode.OK;
    }
}


public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}