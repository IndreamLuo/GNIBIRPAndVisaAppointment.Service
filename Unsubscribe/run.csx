#load "..\shared.csx"

#r "Microsoft.WindowsAzure.Storage"

using System.Net;
using Microsoft.WindowsAzure.Storage.Table;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req,
    string type,
    string key,
    Subscription currentSubscription,
    CloudTable outTable,
    TraceWriter log)
{
    log.Info($"Unsubscribe starts processing request with type({type}) and key({key}).");
    if (currentSubscription != null)
    {
        log.Info("Start deleting");
        var deleteOperation = TableOperation.Delete(currentSubscription);
        outTable.Execute(deleteOperation);
        log.Info("Deleted");

        return req.CreateResponse(HttpStatusCode.OK);
    }

    log.Info("Not exist");
    return req.CreateResponse(HttpStatusCode.Accepted);
}
