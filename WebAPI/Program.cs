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
using IAuthorizationService = Domain.Contracts.IAuthorizationService;

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
builder.Services.AddScoped<IRoleRegistry, RoleRegistry>();

// Settings
builder.Services.Configure<AuthenticationSettings>(
    builder.Configuration.GetSection("AuthenticationSettings"));
builder.Services.Configure<AuthorizationSettings>(
    builder.Configuration.GetSection("AuthorizationSettings"));

// Domain services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

// Authentication
builder.Services.AddAuthorization();
AddTokenAuthentication(builder.Services, builder.Configuration);

// Build Applcation
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

app.MapPost("/login", [AllowAnonymous] async ([FromBody] SignInRequest signIn,
    IAuthenticationService authenticationService) =>
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

app.MapPost("/register", [AllowAnonymous] async ([FromBody] SignUpRequest signUp,
    IAuthenticationService authenticationService) =>
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

app.MapPost("/user/{userId}/role", [Authorize] async (Guid userId, [FromBody] string roleName,
    IAuthorizationService authorizationService, IAuthenticationService authenticationService, IHttpContextAccessor httpContextAccessor) =>
{
    try
    {
        var role = new Role(roleName);
        var currentUser = await GetLoggedInUserAsync(httpContextAccessor, authenticationService);
        if (currentUser is not null && await authorizationService.CanManageRoles(currentUser.Guid))
        {
            await authorizationService.AssignRoleAsync(role, userId);
            return Results.Ok();
        };
        return Results.Unauthorized();
    }
    catch (AuthorizationException authEx) when (authEx.Cause == ExceptionCause.IncorrectData)
    {
        return Results.BadRequest(new { authEx.Message });
    }
});

await AddPredefinedRoles(app);

// Run Application
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

static async Task<WebApplication> AddPredefinedRoles(WebApplication app)
{
    using var scopeServices = app.Services.CreateScope();
    var authorizationService = scopeServices.ServiceProvider.GetRequiredService<IAuthorizationService>();
    await authorizationService.AddPredefinedRolesAsync();
    return app;
}

async Task<User?> GetLoggedInUserAsync(IHttpContextAccessor httpAccessor, IAuthenticationService authenticationService)
{
    ClaimsPrincipal? userClaims = httpAccessor.HttpContext?.User;
    string email = userClaims.FindFirstValue(ClaimTypes.Email);
    return await authenticationService.GetIdentityAsync(email);
}