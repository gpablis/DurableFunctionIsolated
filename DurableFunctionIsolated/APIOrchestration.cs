using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

static class APIOrchestration
{
    // Client fucntion - Triggers Orchestrator
    [Function(nameof(Orchestrator_HttpStart))]
    public static async Task<HttpResponseData> Orchestrator_HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
        [DurableClient] DurableClientContext durableContext,
        FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(Orchestrator_HttpStart));

        string instanceId = await durableContext.Client.ScheduleNewOrchestrationInstanceAsync(nameof(APIOrchestrator));
        logger.LogInformation("Created new orchestration with instance ID = {instanceId}", instanceId);

        logger.LogInformation("Orchestration flow start");
        return durableContext.CreateCheckStatusResponse(req, instanceId);
    }
   
    // Orchestration function
    [Function(nameof(APIOrchestrator))]
    public static async Task<string> APIOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context, FunctionContext executionContext)
    {
        ILogger logger = executionContext.GetLogger(nameof(APIOrchestrator));
        // Orchestrator executes Activity function
        string result = "";
        result += await context.CallActivityAsync<string>(nameof(SendRequestAsync), "Derby") + " ";
        result += await context.CallActivityAsync<string>(nameof(SendRequestAsync), "Caipirissima") + " ";
        result += await context.CallActivityAsync<string>(nameof(SendRequestAsync), "Brainteaser");
        logger.LogInformation("Orchestration flow completed");

        return result;
    }

    // Activity Function, receives a string drink from Orchestrator and calls api with drink parameter
    [Function(nameof(SendRequestAsync))]
    public static async Task<string> SendRequestAsync([ActivityTrigger] string drink, FunctionContext executionContext)
    {
        
        var API = "https://www.thecocktaildb.com/api/json/v1/1/search.php?s="+drink;

        var responseString = string.Empty;
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(API);
            responseString = await response.Content.ReadAsStringAsync();
        }

        ILogger logger = executionContext.GetLogger(nameof(SendRequestAsync));
        logger.LogInformation(responseString);
        return $"Drink:{drink}";
    }
}