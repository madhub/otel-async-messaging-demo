// See https://aka.ms/new-console-template for more information

using Consumer;
using EasyNetQ;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;



CreateHostBuilder(args).Build().Run();

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureServices((hostContext, services) =>
        {
            var serviceAttributes = new[] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName),
                                new KeyValuePair<string, object>("service.name","MessageConsumer")};
            services.AddLogging();
            var bus = RabbitHutch.CreateBus("host=localhost");
            services.AddSingleton(bus);
            services.AddSingleton<TextMapPropagator, TraceContextPropagator>();
            services.AddHostedService<BgService>();
            services.AddOpenTelemetry()
            .WithTracing(b => b
                .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
                .AddSource("Azure.Storage.*")
                .AddSource("Queue.*")
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());
        })
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            var env = hostingContext.HostingEnvironment;

            config.AddEnvironmentVariables();
            config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            config.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
        });