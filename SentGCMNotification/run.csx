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

    var parameters = new Queue<string>(eventMessage.Split(new [] { "\r\n", "\n" }, StringSplitOptions.None));

    using (var client = new HttpClient())
    {
        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", GetEnvironmentVariable("GCMServiceKey"));
        
        var data = new Dictionary<string, string>();
        var to = string.Empty;

        while (parameters.Any())
        {
            var key = parameters.Dequeue();
            var value = parameters.Dequeue();
            
            if (key == "gcmToken")
            {
                to = value;
            }
            else
            {
                data[key] = value;
            }
        }

        if (!data.ContainsKey("_timestamp"))
        {
            data["_timestamp"] = DateTime.UtcNow.Ticks.ToString();
        }

        var dataString = JsonConvert.SerializeObject(new {
            data = data,
            to = to
        });
        var content = new StringContent(dataString, Encoding.Default, "application/json");

        log.Info("GCM Post Start");

        var response = await client.PostAsync("https://gcm-http.googleapis.com/gcm/send", content);

        log.Info("GCM Post Status: " + response.StatusCode);
        log.Info(await response.Content.ReadAsStringAsync());
    }
    
    log.Info("Sent GCM Downstream Message function ends.");
}
