using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application;
using Movies.Application.Repositories;

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


//////

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddDatabase(config["Database:ConnectionString"]!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication(); ///should always be before authorization 
app.UseAuthorization();

//to register the Middleware 
app.UseMiddleware<ValidationMappingMiddleware>();
app.MapControllers();

var dbIntializer = app.Services.GetRequiredService<DbIntializer>(); // to run the initializor 
await dbIntializer.InitializeAsync();

app.Run();
