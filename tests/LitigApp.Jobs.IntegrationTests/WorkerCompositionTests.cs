using FluentAssertions;
using LitigApp.Application;
using LitigApp.Application.Common.Abstractions;
using LitigApp.Infrastructure;
using LitigApp.Jobs.ProcessSyncJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LitigApp.Jobs.IntegrationTests;

/// <summary>
/// Guards the worker-role composition: worker-only jobs (BulkImportJob) must have every
/// dependency registered in the worker role — this catches "job depends on an api-only
/// service" bugs that the WebApplicationFactory tests (which run as the api role) miss.
/// Inspects the descriptors only, so no database connection is made.
/// </summary>
public class WorkerCompositionTests
{
    private static IServiceCollection BuildWorkerServices()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=x;Username=x;Password=x",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(config, isWorker: true);
        services.AddApplication(isWorker: true);
        services.AddJobs(config, isWorker: true);
        return services;
    }

    [Fact]
    public void WorkerRole_RegistersBulkImportJobAndItsCreatorDependency()
    {
        var services = BuildWorkerServices();

        services.Should().Contain(d => d.ServiceType == typeof(BulkImportJob),
            "the worker drains the bulk_import queue");
        services.Should().Contain(d => d.ServiceType == typeof(IProcessCreator),
            "BulkImportJob depends on IProcessCreator and runs in the worker role");
    }

    [Theory]
    [InlineData(typeof(IProcessCreator))]
    [InlineData(typeof(ICurrentUserService))]
    [InlineData(typeof(IImportJobRepository))]
    [InlineData(typeof(IOutboxRepository))]
    [InlineData(typeof(ISyncJobScheduler))]
    public void WorkerRole_RegistersSharedCreationDependencies(Type serviceType)
    {
        var services = BuildWorkerServices();

        services.Should().Contain(d => d.ServiceType == serviceType);
    }
}
