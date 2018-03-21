using System.Net;

public static async Task Run(string eventMessage, TraceWriter log)
{
    log.Info($"C# HTTP trigger function processed a request from event({eventMessage}).");
}
