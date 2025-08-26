using Kurrent.Client.Streams;

namespace Kurrent.Client.PersistentSubscriptions;

[PublicAPI]
public record ListPersistentSubscriptionsOptions {
    public StreamName Stream { get; init; } = StreamName.None;
}
