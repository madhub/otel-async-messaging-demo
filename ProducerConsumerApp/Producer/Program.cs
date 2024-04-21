using EasyNetQ;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var serviceAttributes = new[] { new KeyValuePair<string, object>("service.instance.id", Environment.MachineName),
                                new KeyValuePair<string, object>("service.name","MessageProducer")};

// docker run --rm -it -p 15672:15672 -p 5672:5672 rabbitmq:3-management
// docker run --rm -it --name jaeger   -e COLLECTOR_OTLP_ENABLED=true   -p 16686:16686   -p 4317:4317   -p 4318:4318   jaegertracing/all-in-one
// docker run -p 3000:3000 -p 4317:4317 -p 4318:4318 --rm -ti grafana/otel-lgtm

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var bus = RabbitHutch.CreateBus("host=localhost");
builder.Services.AddSingleton(bus);
builder.Services.AddSingleton<TextMapPropagator, TraceContextPropagator>();
builder.Services
    .AddOpenTelemetry()
    .WithTracing(b => b
        .ConfigureResource(rb => rb.AddAttributes(serviceAttributes))
        //.AddSource("Azure.Storage.*")
        .AddSource("Queue.*")
        .AddHttpClientInstrumentation()
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());

builder.Logging.AddOpenTelemetry(b => {
    b.ParseStateValues = true;
    b.AddOtlpExporter();
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

app.Run();
