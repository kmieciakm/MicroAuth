using API.Web;
using Database.MsSQL;
using Domain.Contracts;
using Domain.Exceptions;
using Domain.Infrastructure;
using Domain.Models;
using Domain.Services;
using Mailing.Queue;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using IAuthorizationService = Domain.Contracts.IAuthorizationService;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration
    .AddUserSecrets<Program>()
    .Build();

ConfigureServices(builder);

var app = builder.Build();
await Configure(app);

app.Run();

static void ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddControllers();
    builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
    builder.Services.AddSwaggerGen(c => {
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
        c.OperationFilter<AddSwaggerHeadersFilter>();
    });

    // MS SQL Database setup *****************************************************************
    var identityBuilder = builder.Services.AddIdentityCore<DbUser>(opt => {
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
    // *************************************************************************************

    // Azure Table Storage setup
/*    builder.Services.Configure<AzureTables.AzureStorageSettings>(
    builder.Configuration.GetSection("AzureStorageSettings"));

    builder.Services.AddSingleton<Database.AzureTables.IUsersRolesTable, Database.AzureTables.UsersRolesTable>();
    builder.Services.AddSingleton<Database.AzureTables.IRoleTable, Database.AzureTables.RolesTable>();
    builder.Services.AddSingleton<Database.AzureTables.IUserTable, Database.AzureTables.UsersTable>();
    builder.Services.AddSingleton<Database.AzureTables.ITokenTable, Database.AzureTables.TokenTable>();
    builder.Services.AddScoped<IUserRegistry, Database.AzureTables.UserRegistry>();
    builder.Services.AddScoped<IRoleRegistry, Database.AzureTables.RoleRegistry>();*/

    // Settings
    builder.Services.Configure<ResetPasswordEmailSettings>(
        builder.Configuration.GetSection("ResetPasswordEmailSettings"));
    builder.Services.Configure<ServiceBusSettings>(
        builder.Configuration.GetSection("ServiceBusSettings"));
    builder.Services.Configure<AuthenticationSettings>(
        builder.Configuration.GetSection("AuthenticationSettings"));
    builder.Services.Configure<AuthorizationSettings>(
        builder.Configuration.GetSection("AuthorizationSettings"));

    // Domain services
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IAccountService, AccountService>();
    builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IMailingService, MailingService>();
    builder.Services.AddScoped<IMailSender, ServiceBusMailingService>();

    // Authentication
    builder.Services.AddAuthorization();
    AddTokenAuthentication(builder.Services, builder.Configuration);
}

static async Task<WebApplication> Configure(WebApplication app)
{
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.MapControllers();

    await AddPredefinedRoles(app);
    return app;
}

static IServiceCollection AddTokenAuthentication(IServiceCollection services, IConfiguration configuration)
{
    var settingsSection = configuration.GetSection("AuthenticationSettings");
    var settings = settingsSection.Get<AuthenticationSettings>();
    var key = Encoding.ASCII.GetBytes(settings.Secret);

    services
        .Configure<AuthenticationSettings>(settingsSection)
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(authOptions => {
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
