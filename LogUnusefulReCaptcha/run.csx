#load "..\shared.csx"
#load "..\test.csx"

#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, ICollector<reCaptchaToken> outTable, TraceWriter log)
{
    string token = await req.Content.ReadAsStringAsync();
    var now = DateTime.UtcNow.AddHours(1);

    outTable.Add(new reCaptchaToken()
    {
        PartitionKey = token,
        RowKey = "New",
        Time = now,
        Token = token
    });

    reCaptcha.AssertCannotBeCrossDomainUsed(token);

    return req.CreateResponse(HttpStatusCode.Created);
}

