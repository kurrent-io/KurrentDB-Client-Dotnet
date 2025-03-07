using System.Collections.Immutable;
using EventStore.Client;
using Kurrent.Client.Streams.DecisionMaking;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Tests.Streams.DecisionMaking.UnionTypes;

using static ShoppingCart;
using static ShoppingCart.Event;
using static ShoppingCart.Command;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:Decide")]
public class DecisionMakingWithDeciderTests(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task handles_business_logic_with_decider_and_typed_events() {
		// Given
		var shoppingCartId  = Guid.NewGuid();
		var clientId        = Guid.NewGuid();
		var shoesId         = Guid.NewGuid();
		var tShirtId        = Guid.NewGuid();
		var twoPairsOfShoes = new PricedProductItem(shoesId, 2, 100);
		var pairOfShoes     = new PricedProductItem(shoesId, 1, 100);
		var tShirt          = new PricedProductItem(tShirtId, 1, 50);

		var streamName = $"shopping_cart-{shoppingCartId}";

		var result = await Fixture.Streams.DecideAsync(
			streamName,
			new Open(clientId, DateTime.UtcNow),
			Decider
		);

		Assert.IsType<SuccessResult>(result);

		result = await Fixture.Streams.DecideAsync(
			streamName,
			new AddProductItem(twoPairsOfShoes, DateTime.UtcNow),
			Decider
		);

		Assert.IsType<SuccessResult>(result);

		result = await Fixture.Streams.DecideAsync(
			streamName,
			new AddProductItem(tShirt, DateTime.UtcNow),
			Decider
		);

		Assert.IsType<SuccessResult>(result);

		result = await Fixture.Streams.DecideAsync(
			streamName,
			new RemoveProductItem(pairOfShoes, DateTime.UtcNow),
			Decider
		);

		Assert.IsType<SuccessResult>(result);

		result = await Fixture.Streams.DecideAsync(
			streamName,
			new Confirm(DateTime.UtcNow),
			Decider
		);

		Assert.IsType<SuccessResult>(result);

		await Assert.ThrowsAsync<InvalidOperationException>(
			() =>
				Fixture.Streams.DecideAsync(
					streamName,
					new Cancel(DateTime.UtcNow),
					Decider
				)
		);
	}
}

public record PricedProductItem(
	Guid ProductId,
	int Quantity,
	decimal UnitPrice
) {
	public decimal TotalPrice => Quantity * UnitPrice;
}

public abstract record ShoppingCart {
	public abstract record Event {
		public record Opened(
			Guid ClientId,
			DateTimeOffset OpenedAt
		) : Event;

		public record ProductItemAdded(
			PricedProductItem ProductItem,
			DateTimeOffset AddedAt
		) : Event;

		public record ProductItemRemoved(
			PricedProductItem ProductItem,
			DateTimeOffset RemovedAt
		) : Event;

		public record Confirmed(
			DateTimeOffset ConfirmedAt
		) : Event;

		public record Canceled(
			DateTimeOffset CanceledAt
		) : Event;

		// This won't allow external inheritance and mimic union type in C#
		Event() { }
	}

	public record Initial : ShoppingCart;

	public record Pending(ProductItems ProductItems) : ShoppingCart;

	public record Closed : ShoppingCart;

	public static ShoppingCart Evolve(ShoppingCart state, Event @event) =>
		(state, @event) switch {
			(Initial, Opened) =>
				new Pending(ProductItems.Empty),

			(Pending(var productItems), ProductItemAdded(var productItem, _)) =>
				new Pending(productItems.Add(productItem)),

			(Pending(var productItems), ProductItemRemoved(var productItem, _)) =>
				new Pending(productItems.Remove(productItem)),

			(Pending, Confirmed) =>
				new Closed(),

			(Pending, Canceled) =>
				new Closed(),

			_ => state
		};

	public abstract record Command {
		public record Open(
			Guid ClientId,
			DateTimeOffset Now
		) : Command;

		public record AddProductItem(
			PricedProductItem ProductItem,
			DateTimeOffset Now
		) : Command;

		public record RemoveProductItem(
			PricedProductItem ProductItem,
			DateTimeOffset Now
		) : Command;

		public record Confirm(
			DateTimeOffset Now
		) : Command;

		public record Cancel(
			DateTimeOffset Now
		) : Command;

		Command() { }
	}

	public static Event[] Decide(Command command, ShoppingCart state) =>
		(state, command) switch {
			(Pending, Open) => [],

			(Initial, Open(var clientId, var now)) => [new Opened(clientId, now)],

			(Pending, AddProductItem(var productItem, var now)) => [new ProductItemAdded(productItem, now)],

			(Pending(var productItems), RemoveProductItem(var productItem, var now)) =>
				productItems.HasEnough(productItem)
					? [new ProductItemRemoved(productItem, now)]
					: throw new InvalidOperationException("Not enough product items to remove"),

			(Pending, Confirm(var now)) => [new Confirmed(now)],

			(Pending, Cancel(var now)) => [new Canceled(now)],

			_ => throw new InvalidOperationException(
				$"Cannot {command.GetType().Name} for {state.GetType().Name} shopping cart"
			)
		};

	public static readonly Decider<ShoppingCart, Command, Event> Decider = new Decider<ShoppingCart, Command, Event>(
		Decide,
		Evolve,
		() => new Initial()
	);
}

public record ProductItems(ImmutableDictionary<string, int> Items) {
	public static ProductItems Empty => new(ImmutableDictionary<string, int>.Empty);

	public ProductItems Add(PricedProductItem productItem) =>
		IncrementQuantity(Key(productItem), productItem.Quantity);

	public ProductItems Remove(PricedProductItem productItem) =>
		IncrementQuantity(Key(productItem), -productItem.Quantity);

	public bool HasEnough(PricedProductItem productItem) =>
		Items.TryGetValue(Key(productItem), out var currentQuantity) && currentQuantity >= productItem.Quantity;

	static string Key(PricedProductItem pricedProductItem) =>
		$"{pricedProductItem.ProductId}_{pricedProductItem.UnitPrice}";

	ProductItems IncrementQuantity(string key, int quantity) =>
		new(Items.SetItem(key, Items.TryGetValue(key, out var current) ? current + quantity : quantity));
}
