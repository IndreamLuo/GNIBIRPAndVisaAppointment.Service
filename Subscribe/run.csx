#r "Microsoft.Azure.ApiHub.Sdk"
#r "Newtonsoft.Json"

using System;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ApiHub;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

public static IActionResult Run(HttpRequest req,
    TraceWriter log,
    ITable<Subscription> subscriptions
    )
{
    log.Info("C# HTTP trigger function processed a request.");

    var token = req.Query["token"];
    var email = req.Query["email"];
    var phone = req.Query["phone"];
    var type = req.Query["type"];
    bool? hasIRP = null;
    bool hasIRPValue;
    if (bool.TryParse(req.Query["hasIRP"], out hasIRPValue)) {
        hasIRP = hasIRPValue;
    }
    int? category = Subscription.FromStringToCategory(req.Query["category"]);
    

    string name = req.Query["name"];

    string requestBody = new StreamReader(req.Body).ReadToEnd();
    dynamic data = JsonConvert.DeserializeObject(requestBody);
    name = name ?? data?.name;

    return name != null
        ? (ActionResult)new OkObjectResult($"Hello, {name}")
        : new BadRequestObjectResult("Please pass a name on the query string or in the request body");
}

public class Subscription
{
    public string Token { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public AppointmentType Type { get; set; }
    public int? Category { get; set; }
    public bool? HasIRP { get; set; }

    public static int? FromStringToCategory(string categoryString) {
        switch(categoryString.ToLower()) {
            case "work":
                return 1;
            case "study":
                return 2;
            case "other":
                return 3;
            case "family":
                return 4;
            case "individual":
                return 5;
            case "emergency":
                return 6;
            default:
                return null;
        }
    }
}

public enum AppointmentType
{
    IRP,
    Visa
}