#load "..\shared.csx"

#r "Newtonsoft.Json"

#r "Microsoft.WindowsAzure.Storage"
using Microsoft.WindowsAzure.Storage.Table;

using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.Text;

public static void Run(TimerInfo Timer,
    TraceWriter log,
    IQueryable<Appointment> lastAppointmentInput,
    CloudTable outTable,
    CloudTable outLastTable,
    ICollector<string> newValidAppointmentEventHubMessages)
{
    log.Info("Watch IRP And Visa Services function processed a request.");
    var lastAppointments = lastAppointmentInput.ToArray();
    log.Info($"Last Appointments:{lastAppointments.Count()}");

    var now = DateTime.Now;
    var day = DateTime.Now.ToString("ddMMyyyy HH:mm:ss");
    var rowKey = 0;

    using (var client = new HttpClient())
    {
        log.Info(now.ToString());
        var newAppointments = new List<Appointment>();

        log.Info("Start loading from APIs.");
        foreach (var api in APIs)
        {
            dynamic urlResult;
            if (api.URL == null)
            {
                urlResult = api.Data;
            }
            else
            {
                var response = client.GetAsync(api.URL).Result;
                if (response.IsSuccessStatusCode)
                {
                    urlResult = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result); 
                }
                else
                {
                    urlResult = new { error = response.StatusCode };
                }
            }
            
            switch (api.Type)
            {
                case "IRP":
                    if (urlResult.empty != "TRUE")
                    {
                        foreach (var slot in urlResult.slots)
                        {
                            var time = ConvertSlotToDateTime(log, api.Type, api.Category, api.SubCategory, (slot.time as object).ToString());
                            newAppointments.Add(new Appointment
                            {
                                PartitionKey = day,
                                RowKey = rowKey++.ToString(),
                                Type = api.Type,
                                Category = api.Category,
                                SubCategory = api.SubCategory,
                                Time = time.Time,
                                Expiration = time.Expiration,
                                Published = now
                            });
                        }
                    }
                    break;
                case "Visa":
                    if (urlResult.empty != "TRUE" && urlResult.dates != null && urlResult.dates[0] != "01/01/1900")
                    {
                        foreach (var date in urlResult.dates)
                        {
                            var subUrlResponse = client.GetAsync(string.Format(VisaSubURL, date, api.Category[0], 1)).Result;
                            var subUrlResult = JsonConvert.DeserializeObject(subUrlResponse.Content.ReadAsStringAsync().Result);
                            if (subUrlResult.slots != null)
                            {
                                foreach (var slot in subUrlResult.slots)
                                {
                                    var time = ConvertSlotToDateTime(log, api.Type, api.Category, api.SubCategory, (slot.time as object).ToString());
                                    newAppointments.Add(new Appointment
                                    {
                                        PartitionKey = day,
                                        RowKey = rowKey++.ToString(),
                                        Type = api.Type,
                                        Category = api.Category,
                                        SubCategory = api.SubCategory,
                                        Time = time.Time,
                                        Expiration = time.Expiration,
                                        Published = now
                                    });
                                }
                            }
                        }
                    }
                    break;
            }
        }


        log.Info("Check with last appointments.");
        log.Info("Checking count.");
        log.Info($"new({newAppointments.Count()}) : old({lastAppointments.Count()})");

        var passedAppointments = lastAppointments
            .Where(lastAppointment => newAppointments
                .All(newAppointment => newAppointment.Type != lastAppointment.Type
                    || newAppointment.Category != lastAppointment.Category
                    || newAppointment.SubCategory != lastAppointment.SubCategory
                    || newAppointment.Time != lastAppointment.Time
                    || newAppointment.Expiration != lastAppointment.Expiration))
            .ToArray();
        
        var newValidAppointments = newAppointments
            .Where(newAppointment => lastAppointments
                .All(lastAppointment => newAppointment.Type != lastAppointment.Type
                    || newAppointment.Category != lastAppointment.Category
                    || newAppointment.SubCategory != lastAppointment.SubCategory
                    || newAppointment.Time != lastAppointment.Time
                    || newAppointment.Expiration != lastAppointment.Expiration))
            .ToArray();

        log.Info($"New valid appointments({newValidAppointments.Count()}), passed Appointments({passedAppointments.Count()}).");
        if (newValidAppointments.Count() > 0 || passedAppointments.Count() > 0)
        {
            log.Info("All replicated appointments to be set existing keys.");
            foreach (var appointment in newAppointments)
            {
                var existingAppointment = lastAppointments.SingleOrDefault(lastAppointment => appointment.Type == lastAppointment.Type
                    && appointment.Category == lastAppointment.Category
                    && appointment.SubCategory == lastAppointment.SubCategory
                    && appointment.Time == lastAppointment.Time
                    && appointment.Expiration == lastAppointment.Expiration);
                
                log.Info($"Appointment({existingAppointment.PartitionKey}/{existingAppointment.RowKey}) changed.");
                if (existingAppointment != null)
                {
                    appointment.PartitionKey = existingAppointment.PartitionKey;
                    appointment.RowKey = existingAppointment.RowKey;
                    appointment.Published = existingAppointment.Published;
                }
            }

            log.Info("Delete all last appointments.");
            foreach (var appointment in lastAppointments)
            {
                outLastTable.Execute(TableOperation.Delete(appointment));
            }

            log.Info("Set last appointments.");
            foreach (var appointment in newAppointments)
            {
                outLastTable.Execute(TableOperation.Insert(appointment));
            }

            log.Info("Output new appointments.");
            foreach (var appointment in newValidAppointments)
            {
                outTable.Execute(TableOperation.Insert(appointment));
                var eventMessage = appointment.ToEventMessage();
                log.Info($"New Event Message: {eventMessage}");
                newValidAppointmentEventHubMessages.Add(appointment.ToEventMessage());
            }

            log.Info("Set passed appointments.");
            foreach (var appointment in passedAppointments)
            {
                appointment.ETag = "*";
                appointment.Appointed = now;
                outTable.Execute(TableOperation.Replace(appointment));
            }
        }
    }
}



public class TimeRange
{
    public DateTime? Time { get; set; }
    public DateTime? Expiration { get; set; }
}



public class API
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public string URL { get; set; }
    public object Data { get; set; }
    public string SubURL { get; set; }
}



public static string VisaSubURL = "https://reentryvisa.inis.gov.ie/website/INISOA/IOA.nsf/(getApps4DT)?openagent&dt={0}&type={1}&num={2}";
public static API[] APIs = new []
{
    new API
    {
        Type = "IRP",
        Category = "Work",
        SubCategory = "New",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Work&sbcat=All&typ=New",
    },
    new API
    {
        Type = "IRP",
        Category = "Work",
        SubCategory = "Renewal",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Work&sbcat=All&typ=Renewal",
    },
    new API
    {
        Type = "IRP",
        Category = "Study",
        SubCategory = "New",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Study&sbcat=All&typ=New",
    },
    new API
    {
        Type = "IRP",
        Category = "Study",
        SubCategory = "Renewal",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Study&sbcat=All&typ=Renewal",
    },
    new API
    {
        Type = "IRP",
        Category = "Other",
        SubCategory = "New",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Other&sbcat=All&typ=New",
    },
    new API
    {
        Type = "IRP",
        Category = "Other",
        SubCategory = "Renewal",
        URL = "https://burghquayregistrationoffice.inis.gov.ie/Website/AMSREG/AMSRegWeb.nsf/(getAppsNear)?openpage&cat=Other&sbcat=All&typ=Renewal",
    },
    new API
    {
        Type = "Visa",
        Category = "Individual",
        URL = "https://reentryvisa.inis.gov.ie/website/INISOA/IOA.nsf/(getDTAvail)?openagent&type=I",
    },
    new API
    {
        Type = "Visa",
        Category = "Family",
        URL = "https://reentryvisa.inis.gov.ie/website/INISOA/IOA.nsf/(getDTAvail)?openagent&type=F",
    },
    new API
    {
        Type = "Visa",
        Category = "Emergency",
        Data = new
        {
            empty = "FALSE",
            dates = new []
            {
                string.Format($"{DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year}"),
                string.Format($"{DateTime.Now.AddDays(1).Day}/{DateTime.Now.AddDays(1).Month}/{DateTime.Now.AddDays(1).Year}"),
                string.Format($"{DateTime.Now.AddDays(2).Day}/{DateTime.Now.AddDays(2).Month}/{DateTime.Now.AddDays(2).Year}")
            }
        }
    }
};



public static Dictionary<string, int> Months = new Dictionary<string, int>
{
    {"January", 1},
    {"February", 2},
    {"March", 3},
    {"April", 4},
    {"May", 5},
    {"June", 6},
    {"July", 7},
    {"Auguest", 8},
    {"September", 9},
    {"October", 10},
    {"November", 11},
    {"December", 12},
};



public static TimeRange ConvertSlotToDateTime(TraceWriter log, string type, string category, string subCategory, string slot)
{
    DateTime? time = null;
    DateTime? expiration = null;
    
    switch (type)
    {
        case "IRP":
            //25 April 2018 - 11:00
            var units = slot.Split(' ', ':');
            time = new DateTime(Convert.ToInt32(units[2]),
                Months[units[1]],
                Convert.ToInt32(units[0]),
                Convert.ToInt32(units[4]),
                Convert.ToInt32(units[5]),
                0);
            break;

        case "Visa":
            //"18/03/2018 08:30 AM - 12:00 PM"
            var visaUnits = slot.Split(' ', '/', ':');
            var year = Convert.ToInt32(visaUnits[2]);
            var month = Convert.ToInt32(visaUnits[1]);
            var day = Convert.ToInt32(visaUnits[0]);
            var hour = Convert.ToInt32(visaUnits[3]) % 12 + (visaUnits[5] == "PM" ? 12 : 0);
            var minute = Convert.ToInt32(visaUnits[4]);
            time = new DateTime(year, month, day, hour, minute, 0);
            if (visaUnits.Length > 6)
            {
                var expiredHour = Convert.ToInt32(visaUnits[7]);
                var expiredMinute = Convert.ToInt32(visaUnits[8]);
                expiration = new DateTime(year, month, day, expiredHour, expiredMinute, 0);
            }
            break;
        
        default:
            log.Info("Unknown appointment type.");
            time = DateTime.MinValue;
            break;
    }

    return new TimeRange
    {
        Time = time,
        Expiration = expiration
    };
}



public static bool AreEqual(string left, string right)
{
    return left == right
    || (left == null || left == string.Empty) && (right == null || right == string.Empty);
}



public static string GetEnvironmentVariable(string name)
{
    return System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
}