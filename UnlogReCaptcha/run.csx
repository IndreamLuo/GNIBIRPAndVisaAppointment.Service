#load "..\shared.csx"
#load "..\test.csx"

#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req,
    IQueryable<reCaptchaToken> inTable,
    CloudTable outTable,
    TraceWriter log)
{
    string token = await req.Content.ReadAsStringAsync();
    var now = DateTime.UtcNow.AddHours(1);

    reCaptcha.StopAssertingCannotBeCrossDomainUsed(token);

    outTable.Execute(TableOperation.Delete(inTable.Where(item => item.PartitionKey == token).First()));

    return req.CreateResponse(HttpStatusCode.Created);
}
