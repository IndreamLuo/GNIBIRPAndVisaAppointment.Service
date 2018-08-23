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
    public int ValidIRPWorkNew { get; set; }
    public int PublishIRPWorkNew { get; set; }
    public double TotalContinuousIRPWorkNew { get; set; }
    public int ValidIRPWorkRenew { get; set; }
    public int PublishIRPWorkRenew { get; set; }
    public double TotalContinuousIRPWorkRenew { get; set; }
    public int ValidIRPStudyNew { get; set; }
    public int PublishIRPStudyNew { get; set; }
    public double TotalContinuousIRPStudyNew { get; set; }
    public int ValidIRPStudyRenew { get; set; }
    public int PublishIRPStudyRenew { get; set; }
    public double TotalContinuousIRPStudyRenew { get; set; }
    public int ValidIRPOtherNew { get; set; }
    public int PublishIRPOtherNew { get; set; }
    public double TotalContinuousIRPOtherNew { get; set; }
    public int ValidIRPOtherRenew { get; set; }
    public int PublishIRPOtherRenew { get; set; }
    public double TotalContinuousIRPOtherRenew { get; set; }
    public int ValidVisaIndividual { get; set; }
    public int PublishVisaIndividual { get; set; }
    public double TotalContinuousVisaIndividual { get; set; }
    public int ValidVisaFamily { get; set; }
    public int PublishVisaFamily { get; set; }
    public double TotalContinuousVisaFamily { get; set; }
    public int ValidVisaEmergency { get; set; }
    public int PublishVisaEmergency { get; set; }
    public double TotalContinuousVisaEmergency { get; set; }
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


public class AppointmentTypes
{
    public const string IRP = "IRP";
    public const string Visa = "Visa";
}

public class Categories
{
    public const string Work = "Work";
    public const string Study = "Study";
    public const string Other = "Other";
    public const string Individual = "Individual";
    public const string Family = "Family";
    public const string Emergency = "Emergency";
}

public class SubCategories
{
    public const string New = "New";
    public const string Renewal = "Renewal";
}


public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}


public class Watch : TableEntity
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public double WorkNew { get; set; }
    public double WorkRenewal { get; set; }
    public double StudyNew { get; set; }
    public double StudyRenewal { get; set; }
    public double OtherNew { get; set; }
    public double OtherRenewal { get; set; }
    public double Individual { get; set; }
    public double Family { get; set; }
    public double Emergency { get; set; }
}