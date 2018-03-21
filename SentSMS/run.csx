#r "Twilio.Api"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "..\shared.csx"

using Twilio;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;

public static async Task Run(string eventMessage, IAsyncCollector<SMSMessage> messages, IQueryable<Subscription> subscriptions, TraceWriter log)
{
    log.Info($"Sent SMS function processed a request from event({eventMessage}).");

    // var tos = req.GetQueryNameValuePairs()
    //     .Where(pair => pair.Key.ToLower() == "to")
    //     .Select(pair => pair.Value)
    //     .ToArray();
    // log.Info($"To: [{string.Join(", ", tos)}]");
    // var content = await req.Content.ReadAsAsync<string>();

    // Task.WaitAll(tos.Select(to => messages.AddAsync(new SMSMessage {
    //     To = to,
    //     Body = content
    // })).ToArray());

    // log.Info("Sent SMS function finished.");
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}
