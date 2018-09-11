#load "..\shared.csx"

#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<reCaptchaToken> outTable, TraceWriter log)
{
    dynamic data = await req.Content.ReadAsAsync<object>();
    string token = data.token;
    var now = DateTime.UtcNow.AddHours(1);

    outTable.Add(new reCaptchaToken()
    {
        PartitionKey = token,
        RowKey = "New",
        Time = now,
        Token = token
    });

    return req.CreateResponse(HttpStatusCode.Created);
}

