#load "..\shared.csx"

using System.Net;

public static async Task Run(string eventMessage,
    TraceWriter log,
    IQueryable<Subscription> subscriptions,
    ICollector<string> gcmNotificationEvents,
    ICollector<string> smsEvents)
{
    log.Info($"C# HTTP trigger function processed a request from event({eventMessage}).");

    var parameters = eventMessage.Split('^');
    var type = parameters[0];
    var category = parameters[1];
    var subCategory = parameters[2];
    DateTime parseTime;
    DateTime? time = null;
    if (DateTime.TryParse(parameters[3], out parseTime))
    {
        time = parseTime;
    }
    DateTime? expiration = null;
    if (DateTime.TryParse(parameters[4], out parseTime))
    {
        expiration = parseTime;
    }

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

        log.Info($"SMS sent.");
    }

    log.Info("Sending GCM");

    var gcms = validSubscriptions.Where(subscription => subscription.PartitionKey == "GCM");

    log.Info($"GCM subscriptions got({gcms.Count()}).");

    var timeFormat = "dd MMMM yyyy HH:mm";

    var gcmMessage = $"type\n{type}\n"
        + $"category\n{category}\n"
        + $"subCategory\n{subCategory}\n"
        + $"time\n{time}\n"
        + $"expiration\n{expiration}\n"
        + $"title\nNew Valid [{type}] Appointment\n"
        + $"message\n{category}-{subCategory}: {(time.HasValue ? time.Value.ToString(timeFormat) : string.Empty)}{(expiration == null ? string.Empty : "-")}{(expiration.HasValue ? expiration.Value.ToString(timeFormat) : string.Empty)}\n";
    foreach (var gcm in gcms)
    {
        var gcmEvent = $"{gcmMessage}gcmToken\n{gcm.RowKey}";

        log.Info($"GCM sending: ({gcmEvent})");

        gcmNotificationEvents.Add(gcmEvent);

        log.Info($"GCM sent.");
    }

    log.Info("Finished.");
}
