#r "Microsoft.Azure.Documents.Client"
using System;
using System.Collections.Generic;
using Microsoft.Azure.Documents;

public static void Run(IReadOnlyList<Document> documents, TraceWriter log)
{
    log.Info("------------------------------------Triggered by CosmosDB--------");
    if (documents != null && documents.Count > 0)
    {
        log.Verbose("Documents modified " + documents.Count);
        log.Verbose("First document Id " + documents[0].Id);
    }
}
