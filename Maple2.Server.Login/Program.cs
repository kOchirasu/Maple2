using Autofac;
using Autofac.Extensions.DependencyInjection;
using Maple2.Server.Login;
using Microsoft.Extensions.Hosting;

await new HostBuilder()
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureServices(Startup.ConfigureServices)
    .ConfigureContainer<ContainerBuilder>(Startup.ConfigureContainer)
    .RunConsoleAsync();
