namespace Movies.Api.Auth;



public static class IdentityExtensions
{
    //extension method on the HttpContext to get the id 
    public static Guid? GetUserId(this HttpContext context)
    {
        var userId = context.User.Claims.SingleOrDefault(x => x.Type == "userid"); // get user Id From the claims 
        if (Guid.TryParse(userId?.Value, out var parsedId))
        {
            return parsedId;
        }

        return null;
    }
}