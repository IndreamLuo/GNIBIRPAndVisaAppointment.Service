#load "..\shared.csx"

using System.Net;

public static async Task Run(string eventMessage,
    TraceWriter log,
    IQueryable<Subscription> subscriptions,
    ICollector<string> gcmNotificationEvents,
    ICollector<string> smsEvents)
{
    log.Info($"C# HTTP trigger function processed a request from event({eventMessage}).");

    var appointment = Appointment.FromEventMessage(eventMessage);

    log.Info("Event Message deserialized.");

    var validSubscriptions = appointment.Type.ToLower() == "irp"
    ? subscriptions
        .ToArray()
        .Where(subscription => subscription.IRPCategory == appointment.Category && subscription.IRPSubCategory == appointment.SubCategory)
    : subscriptions
        .ToArray()
        .Where(subscription => subscription.VisaCategory == appointment.Category);
    
    log.Info("Sending SMS");

    var smses = validSubscriptions.Where(subscription => subscription.PartitionKey == "SMS");

    log.Info($"SMS subscriptions got({smses.Count()}).");

    foreach (var sms in smses)
    {
        var smsEvent = $"{appointment.Time} - {appointment.Type}/{appointment.Category}/{appointment.SubCategory}\n"
            + sms.RowKey;

        log.Info($"SMS sending: ({smsEvent})");

        smsEvents.Add(smsEvent);

        log.Info($"SMS sent.");
    }

    log.Info("Sending GCM");

    var gcms = validSubscriptions.Where(subscription => subscription.PartitionKey == "GCM");

    log.Info($"GCM subscriptions got({gcms.Count()}).");

    var timeFormat = "dd MMMM yyyy HH:mm";

    var gcmMessage = $"type\n{appointment.Type}\n"
        + $"category\n{appointment.Category}\n"
        + $"subCategory\n{appointment.SubCategory}\n"
        + $"time\n{appointment.Time}\n"
        + $"expiration\n{appointment.Expiration}\n"
        + $"title\nNew Valid [{appointment.Type}] Appointment\n"
        + $"message\n{appointment.Category}-{appointment.SubCategory}: {(appointment.Time.HasValue ? appointment.Time.Value.ToString(timeFormat) : string.Empty)}{(appointment.Expiration == null ? string.Empty : "-")}{(appointment.Expiration.HasValue ? appointment.Expiration.Value.ToString(timeFormat) : string.Empty)}\n";
    foreach (var gcm in gcms)
    {
        var gcmEvent = $"{gcmMessage}gcmToken\n{gcm.RowKey}";

        log.Info($"GCM sending: ({gcmEvent})");

        gcmNotificationEvents.Add(gcmEvent);

        log.Info($"GCM sent.");
    }

    log.Info("Finished.");
}
