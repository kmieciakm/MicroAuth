using Database.MsSQL;
using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;
using Domain.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT Bearer token **_only_**",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {securityScheme, new string[] { }}
    });
});

// Database setup
var identityBuilder = builder.Services.AddIdentityCore<DbUser>(opt =>
{
    opt.User.RequireUniqueEmail = true;
    opt.Password.RequireDigit = true;
    opt.Password.RequireUppercase = true;
    opt.Password.RequireLowercase = true;
    opt.Password.RequireNonAlphanumeric = true;
    opt.Password.RequiredLength = 8;
});

identityBuilder
    .AddRoles<DbRole>()
    .AddSignInManager<SignInManager<DbUser>>()
    .AddRoleManager<RoleManager<DbRole>>()
    .AddUserManager<UserManager<DbUser>>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddDbContext<AuthDbContext>(options => {
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("SqlServer"));
});

builder.Services.AddScoped<IUserRegistry, UserRegistry>();

// Settings
builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("AuthenticationSettings"));

// Domain services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Authentication
builder.Services.AddAuthorization();
AddTokenAuthentication(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/identity", [Authorize] async (IAuthenticationService authenticationService, IHttpContextAccessor httpAccessor) =>
{
    ClaimsPrincipal? userClaims = httpAccessor.HttpContext?.User;
    string email = userClaims.FindFirstValue(ClaimTypes.Email);
    if (email.IsNullOrEmpty())
    {
        return Results.Unauthorized();
    }

    var user = await authenticationService.GetIdentityAsync(email);
    if (user == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(user);
});

app.MapPost("/login", [AllowAnonymous] async ([FromBody] SignInRequest signIn, IAuthenticationService authenticationService) =>
{
    try
    {
        var token = await authenticationService.SignInAsync(signIn);
        return Results.Ok(token);
    }
    catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
    {
        return Results.BadRequest(authEx.Message);
    }
    catch (AuthenticationException authEx) when (authEx.Cause == ExceptionCause.Unknown)
    {
        return Results.Problem(statusCode: 500, detail: authEx.Message);
    }
});

app.MapPost("/register", [AllowAnonymous] async ([FromBody] SignUpRequest signUp, IAuthenticationService authenticationService) =>
{
    try
    {
        var user = await authenticationService.SignUpAsync(signUp);
        return Results.Ok(user);
    }
    catch (RegistrationException registerEx) when (registerEx.Cause == ExceptionCause.IncorrectData)
    {
        return Results.BadRequest(new { registerEx.Message, registerEx.Details });
    }
});

app.Run();

static IServiceCollection AddTokenAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var settingsSection = configuration.GetSection("AuthenticationSettings");
    var settings = settingsSection.Get<AuthenticationSettings>();
    var key = Encoding.ASCII.GetBytes(settings.Secret);

    services
        .Configure<AuthenticationSettings>(settingsSection)
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(authOptions =>
        {
            authOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = settings.Issuer,
                ValidAudience = settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            };
        });
    return services;
}