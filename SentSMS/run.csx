#r "Twilio.Api"

using Twilio;

using System.Net;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, IAsyncCollector<SMSMessage> messages, TraceWriter log)
{
    log.Info("Sent SMS function processed a request.");

    var tos = req.GetQueryNameValuePairs()
        .Where(pair => pair.Key.ToLower() == "to")
        .Select(pair => pair.Value)
        .ToArray();
    log.Info($"To: [{string.Join(", ", tos)}]");
    var content = await req.Content.ReadAsAsync<string>();

    Task.WaitAll(tos.Select(to => messages.AddAsync(new SMSMessage {
        To = to,
        Body = content
    })).ToArray());

    log.Info("Sent SMS function finished.");

    return req.CreateResponse(HttpStatusCode.OK, "Sent.");
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}
