using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using TranzrMoves.Worker;
using TranzrMoves.Worker.HostedServices;

namespace TranzrMoves.UnitTests.Worker;

public sealed class WorkerHostConfigurationTests
{
    [Theory]
    [InlineData("Scheduler", WorkerRole.Scheduler)]
    [InlineData("processor", WorkerRole.Processor)]
    [InlineData("All", WorkerRole.All)]
    public void GetWorkerRole_ParsesValidValues(string configured, WorkerRole expected)
    {
        var configuration = BuildConfiguration(configured);
        var environment = CreateEnvironment(Environments.Development);

        var role = WorkerHostConfiguration.GetWorkerRole(configuration, environment);

        role.Should().Be(expected);
    }

    [Fact]
    public void GetWorkerRole_WhenMissing_Throws()
    {
        var configuration = new ConfigurationBuilder().Build();
        var environment = CreateEnvironment(Environments.Production);

        var act = () => WorkerHostConfiguration.GetWorkerRole(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Worker:Role*required*");
    }

    [Fact]
    public void GetWorkerRole_WhenInvalid_Throws()
    {
        var configuration = BuildConfiguration("InvalidRole");
        var environment = CreateEnvironment(Environments.Production);

        var act = () => WorkerHostConfiguration.GetWorkerRole(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid*Worker:Role*");
    }

    [Fact]
    public void GetWorkerRole_All_InProduction_Throws()
    {
        var configuration = BuildConfiguration("All");
        var environment = CreateEnvironment(Environments.Production);

        var act = () => WorkerHostConfiguration.GetWorkerRole(configuration, environment);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Development*");
    }

    [Theory]
    [InlineData(WorkerRole.Scheduler, false)]
    [InlineData(WorkerRole.Processor, true)]
    [InlineData(WorkerRole.All, true)]
    public void RequiresStripe_ReturnsExpected(WorkerRole role, bool expected)
    {
        WorkerHostConfiguration.RequiresStripe(role).Should().Be(expected);
    }

    [Fact]
    public void RegisterPayLaterHostedServices_Scheduler_RegistersListenerAndRecovery()
    {
        var services = new ServiceCollection();

        WorkerHostConfiguration.RegisterPayLaterHostedServices(services, WorkerRole.Scheduler);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(BalanceChargeExpiryListener));
        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(BalanceChargeRecoveryWorker));
    }

    [Fact]
    public void RegisterPayLaterHostedServices_Processor_RegistersNoPayLaterHostedServices()
    {
        var services = new ServiceCollection();

        WorkerHostConfiguration.RegisterPayLaterHostedServices(services, WorkerRole.Processor);

        services.Should().NotContain(d => d.ImplementationType == typeof(BalanceChargeExpiryListener));
        services.Should().NotContain(d => d.ImplementationType == typeof(BalanceChargeRecoveryWorker));
    }

    private static IConfiguration BuildConfiguration(string role) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [WorkerHostConfiguration.RoleConfigurationKey] = role
            })
            .Build();

    private static IHostEnvironment CreateEnvironment(string environmentName)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }
}
