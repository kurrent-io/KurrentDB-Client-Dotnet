using EventStore.Client;
using Kurrent.Client.Streams.DecisionMaking;
using Kurrent.Client.Streams.GettingState;

namespace Kurrent.Client.Tests.Streams.DecisionMaking.StateBased;

[Trait("Category", "Target:Streams")]
[Trait("Category", "Operation:GetState")]
public class DecisionMakingWithAggregateStore(ITestOutputHelper output, KurrentPermanentFixture fixture)
	: KurrentPermanentTests<KurrentPermanentFixture>(output, fixture) {
	[RetryFact]
	public async Task handles_business_logic_with_aggregate_and_aggregate_store() {
		// Given
		var shoppingCartId = Guid.NewGuid();
		var clientId       = Guid.NewGuid();
		var shoesId        = Guid.NewGuid();
		var tShirtId       = Guid.NewGuid();
		var twoPairsOfShoes = new PricedProductItem {
			ProductId = shoesId,
			Quantity  = 2,
			UnitPrice = 100
		};

		var pairOfShoes = new PricedProductItem {
			ProductId = shoesId,
			Quantity  = 1,
			UnitPrice = 100
		};

		var tShirt = new PricedProductItem {
			ProductId = tShirtId,
			Quantity  = 1,
			UnitPrice = 50
		};

		var streamName = $"shopping_cart-{shoppingCartId}";

		var stateBuilder = StateBuilder.For(ShoppingCart.Initial);

		var aggregateStore = new AggregateStore<ShoppingCart>(
			Fixture.Streams,
			new AggregateStoreOptions<ShoppingCart> { StateBuilder = stateBuilder }
		);

		var result = await aggregateStore.AddAsync(
			streamName,
			ShoppingCart.Open(shoppingCartId, clientId, DateTime.UtcNow)
		);

		Assert.IsType<SuccessResult>(result);
		
		result = await aggregateStore.HandleAsync(
			streamName,
			state => state.AddProductItem(twoPairsOfShoes, DateTime.UtcNow)
		);

		Assert.IsType<SuccessResult>(result);

		result = await aggregateStore.HandleAsync(
			streamName,
			state => state.AddProductItem(tShirt, DateTime.UtcNow)
		);

		Assert.IsType<SuccessResult>(result);

		result = await aggregateStore.HandleAsync(
			streamName,
			state => state.RemoveProductItem(pairOfShoes, DateTime.UtcNow)
		);

		Assert.IsType<SuccessResult>(result);
		
		result = await aggregateStore.HandleAsync(
			streamName,
			state => state.Confirm(DateTime.UtcNow)
		);

		Assert.IsType<SuccessResult>(result);

		await Assert.ThrowsAsync<InvalidOperationException>(
			() =>
				aggregateStore.HandleAsync(
					streamName,
					state => state.Cancel(DateTime.UtcNow)
				)
		);
	}
}

public record ShoppingCartOpened(
	Guid ShoppingCartId,
	Guid ClientId,
	DateTimeOffset OpenedAt
);

public record ProductItemAddedToShoppingCart(
	Guid ShoppingCartId,
	PricedProductItem ProductItem,
	DateTimeOffset AddedAt
);

public record ProductItemRemovedFromShoppingCart(
	Guid ShoppingCartId,
	PricedProductItem ProductItem,
	DateTimeOffset RemovedAt
);

public record ShoppingCartConfirmed(
	Guid ShoppingCartId,
	DateTimeOffset ConfirmedAt
);

public record ShoppingCartCanceled(
	Guid ShoppingCartId,
	DateTimeOffset CanceledAt
);

public class PricedProductItem {
	public Guid    ProductId  { get; set; }
	public decimal UnitPrice  { get; set; }
	public int     Quantity   { get; set; }
	public decimal TotalPrice => Quantity * UnitPrice;
}

public class ShoppingCart : Aggregate {
	public Guid                     Id           { get; private set; }
	public ShoppingCartStatus       Status       { get; private set; }
	public IList<PricedProductItem> ProductItems { get; } = new List<PricedProductItem>();

	public bool IsClosed => ShoppingCartStatus.Closed.HasFlag(Status);

	public static ShoppingCart Open(Guid cartId, Guid clientId, DateTimeOffset now) =>
		new(cartId, clientId, now);

	public static ShoppingCart Initial() => new();

	ShoppingCart(Guid id, Guid clientId, DateTimeOffset now) {
		var @event = new ShoppingCartOpened(id, clientId, now);

		Enqueue(@event);
	}

	//just for default creation of empty object
	ShoppingCart() { }

	void Apply(ShoppingCartOpened opened) {
		Id     = opened.ShoppingCartId;
		Status = ShoppingCartStatus.Pending;
	}

	public void AddProductItem(PricedProductItem productItem, DateTimeOffset now) {
		if (IsClosed)
			throw new InvalidOperationException($"Adding product item for cart in '{Status}' status is not allowed.");

		var @event = new ProductItemAddedToShoppingCart(Id, productItem, now);

		Enqueue(@event);
	}

	void Apply(ProductItemAddedToShoppingCart productItemAdded) {
		var (_, pricedProductItem, _) = productItemAdded;
		var productId     = pricedProductItem.ProductId;
		var quantityToAdd = pricedProductItem.Quantity;

		var current = ProductItems.SingleOrDefault(pi => pi.ProductId == productId);

		if (current == null)
			ProductItems.Add(pricedProductItem);
		else
			current.Quantity += quantityToAdd;
	}

	public void RemoveProductItem(PricedProductItem productItemToBeRemoved, DateTimeOffset now) {
		if (IsClosed)
			throw new InvalidOperationException($"Removing product item for cart in '{Status}' status is not allowed.");

		if (!HasEnough(productItemToBeRemoved))
			throw new InvalidOperationException("Not enough product items to remove");

		var @event = new ProductItemRemovedFromShoppingCart(Id, productItemToBeRemoved, now);

		Enqueue(@event);
	}

	bool HasEnough(PricedProductItem productItem) {
		var currentQuantity = ProductItems.Where(pi => pi.ProductId == productItem.ProductId)
			.Select(pi => pi.Quantity)
			.FirstOrDefault();

		return currentQuantity >= productItem.Quantity;
	}

	void Apply(ProductItemRemovedFromShoppingCart productItemRemoved) {
		var (_, pricedProductItem, _) = productItemRemoved;
		var productId        = pricedProductItem.ProductId;
		var quantityToRemove = pricedProductItem.Quantity;

		var current = ProductItems.Single(pi => pi.ProductId == productId);

		if (current.Quantity == quantityToRemove)
			ProductItems.Remove(current);
		else
			current.Quantity -= quantityToRemove;
	}

	public void Confirm(DateTimeOffset now) {
		if (IsClosed)
			throw new InvalidOperationException($"Confirming cart in '{Status}' status is not allowed.");

		if (ProductItems.Count == 0)
			throw new InvalidOperationException("Cannot confirm empty shopping cart");

		var @event = new ShoppingCartConfirmed(Id, now);

		Enqueue(@event);
	}

	void Apply(ShoppingCartConfirmed confirmed) {
		Status = ShoppingCartStatus.Confirmed;
	}

	public void Cancel(DateTimeOffset now) {
		if (IsClosed)
			throw new InvalidOperationException($"Canceling cart in '{Status}' status is not allowed.");

		var @event = new ShoppingCartCanceled(Id, now);

		Enqueue(@event);
	}

	void Apply(ShoppingCartCanceled canceled) {
		Status = ShoppingCartStatus.Canceled;
	}

	public override void Apply(object @event) {
		switch (@event) {
			case ShoppingCartOpened opened:
				Apply(opened);
				return;

			case ProductItemAddedToShoppingCart productItemAdded:
				Apply(productItemAdded);
				return;

			case ProductItemRemovedFromShoppingCart productItemRemoved:
				Apply(productItemRemoved);
				return;

			case ShoppingCartConfirmed confirmed:
				Apply(confirmed);
				return;

			case ShoppingCartCanceled canceled:
				Apply(canceled);
				return;
		}
	}
}

public enum ShoppingCartStatus {
	Pending   = 1,
	Confirmed = 2,
	Canceled  = 4,

	Closed = Confirmed | Canceled
}
