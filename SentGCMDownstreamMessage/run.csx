#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;
using System.Net.Http;
using System.Text;

public static async Task Run(string eventMessage, IQueryable<Subscription> subscriptions, TraceWriter log)
{
    log.Info($"Sent GCM Downstream Message function starts for event({eventMessage}).");

    var parameters = eventMessage.Split('/', ' ', '-');
    var type = parameters[0];
    var category = parameters[1];
    var subCategory = parameters[2];
    var time = parameters[3];
    var expiration = parameters[4];

    var title = $"New Valid Appointment";
    log.Info($"Title: ({title})");
    var information = $"{category}{(subCategory != string.Empty ? "/" : string.Empty)}{subCategory}:{time}{(expiration == string.Empty ? "-" : string.Empty)}{expiration}";
    log.Info("Information: " + title);
    var tos = subscriptions
        .Where(subscription => (category == null || subscription.Category == category[0] || subscription.Category == null)
            && (subCategory == null || subscription.SubCategory == subCategory[0] || subscription.SubCategory == null))
        .Select(subscription => subscription.RowKey)
        .ToArray();
    log.Info($"To: [{string.Join(", ", tos)}]");
    var result = await SentGCMDownstreamMessage(title, information, tos, log);
    
    log.Info("Sent GCM Downstream Message function ends.");
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


public class Subscription : TableEntity
{
    public char? Type { get; set; }
    public char? Category { get; set; }
    public char? SubCategory { get; set; }
}