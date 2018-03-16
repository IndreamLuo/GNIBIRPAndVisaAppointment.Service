#r "Newtonsoft.Json"

using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.Text;

public static object Run(TimerInfo Timer, TraceWriter log, IEnumerable<dynamic> lastAppointments)
{
    log.Info("Watch IRP And Visa Services function processed a request.");
    log.Info($"Last Appointments:{lastAppointments.Count().ToString()}");

    using (var client = new HttpClient())
    {
        var now = DateTime.Now;
        log.Info(now.ToString());
        var newAppointments = new
        {
            Time = now,
            Appointments = new List<Appointment>()
        };

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
            
            var appointment = new Appointment
            {
                Type = api.Type,
                Category = api.Category,
                SubCategory = api.SubCategory,
                Times = new List<object>(),
                Expirations = new List<object>()
            };
            object expiration;
            var anyTime = false;
            switch (api.Type)
            {
                case "IRP":
                    if (urlResult.empty != "TRUE")
                    {
                        foreach (var slot in urlResult.slots)
                        {
                            var time = ConvertSlotToDateTime(log, api.Type, api.Category, api.SubCategory, (slot.time as object).ToString(), out expiration);
                            appointment.Times.Add(time);
                            appointment.Expirations.Add(expiration);
                            anyTime = true;
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
                                    var time = ConvertSlotToDateTime(log, api.Type, api.Category, api.SubCategory, (slot.time as object).ToString(), out expiration);
                                    appointment.Times.Add(time);
                                    appointment.Expirations.Add(expiration);
                                    anyTime = true;
                                }
                            }
                        }
                    }
                    break;
            }

            if (anyTime)
            {
                newAppointments.Appointments.Add(appointment);
            }
        }

        log.Info("Watch IRP And Visa Services function finished.");
        return IsDifferentAppointments(log, newAppointments, lastAppointments.FirstOrDefault())
        ? newAppointments
        : null;
    }
}



public class Appointment
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public List<object> Times { get; set; }
    public List<object> Expirations { get; set; }
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
        URL = "https://reentryvisa.inis.gov.ie/website/INISOA/IOA.nsf/(getDTAvail)?openagent&type=I",
        Category = "Individual",
    },
    new API
    {
        Type = "Visa",
        URL = "https://reentryvisa.inis.gov.ie/website/INISOA/IOA.nsf/(getDTAvail)?openagent&type=F",
        Category = "Family",
    },
    new API
    {
        Category = "Visa",
        Type = "Emergency",
        Data = new
        {
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



public static object ConvertSlotToDateTime(TraceWriter log, string type, string category, string subCategory, string slot, out object expiration)
{
    object result = slot;
    expiration = null;
    
    switch (type)
    {
        case "IRP":
            //25 April 2018 - 11:00
            var units = slot.Split(' ', ':');
            result = new DateTime(Convert.ToInt32(units[2]),
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
            result = new DateTime(year, month, day, hour, minute, 0);
            if (visaUnits.Length > 6)
            {
                var expiredHour = Convert.ToInt32(visaUnits[7]);
                var expiredMinute = Convert.ToInt32(visaUnits[8]);
                expiration = new DateTime(year, month, day, expiredHour, expiredMinute, 0);
            }
            break;
        
        default:
            log.Info("Unknown appointment type.");
            result = DateTime.MinValue;
            break;
    }

    return result;
}



public static bool IsDifferentAppointments(TraceWriter log, dynamic newAppointments, dynamic lastAppointments)
{
    log.Info("Check with last appointments.");
    log.Info("Checking count.");
    log.Info($"new({newAppointments.Appointments.Count}) : old({lastAppointments?.Appointments?.Count ?? -1})");
    if (newAppointments.Appointments.Count != lastAppointments?.Appointments?.Count ?? -1)
    {
        return true;
    }

    var newAppointmentsList = (newAppointments
        .Appointments as IEnumerable<object>)
        .Cast<dynamic>()
        .OrderBy(appointment => appointment.Type)
        .ThenBy(appointment => appointment.Category)
        .ThenBy(appointment => appointment.SubCategory)
        .ToArray();

    var lastAppointmentsList = (lastAppointments
        .Appointments as IEnumerable<object>)
        .Cast<dynamic>()
        .OrderBy(appointment => appointment.Type)
        .ThenBy(appointment => appointment.Category)
        .ThenBy(appointment => appointment.SubCategory)
        .ToArray();

    log.Info("Checking order.");
    for (var index = 0; index < newAppointmentsList.Length; index++)
    {
        var newAppointment = newAppointmentsList[index];
        var lastAppointment = lastAppointmentsList[index];

        log.Info("Checking categories.");
        if (!AreEqual(newAppointment.Type, lastAppointment?.Type?.ToString())
            || !AreEqual(newAppointment.Category, lastAppointment?.Category?.ToString())
            || !AreEqual(newAppointment.SubCategory, lastAppointment?.SubCategory?.ToString()))
        {
            log.Info("Different in Type/Category/Subcategory");
            return true;
        }

        var newTimes = (newAppointment.Times as IEnumerable<object>)
            .Select(time => time.ToString())
            .ToArray();
        var lastTimes = (lastAppointment.Times as IEnumerable<object>)
            .Select(time => time.ToString())
            .ToArray();

        log.Info("Check times.");

        if (newTimes.Length != lastTimes.Length)
        {
            log.Info($"Count difference between new times({newTimes.Length}) and last times({lastTimes.Length}).");
            return true;
        }

        for (var subIndex = 0; subIndex < newTimes.Length; subIndex++)
        {
            if (newTimes[subIndex] != lastTimes[subIndex])
            {
                log.Info($"{newTimes[subIndex]} is different from {lastTimes[subIndex]}.");
                return true;
            }
        }
    }
    
    log.Info("Same as last appointments");
    return false;
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