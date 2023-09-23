// Install `NATS.Client.Core` from NuGet.
using System.Diagnostics;
using NATS.Client.Core;

var stopwatch = Stopwatch.StartNew();
void Log(string log) => Console.WriteLine($"{stopwatch.Elapsed} {log}");

// `NATS_URL` environment variable can be used to pass the locations of the NATS servers.
var url = Environment.GetEnvironmentVariable("NATS_URL") ?? "127.0.0.1:4222";
Log($"[CON] Connecting to {url}");

// Connect to NATS server. Since connection is disposable at the end of our scope we should flush
// our buffers and close connection cleanly.
var opts = NatsOpts.Default with { Url = url };
await using var nats = new NatsConnection(opts);

// Subscribe to a subject and start waiting for messages in the background.
await using var sub = await nats.SubscribeAsync<Order>("orders.>");

Log("[SUB] waiting for messages...");
var task = Task.Run(async () =>
{
    await foreach (var msg in sub.Msgs.ReadAllAsync())
    {
        var order = msg.Data;
        Log($"[SUB] received {msg.Subject}: {order}");
    }

    Log($"[SUB] unsubscribed");
});

// Let's publish a few orders.
for (int i = 0; i < 5; i++)
{
    Log($"[PUB] publishing order {i}...");
    await nats.PublishAsync($"orders.new.{i}", new Order(OrderId: i));
    await Task.Delay(500);
}

// We can unsubscribe now all orders are published. Unsubscribing or disposing the subscription
// should complete the message loop and exit the background task cleanly.
await sub.UnsubscribeAsync();
await task;

// That's it! We saw how we can subscribe to a subject and publish messages that would
// be seen by the subscribers based on matching subjects.
Log("Bye!");

public record Order(int OrderId);