// using System.Net;
// using Microsoft.Extensions.Logging.Abstractions;
//
// namespace Kurrent.Client.Tests;
//
// public class ResourcePoolTests {
// 	[Test]
// 	public async Task GetAsync_ShouldCreateResourceOnFirstCall() {
// 		// Arrange
// 		var factoryCalled = false;
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				factoryCalled = true;
// 				return new ValueTask<TestResource>(new TestResource(config));
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Act
// 		var resource = await pool.GetAsync();
//
// 		// Assert
// 		factoryCalled.ShouldBeTrue();
// 		resource.ShouldNotBeNull();
// 		resource.Config.ShouldBe("test-config");
// 	}
//
// 	[Test]
// 	public async Task GetAsync_ShouldReturnCachedResource_WhenCalledMultipleTimes() {
// 		// Arrange
// 		var factoryCallCount = 0;
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				factoryCallCount++;
// 				return new ValueTask<TestResource>(new TestResource(config));
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Act
// 		var resource1 = await pool.GetAsync();
// 		var resource2 = await pool.GetAsync();
//
// 		// Assert
// 		factoryCallCount.ShouldBe(1);
// 		resource1.ShouldBeSameAs(resource2);
// 	}
//
// 	[Test]
// 	public async Task ResetAsync_ShouldCreateNewResource() {
// 		// Arrange
// 		var factoryCallCount = 0;
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				factoryCallCount++;
// 				return new ValueTask<TestResource>(new TestResource(config) { Id = factoryCallCount });
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Act
// 		var resource1 = await pool.GetAsync();
// 		await pool.ResetAsync();
// 		var resource2 = await pool.GetAsync();
//
// 		// Assert
// 		factoryCallCount.ShouldBe(2);
// 		resource1.ShouldNotBeSameAs(resource2);
// 		resource1.Id.ShouldBe(1);
// 		resource2.Id.ShouldBe(2);
// 	}
//
// 	[Test]
// 	public async Task GetAsync_ShouldThrowException_WhenPoolIsDisposed() {
// 		// Arrange
// 		var pool = new ResourcePool<string, TestResource>(
// 			(config, _) => new ValueTask<TestResource>(new TestResource(config)),
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		await pool.DisposeAsync();
//
// 		// Act & Assert
// 		await Should.ThrowAsync<ObjectDisposedException>(async () =>
// 			await pool.GetAsync()
// 		);
// 	}
//
// 	[Test]
// 	public async Task ResetAsync_ShouldThrowException_WhenPoolIsDisposed() {
// 		// Arrange
// 		var pool = new ResourcePool<string, TestResource>(
// 			(config, _) => new ValueTask<TestResource>(new TestResource(config)),
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		await pool.DisposeAsync();
//
// 		// Act & Assert
// 		await Should.ThrowAsync<ObjectDisposedException>(async () =>
// 			await pool.ResetAsync()
// 		);
// 	}
//
// 	[Test]
// 	public async Task GetAsync_ShouldThrowException_WhenFactoryThrows() {
// 		// Arrange
// 		var exception = new InvalidOperationException("Factory failed");
// 		var factory   = new ResourceFactory<string, TestResource>((_, _) => { throw exception; });
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(10), // Short delay for test
// 			NullLogger.Instance
// 		);
//
// 		// Act & Assert
// 		var thrownException = await Should.ThrowAsync<InvalidOperationException>(async () =>
// 			await pool.GetAsync()
// 		);
//
// 		thrownException.ShouldBeSameAs(exception);
// 	}
//
// 	[Test]
// 	public async Task GetAsync_ShouldHonorCancellationToken() {
// 		// Arrange
// 		var tokenUsed = false;
// 		var factory = new ResourceFactory<string, TestResource>(async (_, ct) => {
// 				// Set up a long-running operation that checks the token
// 				var tcs = new TaskCompletionSource<TestResource>();
// 				var registration = ct.Register(() => {
// 						tokenUsed = true;
// 						tcs.TrySetCanceled();
// 					}
// 				);
//
// 				try {
// 					return await tcs.Task;
// 				}
// 				finally {
// 					await registration.DisposeAsync();
// 				}
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Prepare cancellation token
// 		var cts = new CancellationTokenSource();
//
// 		// Act
// 		var getTask = pool.GetAsync(cts.Token);
//
// 		// Cancel the operation
// 		await cts.CancelAsync();
//
// 		// Assert
// 		await Should.ThrowAsync<OperationCanceledException>(async () =>
// 			await getTask
// 		);
//
// 		tokenUsed.ShouldBeTrue();
// 	}
//
// 	[Test]
// 	public async Task ResetWithConfigAsync_ShouldCreateNewResourceWithNewConfig() {
// 		// Arrange
// 		var configs = new List<string>();
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				configs.Add(config);
// 				return new ValueTask<TestResource>(new TestResource(config));
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"initial-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Act
// 		var resource1 = await pool.GetAsync();
// 		await pool.ResetWithConfigAsync("new-config");
// 		var resource2 = await pool.GetAsync();
//
// 		// Assert
// 		configs.Count.ShouldBe(2);
// 		configs[0].ShouldBe("initial-config");
// 		configs[1].ShouldBe("new-config");
// 		resource1.Config.ShouldBe("initial-config");
// 		resource2.Config.ShouldBe("new-config");
// 		resource1.ShouldNotBeSameAs(resource2);
// 	}
//
// 	[Test]
// 	public async Task ResetWithConfigAsync_ShouldThrowException_WhenPoolIsDisposed() {
// 		// Arrange
// 		var pool = new ResourcePool<string, TestResource>(
// 			(config, _) => new ValueTask<TestResource>(new TestResource(config)),
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		await pool.DisposeAsync();
//
// 		// Act & Assert
// 		await Should.ThrowAsync<ObjectDisposedException>(async () =>
// 			await pool.ResetWithConfigAsync("new-config")
// 		);
// 	}
//
// 	[Test]
// 	public async Task ResetWithConfigAsync_ShouldHonorCancellationToken() {
// 		// Arrange
// 		var tokenUsed = false;
// 		var factory = new ResourceFactory<string, TestResource>(async (_, ct) => {
// 				// Set up a long-running operation that checks the token
// 				var tcs = new TaskCompletionSource<TestResource>();
// 				var registration = ct.Register(() => {
// 						tokenUsed = true;
// 						tcs.TrySetCanceled();
// 					}
// 				);
//
// 				try {
// 					return await tcs.Task;
// 				}
// 				finally {
// 					await registration.DisposeAsync();
// 				}
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Prepare cancellation token
// 		var cts = new CancellationTokenSource();
//
// 		// Act
// 		var resetTask = pool.ResetWithConfigAsync("new-config", cts.Token);
//
// 		// Cancel the operation
// 		await cts.CancelAsync();
//
// 		// Assert
// 		await Should.ThrowAsync<OperationCanceledException>(async () =>
// 			await resetTask
// 		);
//
// 		tokenUsed.ShouldBeTrue();
// 	}
//
// 	[Test]
// 	public async Task ForceReconnect_ShouldUseSpecifiedLeaderEndpoint() {
// 		// Arrange
// 		var configs = new List<ReconnectionRequired>();
// 		var factory = new ResourceFactory<ReconnectionRequired, TestResource>((config, _) => {
// 				configs.Add(config);
// 				return new ValueTask<TestResource>(new TestResource(config.ToString()));
// 			}
// 		);
//
// 		var pool = new ResourcePool<ReconnectionRequired, TestResource>(
// 			factory,
// 			ReconnectionRequired.Rediscover.Instance,
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		var resource1 = await pool.GetAsync();
//
// 		// Act
// 		var endpoint = new DnsEndPoint("test.example.com", 12345);
// 		await pool.ResetWithConfigAsync(new ReconnectionRequired.NewLeader(endpoint));
// 		var resource2 = await pool.GetAsync();
//
// 		// Assert
// 		configs.Count.ShouldBe(2);
// 		configs[0].ShouldBeOfType<ReconnectionRequired.Rediscover>();
// 		configs[1].ShouldBeOfType<ReconnectionRequired.NewLeader>();
//
// 		var leaderConfig = configs[1] as ReconnectionRequired.NewLeader;
// 		leaderConfig.ShouldNotBeNull();
// 		leaderConfig.EndPoint.Host.ShouldBe("test.example.com");
// 		leaderConfig.EndPoint.Port.ShouldBe(12345);
//
// 		resource1.Config.ShouldBe(ReconnectionRequired.Rediscover.Instance.ToString());
// 		resource2.Config.ShouldContain("test.example.com");
// 		resource2.Config.ShouldContain("12345");
// 	}
//
// 	[Test]
// 	public async Task DisposeAsync_ShouldDisposeResource_WhenResourceImplementsIDisposable() {
// 		// Arrange
// 		var disposableResource = new DisposableTestResource("test-config");
// 		var factory = new ResourceFactory<string, DisposableTestResource>((_, _) =>
// 			new ValueTask<DisposableTestResource>(disposableResource)
// 		);
//
// 		var pool = new ResourcePool<string, DisposableTestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Access the resource to ensure it's created
// 		await pool.GetAsync();
//
// 		// Act
// 		await pool.DisposeAsync();
//
// 		// Assert
// 		disposableResource.IsDisposed.ShouldBeTrue();
// 	}
//
// 	[Test]
// 	public async Task DisposeAsync_ShouldDisposeResource_WhenResourceImplementsIAsyncDisposable() {
// 		// Arrange
// 		var asyncDisposableResource = new AsyncDisposableTestResource("test-config");
// 		var factory = new ResourceFactory<string, AsyncDisposableTestResource>((_, _) =>
// 			new ValueTask<AsyncDisposableTestResource>(asyncDisposableResource)
// 		);
//
// 		var pool = new ResourcePool<string, AsyncDisposableTestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(100),
// 			NullLogger.Instance
// 		);
//
// 		// Access the resource to ensure it's created
// 		await pool.GetAsync();
//
// 		// Act
// 		await pool.DisposeAsync();
//
// 		// Assert
// 		asyncDisposableResource.IsDisposed.ShouldBeTrue();
// 	}
//
// 	[Test]
// 	public async Task ExecuteWithRetryAsync_ShouldRetry_WhenTransientErrorOccurs() {
// 		// Arrange
// 		var factoryCallCount = 0;
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				factoryCallCount++;
// 				return new ValueTask<TestResource>(new TestResource(config) { Id = factoryCallCount });
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(10), // Short delay for test
// 			NullLogger.Instance
// 		);
//
// 		var operationCallCount = 0;
//
// 		// Act
// 		var result = await pool.ExecuteWithRetryAsync(
// 			 resource => {
// 				operationCallCount++;
// 				if (operationCallCount < 3)
// 					throw new TransientException("Temporary failure");
//
// 				return Task.FromResult(resource.Id);
// 			},
// 			ex => ex is TransientException,
// 			3
// 		);
//
// 		// Assert
// 		operationCallCount.ShouldBe(3);
// 		factoryCallCount.ShouldBe(3);
// 		result.ShouldBe(3);
// 	}
//
// 	[Test]
// 	public async Task RegisterPool_ShouldAllowResourceToResetItsPool() {
// 		// Arrange
// 		var factoryCallCount = 0;
// 		var factory = new ResourceFactory<string, TestResource>((config, _) => {
// 				factoryCallCount++;
// 				return new ValueTask<TestResource>(new TestResource(config) { Id = factoryCallCount });
// 			}
// 		);
//
// 		var pool = new ResourcePool<string, TestResource>(
// 			factory,
// 			"test-config",
// 			TimeSpan.FromMilliseconds(10), // Short delay for test
// 			NullLogger.Instance
// 		);
//
// 		var resource1 = await pool.GetAsync();
// 		resource1.RegisterPool(pool);
//
// 		// Act
// 		await resource1.ResetPoolAsync();
// 		var resource2 = await pool.GetAsync();
//
// 		// Assert
// 		factoryCallCount.ShouldBe(2);
// 		resource1.Id.ShouldBe(1);
// 		resource2.Id.ShouldBe(2);
// 		resource2.ShouldNotBeSameAs(resource1);
// 	}
//
// 	// Test resources
// 	class TestResource(string config) {
// 		public string Config { get; } = config;
// 		public int    Id     { get; set; }
// 	}
//
// 	class DisposableTestResource(string config) : TestResource(config), IDisposable {
// 		public bool IsDisposed { get; private set; }
//
// 		public void Dispose() => IsDisposed = true;
// 	}
//
// 	class AsyncDisposableTestResource(string config) : TestResource(config), IAsyncDisposable {
// 		public bool IsDisposed { get; private set; }
//
// 		public ValueTask DisposeAsync() {
// 			IsDisposed = true;
// 			return ValueTask.CompletedTask;
// 		}
// 	}
//
// 	class TransientException(string message) : Exception(message);
// }
