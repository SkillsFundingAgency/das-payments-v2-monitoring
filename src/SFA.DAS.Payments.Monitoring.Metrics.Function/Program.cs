using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SFA.DAS.Payments.Application.Infrastructure.Ioc.Modules;
using LocalModules = SFA.DAS.Payments.Monitoring.Metrics.Function.Infrastructure.IoC.Modules;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>(builder =>
    {
        builder.RegisterModule<TelemetryModule>();
        builder.RegisterModule<LoggingModule>();
        builder.RegisterModule<LocalModules.ConfigurationModule>();
        builder.RegisterModule<LocalModules.FunctionsModule>();
    })
    .Build();

host.Run();
