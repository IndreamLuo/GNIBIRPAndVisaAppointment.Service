#load "..\shared.csx"

using System.Net;

public static async Task Run(string eventMessage,
    TraceWriter log,
    IQueryable<Subscription> subscriptions,
    ICollector<string> gcmNotificationEvents,
    ICollector<string> smsEvents)
{
    log.Info($"C# HTTP trigger function processed a request from event({eventMessage}).");

    var parameters = eventMessage.Split('/', ' ', '-');
    var type = parameters[0];
    var category = parameters[1];
    var subCategory = parameters[2];
    var time = parameters[3];
    var expiration = parameters[4];

    log.Info("Event Message deserialized.");

    var validSubscriptions = subscriptions
        .Where(subscription => (subscription.Category == null || subscription.Category == category[0])
            && (subscription.SubCategory == null || subscription.SubCategory == subCategory[0]));
    
    log.Info($"Valid subscriptions({validSubscriptions.Count()})");

    var smses = validSubscriptions.Where(subscription => subscription.PartitionKey == "SMS");
    foreach (var sms in smses)
    {
        var smsEvent = $"{time} - {type}/{category}/{subCategory}\n"
            + sms.RowKey;
        smsEvents.Add(smsEvent);
        log.Info($"SMS sent: ({smsEvent})");
    }

    var gcms = validSubscriptions.Where(subscription => subscription.PartitionKey == "GCM");
    while (gcms.Count() > 0)
    {
        log.Info($"GCM left: ({gcms.Count()})");

        var gcmEvent = $"[{type}]New Valid Appointment\n"
            + $"{category}/{subCategory} - {time}{(expiration == null ? string.Empty : "-")}{expiration}\n"
            + string.Join("\n", gcms.Take(100).Select(subscription => subscription.RowKey));

        gcms = gcms.Skip(100);
    }

    log.Info("Finished.");
}
