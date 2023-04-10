using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Projekt.Extensions;

public static class ServiceRegistrationExtensions
{
    /// <summary>
    /// Adds services to dependency injection container
    /// Use it if you are using creating web application using Microsoft's Dependency Injection container
    /// Be aware that connection string has to be specified in appsettings.json under section "ConnectionStrings" and key "BD2_XML"
    /// </summary>
    /// <param name="services">Services container</param>
    /// <param name="configuration">Configuration to retrieve database connection string</param>
    /// <returns>Services container</returns>
    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DB2_XML");
        if (connectionString == null)
            throw new ArgumentException("Please add connection string to appsettings.json");
        
        services.AddScoped<IXmlService, XmlService>(c => new XmlService(connectionString));

        return services;
    }
}