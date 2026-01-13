using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace support;

public class VerifySubscription
{
    private readonly ILogger<VerifySubscription> _logger;

    public VerifySubscription(ILogger<VerifySubscription> logger)
    {
        _logger = logger;
    }

    [Function("VerifySubscription")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
