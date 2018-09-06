#load "..\shared.csx"

#r "Microsoft.WindowsAzure.Storage"
#r "Newtonsoft.Json"

using System.Net;
using System.Text;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Table;

public static HttpResponseMessage Run(HttpRequestMessage req, IQueryable<Appointment> lastAppointments, TraceWriter log)
{
    var query = lastAppointments
        .ToArray()
        .Select(item => new
        {
            Type = item.Type,
            Category = item.Category,
            SubCategory = item.SubCategory,
            Time = item.Time,
            Expiration = item.Expiration
        });
    var json = JsonConvert.SerializeObject(query);
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json")
    };
}

public class Person : TableEntity
{
    public string Name { get; set; }
}
