using Kurrent.Client.Model;

namespace Kurrent.Client.Streams;

public static partial class StreamsClientExtensions {
    public static ValueTask<Result<Subscription, ReadError>> Subscribe(this Streams.StreamsClient client, Func<AllSubscriptionOptions, AllSubscriptionOptions>? configure = null) {
        var options = new AllSubscriptionOptions();
        options = configure?.Invoke(options) ?? options;
        return client.Subscribe(options);
    }

    public static ValueTask<Result<Subscription, ReadError>> Subscribe(this Streams.StreamsClient client, Func<StreamSubscriptionOptions, StreamSubscriptionOptions>? configure = null) {
        var options = new StreamSubscriptionOptions();
        options = configure?.Invoke(options) ?? options;
        return client.Subscribe(options);
    }
}
