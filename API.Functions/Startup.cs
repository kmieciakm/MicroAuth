using API.Functions;
using AzureTables;
using Database.AzureTables;
using Domain.Contracts;
using Domain.Infrastructure;
using Domain.Models;
using Domain.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace API.Functions;

internal class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        // Azure Table Storage setup
        builder.Services.AddOptions<AzureStorageSettings>()
            .Configure<IConfiguration>((azureSettings, configuration) => {
                BindConfiguration(azureSettings, configuration);
            });

        builder.Services.AddSingleton<IUsersRolesTable, UsersRolesTable>();
        builder.Services.AddSingleton<IRoleTable, RolesTable>();
        builder.Services.AddSingleton<IUserTable, UsersTable>();
        builder.Services.AddScoped<IUserRegistry, UserRegistry>();
        builder.Services.AddScoped<IRoleRegistry, RoleRegistry>();

        // Settings
        builder.Services.AddOptions<AuthenticationSettings>()
            .Configure<IConfiguration>((authSettings, configuration) => {
                BindConfiguration(authSettings, configuration);
            });

        builder.Services.AddOptions<AuthorizationSettings>()
            .Configure<IConfiguration>((authSettings, configuration) => {
                BindConfiguration(authSettings, configuration);
            });

        // Domain services
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
        builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    }

    private T BindConfiguration<T>(T option, IConfiguration configuration) where T : class, new()
    {
        var masterName = option.GetType().Name;
        var properties = option.GetType().GetProperties();
        foreach (var property in properties)
        {
            var propertyConfigName = $"{masterName}_{property.Name}";
            string configValue = configuration[propertyConfigName];
            dynamic value = property.PropertyType.Name switch
            {
                nameof(Boolean) => bool.Parse(configValue),
                nameof(Int32) => int.Parse(configValue),
                _ => configValue
            };
            if (value is not null)
            {
                property.SetValue(option, value, null);
            }
        }
        return option;
    }

    public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
    {
        builder.ConfigurationBuilder
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile("local.settings.json", true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .AddEnvironmentVariables()
            .Build();
    }
}
