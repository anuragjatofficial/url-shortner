using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace URLShortner
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;
        private static Dictionary<string, string> urlMappings = new Dictionary<string, string>();

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,HttpRequestData data)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string url = req.GetDisplayUrl();


            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic jsonData = JsonConvert.DeserializeObject(requestBody);

            string longUrl = jsonData?.url;

            if (string.IsNullOrEmpty(longUrl))
            {
                return new BadRequestObjectResult("Please provide a valid URL in the request body.");
            }
            string shortKey = Guid.NewGuid().ToString("n").Substring(0, 8);

            urlMappings.Add(shortKey, longUrl);
            string shortUrl = $"{GetBaseUrl(req)}/api/r/{shortKey}";


            return new OkObjectResult(new { shortUrl});
        }

        public static string GetBaseUrl(HttpRequest req)
        {
            return $"{req.Scheme}://{req.Host}";
        }

        [Function("RedirectToOriginalUrl")]
        public async Task<IActionResult> RedirectToOriginalUrl(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "r/{code}")] 
            HttpRequest req,
            string code
        )
        {
            _logger.LogInformation($"Redirecting to the original URL for code: {code}");

            if (urlMappings.TryGetValue(code, out string originalUrl))
            {
                return new RedirectResult(originalUrl);
            }

            return new NotFoundResult();
        }
    }
}
