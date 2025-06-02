using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorChallenge.ElevatorClasses;

/// <summary>
/// Provides extension methods for registering elevator-related services and dependencies in the dependency injection container.
/// </summary>
public static class DependencyInjectionSetup
{
    /// <summary>
    /// Registers all elevator system services, including logging, factories, and simulation setup, into the provided <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to which services will be added.</param>
    /// <param name="configuration">The application configuration used for service setup and Serilog configuration.</param>
    public static void AddElevatorServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Serilog from configuration
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        // Register Serilog as the logging provider
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register the ConfigurationWrapper to provide configuration values and helps mock configuration in tests
        services.AddSingleton<IConfigurationWrapper, ConfigurationWrapper>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ConfigurationWrapper>>();
            return new ConfigurationWrapper(logger, configuration);
        });

        // Register the ElevatorFactory with the required dependencies
        services.AddSingleton<IElevatorFactory, ElevatorFactory>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<ElevatorFactory>>();
            var elevatorLogger = provider.GetRequiredService<ILogger<Elevator>>();
            var configurationWrapper = provider.GetRequiredService<IConfigurationWrapper>();
            return new ElevatorFactory(logger, elevatorLogger,configurationWrapper);
        });

        // Register the FloorFactory with the required dependencies
        services.AddSingleton<IFloorFactory, FloorFactory>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<FloorFactory>>();
            var floorLogger = provider.GetRequiredService<ILogger<Floor>>();
            var configurationWrapper = provider.GetRequiredService<IConfigurationWrapper>();
            return new FloorFactory(logger, floorLogger,configurationWrapper);
        });

        // Register the Building class with its dependencies
        services.AddSingleton<IBuilding, Building>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<Building>>();
            return new Building(logger);
        });

        services.AddSingleton<ISimulationSetupService, SimulationSetupService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<SimulationSetupService>>();
            var elevatorFactory = provider.GetRequiredService<IElevatorFactory>();
            var floorFactory = provider.GetRequiredService<IFloorFactory>();
            var building = provider.GetRequiredService<IBuilding>();
            return new SimulationSetupService(logger, configuration, floorFactory, elevatorFactory, building);
        });
    }
}
