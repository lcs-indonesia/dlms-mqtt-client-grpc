namespace DlmsMqttClientGrpc.Tests._Helpers;

using DlmsMqttClientGrpc.Presentation.Extensions;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

public class TestFixture
{
    private readonly IServiceCollection services;
    private Lazy<IServiceProvider> provider;
    public TestFixture()
    {
        services = new ServiceCollection();
        Configure(services);
        provider = new(() => services.BuildServiceProvider());
    }
    public void AddMockSingleton<T>(Mock<T> mock) where T : class
    {
        services.AddSingleton(mock.Object);
        if (provider.IsValueCreated) provider = new(services.BuildServiceProvider());
    }
    public void AddSingleton<T>(T obj) where T : class
    {
        services.AddSingleton<T>(obj);
        if (provider.IsValueCreated) provider = new(services.BuildServiceProvider());
    }

    public void Dispose()
    {
    }

    public T GetService<T>() where T : class => provider.Value.GetRequiredService<T>();

    private void Configure(IServiceCollection service)
    {
        Env.Load(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "DlmsMqttClientGrpc.Presentation", ".env"));
        var config = new ConfigurationBuilder().AddEnvironmentVariables(prefix: SettingsExtension.Prefix).Build();
        service.AddLogging();
        service.AddSettings(config);
        service.AddServices();
        service.AddSingleton(new Mock<IHostApplicationLifetime>().Object);
    }
}

