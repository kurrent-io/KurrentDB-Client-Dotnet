using KurrentDB.Client;

namespace Kurrent.Client.Tests.Options;

public class KurrentClientGossipOptionsTests {
    [Test]
    public void default_gossip_options_have_expected_values() {
        // Create default options
        var options = KurrentClientGossipOptions.Default;

        // Verify default values
        options.MaxDiscoverAttempts.ShouldBe(10);
        options.DiscoveryInterval.ShouldBe(TimeSpan.FromMilliseconds(100));
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(5));
        options.ReadPreference.ShouldBe(NodePreference.Random);
    }

    [Test]
    public void creating_custom_gossip_options_stores_expected_values() {
        // Create custom gossip options
        var options = new KurrentClientGossipOptions {
            MaxDiscoverAttempts = 15,
            DiscoveryInterval   = TimeSpan.FromMilliseconds(200),
            Timeout             = TimeSpan.FromSeconds(10),
            ReadPreference      = NodePreference.Leader
        };

        // Verify custom values were stored correctly
        options.MaxDiscoverAttempts.ShouldBe(15);
        options.DiscoveryInterval.ShouldBe(TimeSpan.FromMilliseconds(200));
        options.Timeout.ShouldBe(TimeSpan.FromSeconds(10));
        options.ReadPreference.ShouldBe(NodePreference.Leader);
    }

    [Test]
    public void gossip_options_with_small_timeout_are_valid() {
        // Create options with minimal timeouts
        var options = new KurrentClientGossipOptions {
            DiscoveryInterval = TimeSpan.FromMilliseconds(10),
            Timeout           = TimeSpan.FromMilliseconds(50)
        };

        // Verify these extreme but valid values are stored correctly
        options.DiscoveryInterval.ShouldBe(TimeSpan.FromMilliseconds(10));
        options.Timeout.ShouldBe(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    public void default_constructor_creates_same_values_as_default_property() {
        // Create with default constructor
        var constructedOptions = new KurrentClientGossipOptions();

        // Get the static Default instance
        var defaultOptions = KurrentClientGossipOptions.Default;

        // Verify they have the same values
        constructedOptions.MaxDiscoverAttempts.ShouldBe(defaultOptions.MaxDiscoverAttempts);
        constructedOptions.DiscoveryInterval.ShouldBe(defaultOptions.DiscoveryInterval);
        constructedOptions.Timeout.ShouldBe(defaultOptions.Timeout);
        constructedOptions.ReadPreference.ShouldBe(defaultOptions.ReadPreference);
    }
}
