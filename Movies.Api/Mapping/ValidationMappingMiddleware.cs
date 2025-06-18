using FluentValidation;
using Movies.Contracts.Responses;

namespace Movies.Api.Mapping;


public class ValidationMappingMiddleware
{
    private RequestDelegate _next;

    public ValidationMappingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    public async Task InvokeAsync(HttpContext context) //context of our request 
    {
        try
        {
            await _next(context); // pass the thing to the next middleware in the pipline 
        }
        catch (ValidationException ex)
        {
            //map the exception to that object 
            context.Response.StatusCode = 400;
            var validationFailureResponse = new ValidationFailureResponse
            {
                Errors = ex.Errors.Select(x => new ValidationResponse
                {
                    PropertyName = x.PropertyName,
                    Message = x.ErrorMessage
                })
            };

            await context.Response.WriteAsJsonAsync(validationFailureResponse);



        }
    }
}