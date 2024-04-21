using EasyNetQ;
using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using System.Diagnostics;

namespace Producer.Controllers;

record class MyMessage(string Text, Dictionary<string, object> Headers);
[ApiController]
[Route("[controller]")]
public class SendController : Controller
{
    private static readonly ActivitySource Source = new("Queue.Publish");
    private readonly IBus bus;
    private readonly TextMapPropagator propagator;
    public SendController(IBus bus, TextMapPropagator propagator)
    {
        this.bus = bus;
        this.propagator = propagator;
    }
    [HttpPost]
    public ActionResult Send([FromBody] object data)
    {
        using var act = Source.StartActivity($"my_queue publish", ActivityKind.Producer);
        Dictionary<string, object> Headers = new();
        PropagationContext context = new(act.Context, Baggage.Current);
        propagator.Inject(context, Headers, static (m, k, v) => m[k] = v);


        var msg = new MyMessage("Hello " + DateTime.Now.ToString(), Headers);
        var msgStr = System.Text.Json.JsonSerializer.Serialize(msg);

        bus.PubSub.Publish<string>(msgStr,
            option =>
            {
                option.WithTopic("my_queue");
            });
        return Ok();
    }
}
