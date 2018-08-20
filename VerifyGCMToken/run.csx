#load "..\shared.csx"

#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

using System.Http;
using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<Subscription> subscriptions, TraceWriter log)
{
    var gcmToken = await req.Content.ReadAsAsync<string>();

    log.Info($"Verifying: {gcmToken}");

    var subscription = subscriptions.FirstOrDefault(item => item.PartitionKey == "GCM" && item.RowKey == gcmToken);

    if (subscription != null)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", GetEnvironmentVariable("GCMServiceKey"));

            var dataString = JsonConvert.SerializeObject(new {
                data = new Dictionary<string, string>
                {
                    ["Hello"] = "world!" 
                },
                to = gcmToken,
                dry_run = true
            });
            var content = new StringContent(dataString, Encoding.Default, "application/json");

            log.Info("GCM Post Start");

            var response = await client.PostAsync("https://gcm-http.googleapis.com/gcm/send", content);
            var responseResultContent = await response.Content.ReadAsStringAsync();

            log.Info("GCM Post Status: " + response.StatusCode);
            log.Info(responseResultContent);

            var responseResult = JsonConvert.DeserializeObject<dynamic>(responseResultContent);
            if (responseResult.success > 0)
            {
                return req.CreateResponse<bool>(HttpStatusCode.OK, true);
            }
        }
    }

    return req.CreateResponse<bool>(HttpStatusCode.OK, false);
}
