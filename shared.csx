#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;


public class Appointment : TableEntity
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public DateTime? Time { get; set; }
    public DateTime? Expiration { get; set; }
    public DateTime Published { get; set; }
    public DateTime? Appointed { get; set; }

    private const char Separator = '^';
    public string ToEventMessage()
    {
        return string.Join(Separator.ToString(), this.Type, this.Category, this.SubCategory, this.Time, this.Expiration, this.Published, this.Appointed);
    }

    public static Appointment FromEventMessage(string message)
    {
        var splits = message.Split(Separator);
        return new Appointment
        {
            Type = splits[0],
            Category = splits[1],
            SubCategory = splits[2],
            Time = Parse(splits[3]),
            Expiration = Parse(splits[4]),
            Published = Parse(splits[5]).Value,
            Appointed = Parse(splits[6])
        };
    }

    private static DateTime? Parse(string timeString)
    {
        DateTime time;
        return DateTime.TryParse(timeString, out time)
        ? time
        : (DateTime?)null;
    }
}


public class Subscription : TableEntity
{
    public string IRPCategory { get; set; }
    public string IRPSubCategory { get; set; }
    public string VisaCategory { get; set; }

    private const char Separator = '^';
    public string ToEventMessage()
    {
        return string.Join(Separator.ToString(), this.IRPCategory, this.IRPSubCategory, this.VisaCategory);
    }

    public static Subscription FromEventMessage(string message)
    {
        var splits = message.Split(Separator);
        return new Subscription
        {
            IRPCategory = splits[0],
            IRPSubCategory = splits[1],
            VisaCategory = splits[2]
        };
    }
}


public class AppointmentStatistics : TableEntity
{
    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int ValidAppointments { get; set; }

    public int PublishAppointments { get; set; }
}


public class Queries
{
    public Queries(HttpRequestMessage req, TraceWriter log)
    {
        var nameValuePairs = req.GetQueryNameValuePairs();
        log.Info($"Queries({nameValuePairs.Count()})");
        Dictionary = nameValuePairs
            .GroupBy(pair => pair.Key.ToLower())
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Value).ToArray());
    }

    public readonly IDictionary<string, string[]> Dictionary;

    public string[] this[string key]
    {
        get
        {
            string[] result;

            if (!Dictionary.TryGetValue(key, out result))
            {
                result = null;
            }

            return result;
        }
        set
        {
            Dictionary[key] = value;
        }
    }
}