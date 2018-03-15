#r "Newtonsoft.Json"

using Newtonsoft.Json;

using System.Net;
using System.Net.Http;
using System.Text;

public static IEnumerable<dynamic> Run(TimerInfo Timer,
    TraceWriter log,
    IEnumerable<dynamic> lastAppointments,
    out IEnumerable<dynamic> newAppointments)
{
    log.Info("Watch IRP And Visa Services function processed a request.");

    using (var client = new HttpClient())
    {
        newAppointments = new List<API>();

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

            var appointments = new List<DateTime>();
            switch (api.Type)
            {
                case "IRP":
                    if (urlResult.empty != "TRUE")
                    {
                        foreach (var slot in urlResult.slots)
                        {
                            var validDate = ConvertSlotToDateTime(api.Type, api.Category, api.SubCategory, (slot as object).ToString());
                            appointments.Add(validDate.Value);
                        }
                    }
                    break;
                case "Visa":
                    break;
            }
        }

        if (lastAppointments != null && lastAppointments.Any())
        {
            
        }

        log.Info("Watch IRP And Visa Services function finished.");
        return newAppointments;
    }
}

public class API
{
    public string Type { get; set; }
    public string Category { get; set; }
    public string SubCategory { get; set; }
    public string URL { get; set; }
    public dynamic Data { get; set; }
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
        Data = {
            dates = new []
            {
                string.Format($"{DateTime.Now.Day}/{DateTime.Now.Month}/{DateTime.Now.Year}"),
                string.Format($"{DateTime.Now.AddDays(1).Day}/{DateTime.Now.AddDays(1).Month}/{DateTime.Now.AddDays(1).Year}"),
                string.Format($"{DateTime.Now.AddDays(2).Day}/{DateTime.Now.AddDays(2).Month}/{DateTime.Now.AddDays(2).Year}")
            }
        }
    }
};

public static DateTime? ConvertSlotToDateTime(string type, string category, string SubCategory, string slot)
{
    DateTime result;

    if (DateTime.TryParse(slot, out result))
    {
        return null;
    }

    return result;
}