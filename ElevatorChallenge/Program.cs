using ElevatorChallenge.ElevatorClasses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext,config) =>
    {
        config.AddJsonFile("appsettings.json",optional: false,reloadOnChange: true);
    })
    .ConfigureServices((context,services) =>
    {
        DependencyInjectionSetup.AddElevatorServices(services,context.Configuration);
    });

var host = builder.Build();

var Simulation = host.Services.GetRequiredService<ISimulationSetupService>();
Simulation.GetBuildingUpAndRunning();

