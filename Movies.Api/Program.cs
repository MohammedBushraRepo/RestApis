using System.Text;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Movies.Api.Auth;
using Movies.Api.Health;
using Movies.Api.Mapping;
using Movies.Api.Swagger;
using Movies.Application;
using Movies.Application.Repositories;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;


//configure the Authentication ********************************************************************
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["Jwt:Issuer"], // Add this
        ValidAudience = config["Jwt:Audience"], // Add this
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
        ValidateIssuerSigningKey = true,//if you didnet make it will not validate any thing
        ValidateLifetime = true,//because we dotn want to have key expired and still work 
        ValidateIssuer = true, // This was true but no ValidIssuer was set
        ValidateAudience = true // This was true but no ValidAudience was set
    };
});

//implement Authorization 
builder.Services.AddAuthorization(x =>
{
    //only admin claim 
    x.AddPolicy(AuthConstants.AdminUserPolicyName, p => p.RequireClaim(AuthConstants.AdminUserClaimName, "true"));

    //either admin or trusted member
    x.AddPolicy(AuthConstants.TrustedMemberPolicyName,
      p => p.RequireAssertion(c =>
      c.User.HasClaim(m => m is { Type: AuthConstants.AdminUserClaimName, Value: "true" }) ||
      c.User.HasClaim(m => m is { Type: AuthConstants.TrustedMemberClaimName, Value: "true" }))

    );
});

// register the api key 
builder.Services.AddScoped<ApiKeyAuthFilter>();

//////
builder.Services.AddApiVersioning(x =>
{
    //this object to specify the default version if not provided 
    x.DefaultApiVersion = new ApiVersion(1.0);
    x.AssumeDefaultVersionWhenUnspecified = true;
    x.ReportApiVersions = true;
    x.ApiVersionReader = new MediaTypeApiVersionReader("api-version");
    //new HeaderApiVersionReader("api-version"); // to view allowed versions from deprecated versions
}).AddMvc().AddApiExplorer();

//Register Caching
//builder.Services.AddResponseCaching();
//*** output caching ====> registeration 
builder.Services.AddOutputCache(x =>
{
    x.AddBasePolicy(c => c.Cache());
    x.AddPolicy("MovieCache", c =>
        c.Cache()
        .Expire(TimeSpan.FromMinutes(1))
        .SetVaryByQuery(new[] { "title", "year", "sortBy", "page", "pageSize" })
        .Tag("movies"));
});

builder.Services.AddControllers();

//register health checks  
builder.Services.AddHealthChecks()
.AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddSwaggerGen(x => x.OperationFilter<SwaggerDefaultValues>());
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"]!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    //to load the appropreate versions
    app.UseSwaggerUI(x =>
 {
     foreach (var description in app.DescribeApiVersions())
     {
         x.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
             description.GroupName);
     }
 });
}

//Add meta end-point to check the health of our Api 
app.MapHealthChecks("_health");

app.UseHttpsRedirection();
app.UseAuthentication(); ///should always be before authorization 
app.UseAuthorization();

//we should have caching here before middleware and after authentication and Authorization  very important 
//app.UseCors();
//app.UseResponseCaching();
app.UseOutputCache();

//to register the Middleware 
app.UseMiddleware<ValidationMappingMiddleware>();
app.MapControllers();

var dbIntializer = app.Services.GetRequiredService<DbIntializer>(); // to run the initializor 
await dbIntializer.InitializeAsync();

app.Run();
