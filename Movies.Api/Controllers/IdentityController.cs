// Import necessary namespaces for JWT handling, security claims, encoding, JSON processing, and ASP.NET Core
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Movies.Api.Auth;

// Define the namespace for the Identity API controllers
namespace Movies.Api.Controllers;

// Decorate the controller with ApiController attribute for ASP.NET Core web API features
[ApiController]
// Define the controller class that inherits from ControllerBase (base class for MVC controllers without view support)
public class IdentityController : ControllerBase
{
    // Constant string for the token secret key (Note: In production, this should be securely stored)
    private const string TokenSecret = "ForTheLoveOfGodStoreAndLoadThisSecurely";
    // Static TimeSpan defining how long the token will be valid (8 hours in this case)
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(8);

    // Define an HTTP POST endpoint at the "token" route
    [HttpPost(ApiEndpoints.Identity.Base)]
    // Method to generate JWT tokens that takes a TokenGenerationRequest from the request body
    public IActionResult GenerateToken(
        [FromBody] TokenGenerationRequest request)
    {
        // Create an instance of JwtSecurityTokenHandler to handle JWT operations
        var tokenHandler = new JwtSecurityTokenHandler();
        // Convert the secret key string into a byte array using UTF8 encoding
        var key = Encoding.UTF8.GetBytes(TokenSecret);

        // Create a list of claims to be included in the token
        var claims = new List<Claim>
        {
            // Add a unique identifier claim (JTI - JWT ID)
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Add subject claim with the user's email
            new(JwtRegisteredClaimNames.Sub, request.Email),
            // Add email claim
            new(JwtRegisteredClaimNames.Email, request.Email),
            // Add custom userid claim from the request
            new("userid", request.UserId.ToString())
        };

        // Process any custom claims provided in the request
        foreach (var claimPair in request.CustomClaims)
        {
            // Cast the claim value to JsonElement to inspect its type
            var jsonElement = (JsonElement)claimPair.Value;
            // Determine the claim value type based on the JsonValueKind
            var valueType = jsonElement.ValueKind switch
            {
                JsonValueKind.True => ClaimValueTypes.Boolean,
                JsonValueKind.False => ClaimValueTypes.Boolean,
                JsonValueKind.Number => ClaimValueTypes.Double,
                _ => ClaimValueTypes.String
            };

            // Create a new claim with proper type handling
            var claim = new Claim(claimPair.Key, claimPair.Value.ToString()!, valueType);
            // Add the custom claim to the claims list
            claims.Add(claim);
        }

        // Create the token descriptor that defines the token's characteristics
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Set the claims identity with all the claims we've prepared
            Subject = new ClaimsIdentity(claims),

            // Set token expiration time (current time + 8 hours)
            Expires = DateTime.UtcNow.Add(TokenLifetime),

            // Set the issuer of the token
            Issuer = "https://id.nickchapsas.com",

            // Set the intended audience of the token
            Audience = "https://movies.nickchapsas.com",

            // Set the signing credentials using the secret key and HMAC SHA256 algorithm
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        // Create the JWT security token based on the descriptor
        var token = tokenHandler.CreateToken(tokenDescriptor);

        // Write the token to a string
        var jwt = tokenHandler.WriteToken(token);

        // Return the token string in an HTTP 200 OK response
        return Ok(jwt);
    }
}