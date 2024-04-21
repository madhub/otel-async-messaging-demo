
using EasyNetQ;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;
namespace Consumer;
record class MyMessage(string Text, Dictionary<string, object> Headers);
public class BgService : BackgroundService
{
    private readonly ActivitySource _receiverSource = new("Queue.Receive");
    private readonly ActivitySource _messageSource = new("Queue.Message");
    private readonly IBus bus;
    private readonly TextMapPropagator propagator;
    private static HttpClient HttpClient = new HttpClient();

    public BgService(IBus bus, TextMapPropagator propagator)
    {
        this.bus = bus;
        this.propagator = propagator;
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.PubSub.Subscribe<string>("my_queue", HandleTextMessage);
        return Task.CompletedTask;
    }

    private void HandleTextMessage(string message)
    {
        using var serverActivity = _receiverSource.StartActivity("ReceiveAndProcess", ActivityKind.Server);

        var msgStr = System.Text.Json.JsonSerializer.Deserialize<MyMessage>(message);

        
        ProcessMessage(msgStr);

        Console.WriteLine($"Message Received"+ msgStr);
    }

    private void ProcessMessage(MyMessage msgStr)
    {
        PropagationContext ctx = propagator.Extract(default, msgStr.Headers, ExtractValue);
        var activityLink = new ActivityLink(Activity.Current?.Context ?? default);
        using var consumerActivity = _messageSource.StartActivity($"my_queue process", ActivityKind.Consumer, ctx.ActivityContext, links: new[] { activityLink });
        consumerActivity.SetTag("process.status", "sucess");
        try
        {
            var result = HttpClient.GetStringAsync("https://httpbin.org/get").GetAwaiter().GetResult();
        }
        catch (Exception exp)
        {
            Console.WriteLine(exp);

        }
        

    }

    private IEnumerable<string> ExtractValue(Dictionary<string,object> Headers, string key)
    {
        if (Headers.TryGetValue(key, out var value) )
        {
            return new[] { value.ToString() };
        }

        return Enumerable.Empty<string>();
    }

    public override void Dispose()
    {
        _messageSource.Dispose();
        _receiverSource.Dispose();
        base.Dispose();
    }
}
