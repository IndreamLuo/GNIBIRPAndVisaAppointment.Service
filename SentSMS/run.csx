#r "Twilio.Api"
#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"

#load "..\shared.csx"

using Twilio;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

using System.Net;

public static async Task Run(string eventMessage, IAsyncCollector<SMSMessage> messages, TraceWriter log)
{
    log.Info($"Sent SMS function processed a request from event({eventMessage}).");

    var parameters = eventMessage.Split('\n');
    var message = parameters[0];
    var phone = parameters[1];
    
    messages.AddAsync(new SMSMessage {
        To = phone,
        Body = message
    });

    log.Info("Sent SMS function finished.");
}

public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}
