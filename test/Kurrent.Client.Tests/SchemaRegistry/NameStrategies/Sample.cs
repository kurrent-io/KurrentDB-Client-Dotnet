// ReSharper disable CheckNamespace

namespace Company.Logistics.Events {
	public record OrderShipped {
		public record Details(string ItemId, int Quantity);
		public record Address(string Street, string City, string ZipCode);
	}
}

record Order(string OrderId, string Product, int Quantity);
