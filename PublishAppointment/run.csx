#load "..\shared.csx"

using System.Net;

public static async Task Run(string eventMessage,
    TraceWriter log,
    IQueryable<Subscription> subscriptions,
    ICollector<string> gcmNotificationEvents,
    ICollector<string> smsEvents)
{
    log.Info($"C# HTTP trigger function processed a request from event({eventMessage}).");

    var parameters = eventMessage.Split('-');
    var type = parameters[0];
    var category = parameters[1];
    var subCategory = parameters[2];
    var time = parameters[3];
    var expiration = parameters[4];

    log.Info("Event Message deserialized.");

    var validSubscriptions = subscriptions
        .ToArray()
        .Where(subscription => (subscription.Category == null || subscription.Category == category[0])
            && (subscription.SubCategory == null || subscription.SubCategory == subCategory[0]));
    
    log.Info("Sending SMS");

    var smses = validSubscriptions.Where(subscription => subscription.PartitionKey == "SMS");

    log.Info($"SMS subscriptions got({smses.Count()}).");

    foreach (var sms in smses)
    {
        var smsEvent = $"{time} - {type}/{category}/{subCategory}\n"
            + sms.RowKey;

        log.Info($"SMS sending: ({smsEvent})");

        smsEvents.Add(smsEvent);

        log.Info($"SMS sent: ({smsEvent})");
    }

    log.Info("Sending GCM");

    var gcms = validSubscriptions.Where(subscription => subscription.PartitionKey == "GCM");

    log.Info($"GCM subscriptions got({gcms.Count()}).");

    var gcmMessage = $"New Valid [{type}] Appointment\n"
        + $"{category}-{subCategory}: {time}{(expiration == string.Empty ? string.Empty : "-")}{expiration}\n";
    foreach (var gcm in gcms)
    {
        var gcmEvent = $"{gcmMessage}{gcm.RowKey}";

        log.Info($"GCM sending: ({gcmEvent})");

        gcmNotificationEvents.Add(gcmEvent);

        log.Info($"GCM sent: ({gcmEvent})");
    }

    log.Info("Finished.");
}
