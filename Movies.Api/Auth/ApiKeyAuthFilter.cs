using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Movies.Api.Auth;

public class ApiKeyAuthFilter : IAuthorizationFilter
{
    // to implment authorization with Api-Key we need to use filter 
    private readonly IConfiguration _configuration;

    public ApiKeyAuthFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        //you extyracted from the header request 
        if (!context.HttpContext.Request.Headers.TryGetValue(AuthConstants.ApiKeyHeaderName,
                out var extractedApiKey))
        {
            //if we cannot extracted 
            context.Result = new UnauthorizedObjectResult("API Key missing");
            return;
        }
        //get the key from the AppSettings
        var apiKey = _configuration["ApiKey"]!;
        if (apiKey != extractedApiKey)
        {
            context.Result = new UnauthorizedObjectResult("Invalid API Key");
        }
    }
}


