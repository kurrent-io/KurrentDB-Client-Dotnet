# NBomber Comprehensive Guide: Mastering gRPC Load Testing with .NET

*The complete NBomber guide for gRPC performance engineering - from unary calls to bidirectional streaming*

## Table of Contents

📚 **Choose Your gRPC Learning Path:**
- 🟢 **New to gRPC + NBomber** → [gRPC Fundamentals](#grpc-fundamentals) + [First gRPC Scenario](#first-grpc)
- 🟡 **Know gRPC Basics** → Jump to [gRPC Streaming Patterns](#grpc-streaming) 
- 🔴 **gRPC Expert** → Go to [Advanced gRPC Patterns](#advanced-grpc)

---

## Understanding NBomber for gRPC {#grpc-fundamentals}

### Why NBomber Excels at gRPC Load Testing

NBomber is uniquely positioned as the **premier gRPC load testing platform** for .NET ecosystems:

**🎯 gRPC-First Design Benefits:**
- **Native .NET gRPC Integration** - Uses the same gRPC clients your applications use
- **All 4 gRPC Patterns** - Unary, Server Streaming, Client Streaming, Bidirectional
- **Connection Management** - Advanced HTTP/2 connection pooling and keepalive
- **Real Streaming** - True async streaming support, not request/response simulation
- **Protocol Buffers** - Full protobuf support with generated client integration

**🏗️ gRPC Performance Testing Architecture:**

```
┌─────────────────────────────────────────────────────────────┐
│                    NBomber gRPC Runner                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌──────────────┐         │
│  │   Unary     │  │   Server    │  │ Bidirectional│         │
│  │ Scenarios   │  │ Streaming   │  │  Streaming   │         │ 
│  └─────────────┘  └─────────────┘  └──────────────┘         │
├─────────────────────────────────────────────────────────────┤
│           gRPC Channel Factories (HTTP/2)                   │
├─────────────────────────────────────────────────────────────┤
│     Load Simulations | gRPC Metrics | Stream Analytics      │
└─────────────────────────────────────────────────────────────┘
```

### gRPC vs HTTP Load Testing: Key Differences

**Traditional HTTP Load Testing:**
- Request → Response pattern only
- Connection per request (HTTP/1.1) or limited multiplexing (HTTP/2)
- Text-based protocols and JSON payloads
- Simple success/failure metrics

**NBomber gRPC Load Testing:**
- **4 distinct interaction patterns** (Unary, Server/Client/Bidirectional streaming)
- **HTTP/2 multiplexing** with persistent connections
- **Binary protobuf** with efficient serialization
- **Stream lifecycle metrics** (messages per stream, stream duration, backpressure)

---

## Your First gRPC Scenario with S2 Stream Store {#first-grpc}

Let's start with NBomber's gRPC capabilities using the real S2 Stream Store service:

### Setting Up gRPC with NBomber

```csharp
// Required packages for gRPC + NBomber
// Install-Package NBomber -Version 6.0.2
// Install-Package Grpc.Net.Client -Version 2.66.0
// Install-Package Google.Protobuf -Version 3.28.2
// Install-Package Grpc.Tools -Version 2.66.0

using NBomber.CSharp;
using Grpc.Net.Client;
using S2.V1Alpha; // Generated from S2 protobuf

// NBomber gRPC Channel Factory - Production Optimized
public static class S2GrpcChannelFactory
{
    public static GrpcChannel CreateOptimizedChannel(string address)
    {
        // Configure HTTP/2 handler for high-performance gRPC
        var handler = new SocketsHttpHandler
        {
            // HTTP/2 Connection Management
            PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan, // Keep connections alive
            KeepAlivePingDelay = TimeSpan.FromSeconds(30),          // Send keepalive every 30s
            KeepAlivePingTimeout = TimeSpan.FromSeconds(5),         // 5s keepalive timeout
            EnableMultipleHttp2Connections = true,                 // Enable connection multiplexing
            
            // Connection Pool Settings for Load Testing
            MaxConnectionsPerServer = 100,                          // Max connections per endpoint
            PooledConnectionLifetime = TimeSpan.FromMinutes(30)     // Recycle connections every 30min
        };
        
        return GrpcChannel.ForAddress(address, new GrpcChannelOptions
        {
            HttpHandler = handler,
            
            // Message Size Configuration
            MaxReceiveMessageSize = 16 * 1024 * 1024,  // 16MB max receive
            MaxSendMessageSize.   = 16 * 1024 * 1024,  // 16MB max send
            
            // Compression for Network Efficiency  
            CompressionProviders = new[] {
                new GzipCompressionProvider(CompressionLevel.Optimal)
            },
            
            // Load Testing Optimizations
            ThrowOperationCanceledOnCancellation    = true, // Proper cancellation handling
            UnsafeUseInsecureChannelCallCredentials = false // Security in production
        });
    }
}
```

**🔍 gRPC Channel Configuration Breakdown:**

1. **SocketsHttpHandler** - Low-level HTTP/2 transport configuration
2. **PooledConnectionIdleTimeout = Infinite** - Keep gRPC connections alive for streaming
3. **KeepAlivePing** - Detect broken connections in long-running streams
4. **EnableMultipleHttp2Connections** - Allow connection multiplexing for high throughput
5. **MaxConnectionsPerServer** - Control connection pool size for load testing
6. **MaxReceiveMessageSize** - Handle large streaming payloads efficiently

### Basic gRPC Unary Call Testing

```csharp
// S2 Stream Store - Basin Management (Unary Calls)
var s2BasinScenario = Scenario.Create("s2_basin_operations", async context => {
    // Create gRPC channel and clients for S2 Stream Store
    var channel       = S2GrpcChannelFactory.CreateOptimizedChannel("https://s2.platform.com:443");
    var accountClient = new AccountService.AccountServiceClient(channel);
    var basinClient   = new BasinService.BasinServiceClient(channel);
    
    try {
        // Step 1: List Basins (Unary Call)
        var listBasinsStep = await Step.Run("list_basins", context, async () => {
            var request = new ListBasinsRequest {
                Prefix = "trading-",        // Only trading-related basins
                Limit = 100,               // Limit results for performance
                StartAfter = ""            // Pagination support
            };
            
            // Execute gRPC unary call
            var response = await accountClient.ListBasinsAsync(request);
            
            return Response.Ok(
                payload: response.Basins.FirstOrDefault()?.Name ?? "trading-default",
                statusCode: "OK",
                sizeBytes: response.CalculateSize() // Protobuf size tracking
            );
        });
        
        var basinName = listBasinsStep.Payload.Value?.ToString() ?? "trading-default";
        
        // Step 2: Get Basin Configuration (Unary Call)
        await Step.Run("get_basin_config", context, async () =>
        {
            var request = new GetBasinConfigRequest
            {
                Basin = basinName
            };
            
            var response = await accountClient.GetBasinConfigAsync(request);
            
            return Response.Ok(
                payload: new { 
                    StorageClass = response.Config.DefaultStreamConfig.StorageClass,
                    CreateOnAppend = response.Config.CreateStreamOnAppend 
                },
                statusCode: "OK",
                sizeBytes: response.CalculateSize()
            );
        });
        
        // Step 3: List Streams in Basin (Unary Call)
        await Step.Run("list_streams", context, async () =>
        {
            var request = new ListStreamsRequest
            {
                Prefix = "market-data/",   // Only market data streams
                Limit = 50
            };
            
            var response = await basinClient.ListStreamsAsync(request);
            
            return Response.Ok(
                payload: response.Streams.Count,
                statusCode: "OK",
                sizeBytes: response.CalculateSize()
            );
        });
        
        return Response.Ok();
    }
    catch (RpcException ex)
    {
        // gRPC-specific error handling
        return Response.Fail($"gRPC Error: {ex.StatusCode} - {ex.Message}");
    }
    finally
    {
        // Proper gRPC resource cleanup
        await channel.ShutdownAsync();
        channel.Dispose();
    }
})
.WithLoadSimulations(
    // Realistic basin management load pattern
    Simulation.InjectPerSec(rate: 50, during: TimeSpan.FromMinutes(5))
);

// Execute the gRPC scenario
NBomberRunner
    .RegisterScenarios(s2BasinScenario)
    .Run();
```

**🔍 gRPC Unary Call Breakdown:**

1. **`ListBasinsAsync(request)`** - Standard gRPC unary call pattern
2. **`response.CalculateSize()`** - Protobuf size measurement for bandwidth analysis
3. **RpcException** - gRPC-specific exception handling with StatusCode
4. **Channel Management** - Explicit shutdown and disposal for load testing
5. **Payload Tracking** - NBomber tracks gRPC-specific data between steps

---

## gRPC Streaming Patterns with NBomber {#grpc-streaming}

### Server Streaming: Real-Time Market Data

Server streaming is perfect for real-time data feeds like market data, game events, or log streaming:

```csharp
// S2 Stream Store - Market Data Server Streaming
var marketDataStreamingScenario = Scenario.Create("s2_market_data_streaming", async context =>
{
    var channel = S2GrpcChannelFactory.CreateOptimizedChannel("https://s2.platform.com:443");
    var streamClient = new StreamService.StreamServiceClient(channel);
    
    try
    {
        var symbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN", "NVDA" };
        var symbol = symbols[context.InvocationNumber % symbols.Length];
        var streamName = $"market-data/equities/{symbol}/real-time";
        
        // Step 1: Setup Real-Time Read Session (Server Streaming)
        await Step.Run("real_time_market_data", context, async () =>
        {
            var readRequest = new ReadSessionRequest
            {
                Stream = streamName,
                SeqNum = 0,                    // Start from latest
                Heartbeats = true,             // Enable connection health monitoring
                
                // Limit for load testing - don't run forever
                Limit = new ReadLimit 
                { 
                    Count = 500,               // Max 500 messages
                    Bytes = 10 * 1024 * 1024   // Max 10MB
                }
            };
            
            var messageCount = 0;
            var totalBytes = 0L;
            var startTime = DateTime.UtcNow;
            
            // Process server streaming responses
            await foreach (var response in streamClient.ReadSession(readRequest).ResponseStream.ReadAllAsync())
            {
                if (response.Output?.Batch != null)
                {
                    // Process each market data record
                    foreach (var record in response.Output.Batch.Records)
                    {
                        messageCount++;
                        totalBytes += record.Body.Length;
                        
                        // Parse market data from protobuf
                        var marketData = ParseMarketDataRecord(record);
                        
                        // Simulate realistic processing time
                        await Task.Delay(Random.Shared.Next(1, 5));
                        
                        // Log high-value trades for analysis
                        if (marketData.Volume > 10000)
                        {
                            context.Logger.LogInformation(
                                "High volume trade: {Symbol} {Volume} @ {Price}", 
                                marketData.Symbol, marketData.Volume, marketData.Price
                            );
                        }
                    }
                }
                
                // Stop conditions for load testing
                if (messageCount >= 500 || 
                    DateTime.UtcNow - startTime > TimeSpan.FromSeconds(60) ||
                    totalBytes > 10 * 1024 * 1024)
                {
                    break;
                }
            }
            
            return Response.Ok(
                payload: new { 
                    MessagesProcessed = messageCount, 
                    BytesProcessed = totalBytes,
                    Symbol = symbol,
                    Duration = DateTime.UtcNow - startTime
                },
                sizeBytes: (int)totalBytes
            );
        });
        
        return Response.Ok();
    }
    catch (RpcException ex)
    {
        return Response.Fail($"Market data streaming failed: {ex.StatusCode} - {ex.Message}");
    }
    finally
    {
        await channel.ShutdownAsync();
        channel.Dispose();
    }
})
.WithLoadSimulations(
    // Lower concurrency for streaming - each user maintains a long connection
    Simulation.KeepConstant(copies: 25, during: TimeSpan.FromMinutes(10))
);

// Helper method to parse market data from S2 records
private static MarketDataRecord ParseMarketDataRecord(SequencedRecord record)
{
    try
    {
        var json = record.Body.ToStringUtf8();
        return JsonSerializer.Deserialize<MarketDataRecord>(json);
    }
    catch (Exception ex)
    {
        return new MarketDataRecord 
        { 
            Symbol = "UNKNOWN", 
            Price = 0, 
            Volume = 0, 
            Timestamp = record.Timestamp 
        };
    }
}

public class MarketDataRecord
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
    public ulong Timestamp { get; set; }
    public string Exchange { get; set; }
    public string TradeType { get; set; }
}
```

**🔍 Server Streaming Breakdown:**

1. **`ReadSession(request).ResponseStream`** - gRPC server streaming pattern
2. **`await foreach`** - Async enumeration over streaming responses
3. **Heartbeats = true** - Detects broken connections in long streams
4. **ReadLimit** - Prevents infinite streams in load testing
5. **Message Processing** - Realistic data processing simulation
6. **KeepConstant Simulation** - Maintains persistent streaming connections

### Client Streaming: High-Frequency Trading Orders

Client streaming is ideal for batch uploads, telemetry ingestion, or high-frequency trading:

```csharp
// S2 Stream Store - High-Frequency Order Submission (Client Streaming)
var hftOrderSubmissionScenario = Scenario.Create("s2_hft_order_submission", async context =>
{
    var channel = S2GrpcChannelFactory.CreateOptimizedChannel("https://s2.platform.com:443");
    var streamClient = new StreamService.StreamServiceClient(channel);
    
    try
    {
        var streamName = "trading/orders/high-frequency";
        var traderId = $"trader_{context.InvocationNumber % 100}";
        
        // Step 1: Submit High-Frequency Order Batch (Client Streaming)
        await Step.Run("submit_hft_orders", context, async () =>
        {
            using var streamingCall = streamClient.AppendSession();
            
            var orderCount = Random.Shared.Next(50, 200); // 50-200 orders per batch
            var sentOrders = 0;
            var totalOrderValue = 0m;
            
            // Send orders rapidly via client streaming
            for (int i = 0; i < orderCount; i++)
            {
                var order = CreateHighFrequencyOrder(context.InvocationNumber, i, traderId);
                totalOrderValue += order.Quantity * order.Price;
                
                var appendRecord = new AppendRecord
                {
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Headers = 
                    {
                        new Header { Name = Encoding.UTF8.GetBytes("order-type"), Value = Encoding.UTF8.GetBytes("HFT") },
                        new Header { Name = Encoding.UTF8.GetBytes("trader-id"), Value = Encoding.UTF8.GetBytes(traderId) },
                        new Header { Name = Encoding.UTF8.GetBytes("symbol"), Value = Encoding.UTF8.GetBytes(order.Symbol) }
                    },
                    Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order))
                };
                
                await streamingCall.RequestStream.WriteAsync(new AppendSessionRequest
                {
                    Input = new AppendInput
                    {
                        Stream = streamName,
                        Records = { appendRecord }
                    }
                });
                
                sentOrders++;
                
                // Realistic high-frequency timing: 1-10ms between orders
                await Task.Delay(Random.Shared.Next(1, 10));
            }
            
            // Complete the client stream
            await streamingCall.RequestStream.CompleteAsync();
            
            // Process server responses to confirm order acceptance
            var confirmedOrders = 0;
            var confirmedSequenceNumbers = new List<ulong>();
            
            await foreach (var response in streamingCall.ResponseStream.ReadAllAsync())
            {
                confirmedOrders++;
                confirmedSequenceNumbers.Add(response.Output.StartSeqNum);
                
                // Verify order sequence integrity
                if (response.Output.EndSeqNum - response.Output.StartSeqNum != 1)
                {
                    context.Logger.LogWarning(
                        "Unexpected batch size in response: Start={Start}, End={End}", 
                        response.Output.StartSeqNum, response.Output.EndSeqNum
                    );
                }
            }
            
            return Response.Ok(
                payload: new { 
                    OrdersSent = sentOrders, 
                    OrdersConfirmed = confirmedOrders,
                    TotalOrderValue = totalOrderValue,
                    TraderId = traderId,
                    SequenceNumbers = confirmedSequenceNumbers.Take(5).ToArray() // Sample for debugging
                }
            );
        });
        
        return Response.Ok();
    }
    catch (RpcException ex)
    {
        return Response.Fail($"HFT order submission failed: {ex.StatusCode} - {ex.Message}");
    }
    finally
    {
        await channel.ShutdownAsync();
        channel.Dispose();
    }
})
.WithLoadSimulations(
    // High-frequency trading burst pattern
    Simulation.RampingInject(rate: 100, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5)),
    Simulation.RampingInject(rate: 0, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(1))
);

private static HighFrequencyOrder CreateHighFrequencyOrder(int invocationNumber, int orderIndex, string traderId)
{
    var symbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN", "NVDA", "SPY", "QQQ" };
    var symbol = symbols[Random.Shared.Next(symbols.Length)];
    
    return new HighFrequencyOrder
    {
        OrderId = $"HFT{invocationNumber:D6}{orderIndex:D3}",
        Symbol = symbol,
        Side = Random.Shared.NextDouble() > 0.5 ? "BUY" : "SELL",
        Quantity = Random.Shared.Next(100, 1000),
        Price = 100 + (decimal)(Random.Shared.NextDouble() * 400), // $100-$500
        OrderType = "LIMIT",
        TimeInForce = "IOC", // Immediate or Cancel for HFT
        TraderId = traderId,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        
        // HFT-specific fields
        AlgorithmId = $"ALGO{Random.Shared.Next(1, 10)}",
        ParentOrderId = orderIndex % 10 == 0 ? $"PARENT{invocationNumber:D6}" : null,
        MaxFloor = Random.Shared.Next(10, 100)
    };
}

public class HighFrequencyOrder
{
    public string OrderId { get; set; }
    public string Symbol { get; set; }
    public string Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public string OrderType { get; set; }
    public string TimeInForce { get; set; }
    public string TraderId { get; set; }
    public long Timestamp { get; set; }
    
    // HFT-specific properties
    public string AlgorithmId { get; set; }
    public string ParentOrderId { get; set; }
    public int MaxFloor { get; set; }
}
```

**🔍 Client Streaming Breakdown:**

1. **`AppendSession()`** - Creates a client streaming call
2. **`streamingCall.RequestStream.WriteAsync()`** - Sends individual messages
3. **`RequestStream.CompleteAsync()`** - Signals end of client stream
4. **Response Processing** - Server confirms each batch with sequence numbers
5. **High-Frequency Timing** - Realistic 1-10ms delays between orders

### Bidirectional Streaming: Real-Time Order Book

Bidirectional streaming enables full-duplex communication for real-time systems:

```csharp
// S2 Stream Store - Real-Time Order Book Management (Bidirectional Streaming)
var orderBookStreamingScenario = Scenario.Create("s2_order_book_streaming", async context =>
{
    var channel = S2GrpcChannelFactory.CreateOptimizedChannel("https://s2.platform.com:443");
    var streamClient = new StreamService.StreamServiceClient(channel);
    
    try
    {
        var symbols = new[] { "BTC/USD", "ETH/USD", "AAPL", "TSLA" };
        var symbol = symbols[context.InvocationNumber % symbols.Length];
        var streamName = $"trading/order-book/{symbol}/real-time";
        var traderId = $"trader_{context.InvocationNumber % 50}";
        
        // Step 1: Real-Time Order Book Updates (Bidirectional Streaming)
        await Step.Run("order_book_streaming", context, async () =>
        {
            using var streamingCall = streamClient.AppendSession();
            
            var sentUpdates = 0;
            var receivedConfirmations = 0;
            var orderBookLevels = InitializeOrderBook(symbol);
            
            // Task 1: Send order book updates continuously
            var sendTask = Task.Run(async () =>
            {
                try
                {
                    for (int i = 0; i < 100; i++) // Send 100 updates
                    {
                        // Generate realistic order book update
                        var update = GenerateOrderBookUpdate(symbol, traderId, orderBookLevels, i);
                        
                        var appendRecord = new AppendRecord
                        {
                            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Headers = 
                            {
                                new Header { Name = Encoding.UTF8.GetBytes("symbol"), Value = Encoding.UTF8.GetBytes(symbol) },
                                new Header { Name = Encoding.UTF8.GetBytes("update-type"), Value = Encoding.UTF8.GetBytes("order-book") },
                                new Header { Name = Encoding.UTF8.GetBytes("trader-id"), Value = Encoding.UTF8.GetBytes(traderId) }
                            },
                            Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(update))
                        };
                        
                        await streamingCall.RequestStream.WriteAsync(new AppendSessionRequest
                        {
                            Input = new AppendInput
                            {
                                Stream = streamName,
                                Records = { appendRecord }
                            }
                        });
                        
                        sentUpdates++;
                        
                        // Real-time order book frequency: 10-50ms between updates
                        await Task.Delay(Random.Shared.Next(10, 50));
                    }
                    
                    await streamingCall.RequestStream.CompleteAsync();
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, "Error sending order book updates");
                }
            });
            
            // Task 2: Receive confirmations simultaneously
            var receiveTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var response in streamingCall.ResponseStream.ReadAllAsync())
                    {
                        receivedConfirmations++;
                        
                        // Process order book confirmation
                        var sequenceGap = response.Output.EndSeqNum - response.Output.StartSeqNum;
                        if (sequenceGap > 1)
                        {
                            context.Logger.LogInformation(
                                "Order book batch processed: {Count} updates, Seq: {Start}-{End}",
                                sequenceGap, response.Output.StartSeqNum, response.Output.EndSeqNum
                            );
                        }
                        
                        // Simulate order book state management
                        await Task.Delay(1);
                    }
                }
                catch (Exception ex)
                {
                    context.Logger.LogError(ex, "Error receiving order book confirmations");
                }
            });
            
            // Wait for both send and receive to complete
            await Task.WhenAll(sendTask, receiveTask);
            
            return Response.Ok(
                payload: new { 
                    Symbol = symbol,
                    UpdatesSent = sentUpdates, 
                    ConfirmationsReceived = receivedConfirmations,
                    TraderId = traderId
                }
            );
        });
        
        return Response.Ok();
    }
    catch (RpcException ex)
    {
        return Response.Fail($"Order book streaming failed: {ex.StatusCode} - {ex.Message}");
    }
    finally
    {
        await channel.ShutdownAsync();
        channel.Dispose();
    }
})
.WithLoadSimulations(
    // Lower concurrency for bidirectional streaming
    Simulation.KeepConstant(copies: 10, during: TimeSpan.FromMinutes(5))
);

// Initialize realistic order book state
private static Dictionary<string, decimal> InitializeOrderBook(string symbol)
{
    var basePrice = symbol switch
    {
        "BTC/USD" => 45000m,
        "ETH/USD" => 3000m,
        "AAPL" => 150m,
        "TSLA" => 200m,
        _ => 100m
    };
    
    return new Dictionary<string, decimal>
    {
        ["bid_price_1"] = basePrice * 0.999m,
        ["ask_price_1"] = basePrice * 1.001m,
        ["bid_size_1"] = 1000m,
        ["ask_size_1"] = 1000m
    };
}

private static OrderBookUpdate GenerateOrderBookUpdate(string symbol, string traderId, 
    Dictionary<string, decimal> orderBookLevels, int updateIndex)
{
    // Simulate realistic order book changes
    var updateType = Random.Shared.NextDouble() switch
    {
        < 0.4 => "PRICE_UPDATE",
        < 0.8 => "SIZE_UPDATE", 
        _ => "NEW_LEVEL"
    };
    
    return new OrderBookUpdate
    {
        Symbol = symbol,
        TraderId = traderId,
        UpdateType = updateType,
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        SequenceNumber = updateIndex,
        
        // Realistic order book levels
        Bids = GenerateOrderBookSide("BID", orderBookLevels["bid_price_1"], 5),
        Asks = GenerateOrderBookSide("ASK", orderBookLevels["ask_price_1"], 5)
    };
}

private static OrderBookLevel[] GenerateOrderBookSide(string side, decimal basePrice, int levels)
{
    var orders = new OrderBookLevel[levels];
    
    for (int i = 0; i < levels; i++)
    {
        var priceOffset = side == "BID" ? -i * 0.01m : i * 0.01m;
        orders[i] = new OrderBookLevel
        {
            Price = basePrice + (basePrice * priceOffset),
            Size = Random.Shared.Next(100, 5000),
            OrderCount = Random.Shared.Next(1, 20)
        };
    }
    
    return orders;
}

public class OrderBookUpdate
{
    public string Symbol { get; set; }
    public string TraderId { get; set; }
    public string UpdateType { get; set; }
    public long Timestamp { get; set; }
    public int SequenceNumber { get; set; }
    public OrderBookLevel[] Bids { get; set; }
    public OrderBookLevel[] Asks { get; set; }
}

public class OrderBookLevel
{
    public decimal Price { get; set; }
    public int Size { get; set; }
    public int OrderCount { get; set; }
}
```

**🔍 Bidirectional Streaming Breakdown:**

1. **`AppendSession()`** - Creates full-duplex streaming call
2. **`Task.Run()` for Send/Receive** - Concurrent sending and receiving
3. **`Task.WhenAll()`** - Waits for both directions to complete
4. **Real-Time Processing** - Simulates actual order book management
5. **Sequence Tracking** - Monitors order integrity in high-frequency updates

---

## Advanced gRPC Patterns with NBomber {#advanced-grpc}

### gRPC Client Factories for Connection Management

Managing gRPC connections efficiently is critical for realistic load testing:

```csharp
// Production-Grade gRPC Client Factory for S2 Stream Store
public static class S2ClientFactory
{
    public static IClientFactory<AccountService.AccountServiceClient> CreateAccountServiceFactory(string endpoint)
    {
        return ClientFactory.Create(
            name: "s2_account_clients",
            
            // Factory function - creates new gRPC clients
            initClient: () =>
            {
                var channel = S2GrpcChannelFactory.CreateOptimizedChannel(endpoint);
                return new AccountService.AccountServiceClient(channel);
            },
            
            // Disposal function - properly cleans up gRPC resources
            disposeClient: client =>
            {
                try
                {
                    var channel = client.Channel as GrpcChannel;
                    channel?.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                    channel?.Dispose();
                }
                catch (Exception ex)
                {
                    // Log but don't throw during cleanup
                    Console.WriteLine($"Error disposing gRPC client: {ex.Message}");
                }
            }
        );
    }
    
    public static IClientFactory<StreamService.StreamServiceClient> CreateStreamServiceFactory(string endpoint)
    {
        return ClientFactory.Create(
            name: "s2_stream_clients",
            
            initClient: () =>
            {
                var channel = S2GrpcChannelFactory.CreateOptimizedChannel(endpoint);
                return new StreamService.StreamServiceClient(channel);
            },
            
            disposeClient: client =>
            {
                try
                {
                    var channel = client.Channel as GrpcChannel;
                    channel?.ShutdownAsync().Wait(TimeSpan.FromSeconds(5));
                    channel?.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing gRPC stream client: {ex.Message}");
                }
            }
        );
    }
}

// Using Client Factories in High-Throughput Scenarios
var highThroughputScenario = Scenario.Create("s2_high_throughput_operations", async context =>
{
    // NBomber provides managed clients from the factory
    var accountClientFactory = S2ClientFactory.CreateAccountServiceFactory("https://s2.platform.com:443");
    var streamClientFactory = S2ClientFactory.CreateStreamServiceFactory("https://s2.platform.com:443");
    
    // Get clients from factory (automatically managed by NBomber)
    var accountClient = accountClientFactory.GetClient(context.ScenarioInfo);
    var streamClient = streamClientFactory.GetClient(context.ScenarioInfo);
    
    try
    {
        // Step 1: Account operations with managed client
        await Step.Run("account_operations", context, async () =>
        {
            var listRequest = new ListBasinsRequest { Prefix = "trading-", Limit = 20 };
            var response = await accountClient.ListBasinsAsync(listRequest);
            return Response.Ok(payload: response.Basins.Count);
        });
        
        // Step 2: Stream operations with managed client  
        await Step.Run("stream_operations", context, async () =>
        {
            var streamName = "market-data/test-stream";
            var checkRequest = new CheckTailRequest { Stream = streamName };
            var response = await streamClient.CheckTailAsync(checkRequest);
            return Response.Ok(payload: response.NextSeqNum);
        });
        
        return Response.Ok();
    }
    catch (RpcException ex)
    {
        return Response.Fail($"High throughput operation failed: {ex.StatusCode}");
    }
    // No manual cleanup needed - NBomber manages client lifecycle
})
.WithClientFactory(accountClientFactory)   // Attach factories to scenario
.WithClientFactory(streamClientFactory)
.WithLoadSimulations(
    // High-throughput pattern for connection reuse testing
    Simulation.InjectPerSec(rate: 500, during: TimeSpan.FromMinutes(10))
);
```

**🔍 gRPC Client Factory Benefits:**

1. **Connection Reuse** - gRPC channels are expensive; factories enable pooling
2. **Automatic Lifecycle** - NBomber handles creation and disposal
3. **Resource Management** - Prevents gRPC channel leaks under load
4. **Performance** - Eliminates connection setup overhead per request
5. **Scalability** - Supports thousands of concurrent gRPC operations

### gRPC Data Feeds for Realistic Testing

```csharp
// S2 Stream Configuration Data Feed
public class S2StreamConfig
{
    public string StreamName { get; set; }
    public StorageClass StorageClass { get; set; }
    public uint RetentionDays { get; set; }
    public TimestampingMode TimestampingMode { get; set; }
    public string Category { get; set; }
}

// Load S2 stream configurations for realistic testing
var streamConfigs = new[]
{
    new S2StreamConfig 
    { 
        StreamName = "market-data/equities/AAPL/trades", 
        StorageClass = StorageClass.Express,
        RetentionDays = 7,
        TimestampingMode = TimestampingMode.ClientPrefer,
        Category = "market-data"
    },
    new S2StreamConfig 
    { 
        StreamName = "market-data/equities/GOOGL/trades", 
        StorageClass = StorageClass.Express,
        RetentionDays = 7,
        TimestampingMode = TimestampingMode.ClientPrefer,
        Category = "market-data"
    },
    new S2StreamConfig 
    { 
        StreamName = "trading/orders/execution-reports", 
        StorageClass = StorageClass.Standard,
        RetentionDays = 30,
        TimestampingMode = TimestampingMode.Arrival,
        Category = "trading"
    },
    new S2StreamConfig 
    { 
        StreamName = "risk/portfolio-valuations", 
        StorageClass = StorageClass.Standard,
        RetentionDays = 365,
        TimestampingMode = TimestampingMode.ClientRequire,
        Category = "risk"
    }
};

var streamConfigFeed = FeedData.FromSeq("s2_stream_configs", streamConfigs);

// Data-driven S2 stream creation scenario
var streamCreationScenario = Scenario.Create("s2_stream_creation", async context =>
{
    var streamClientFactory = S2ClientFactory.CreateStreamServiceFactory("https://s2.platform.com:443");
    var basinClient = new BasinService.BasinServiceClient(
        S2GrpcChannelFactory.CreateOptimizedChannel("https://s2.platform.com:443")
    );
    
    // Get stream configuration from data feed
    var streamConfig = context.FeedItem<S2StreamConfig>();
    
    try
    {
        // Step 1: Create stream with data-driven configuration
        await Step.Run("create_stream", context, async () =>
        {
            var createRequest = new CreateStreamRequest
            {
                Stream = streamConfig.StreamName,
                Config = new StreamConfig
                {
                    StorageClass = streamConfig.StorageClass,
                    Age = streamConfig.RetentionDays * 24 * 60 * 60, // Convert to seconds
                    Timestamping = new StreamConfig.Types.Timestamping
                    {
                        Mode = streamConfig.TimestampingMode,
                        Uncapped = streamConfig.Category == "trading" // Allow future timestamps for trading
                    }
                }
            };
            
            var response = await basinClient.CreateStreamAsync(createRequest);
            
            return Response.Ok(
                payload: new { 
                    StreamName = streamConfig.StreamName,
                    CreatedAt = response.Info.CreatedAt,
                    Category = streamConfig.Category
                }
            );
        });
        
        return Response.Ok();
    }
    catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
    {
        // Stream already exists - this is acceptable in load testing
        return Response.Ok(payload: streamConfig.StreamName);
    }
    catch (RpcException ex)
    {
        return Response.Fail($"Stream creation failed: {ex.StatusCode} - {ex.Message}");
    }
})
.WithFeed(streamConfigFeed)  // Attach S2 stream configuration data
.WithClientFactory(S2ClientFactory.CreateStreamServiceFactory("https://s2.platform.com:443"))
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 20, during: TimeSpan.FromMinutes(3))
);
```

### gRPC Error Handling and Resilience

```csharp
// Comprehensive gRPC error handling for S2 Stream Store
var resilientS2Scenario = Scenario.Create("s2_resilient_operations", async context =>
{
    var streamClientFactory = S2ClientFactory.CreateStreamServiceFactory("https://s2.platform.com:443");
    var streamClient = streamClientFactory.GetClient(context.ScenarioInfo);
    
    var retryCount = 0;
    const int maxRetries = 3;
    
    // Step 1: Resilient stream operations with retry logic
    var appendStep = await Step.Run("resilient_append", context, async () =>
    {
        while (retryCount < maxRetries)
        {
            try
            {
                var streamName = "trading/orders/resilient-test";
                var record = CreateTradingRecord(context.InvocationNumber);
                
                var appendRequest = new AppendRequest
                {
                    Input = new AppendInput
                    {
                        Stream = streamName,
                        Records = { record }
                    }
                };
                
                var response = await streamClient.AppendAsync(appendRequest);
                
                return Response.Ok(
                    payload: new { 
                        SeqNum = response.Output.StartSeqNum,
                        Retries = retryCount
                    }
                );
            }
            catch (RpcException ex)
            {
                retryCount++;
                
                // Handle specific gRPC error conditions
                switch (ex.StatusCode)
                {
                    case StatusCode.DeadlineExceeded:
                        context.Logger.LogWarning("gRPC timeout on attempt {Attempt}, retrying...", retryCount);
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(TimeSpan.FromMilliseconds(100 * retryCount)); // Exponential backoff
                            continue;
                        }
                        break;
                        
                    case StatusCode.Unavailable:
                        context.Logger.LogWarning("gRPC service unavailable on attempt {Attempt}, retrying...", retryCount);
                        if (retryCount < maxRetries)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                            continue;
                        }
                        break;
                        
                    case StatusCode.ResourceExhausted:
                        context.Logger.LogError("gRPC resource exhausted - backing off");
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        return Response.Fail($"Resource exhausted after {retryCount} retries");
                        
                    case StatusCode.InvalidArgument:
                        // Don't retry on client errors
                        return Response.Fail($"Invalid argument: {ex.Message}");
                        
                    case StatusCode.NotFound:
                        // Stream doesn't exist - create it
                        context.Logger.LogInformation("Stream not found, will be created automatically");
                        if (retryCount < maxRetries)
                        {
                            continue;
                        }
                        break;
                        
                    default:
                        context.Logger.LogError("Unexpected gRPC error: {StatusCode} - {Message}", ex.StatusCode, ex.Message);
                        break;
                }
                
                if (retryCount >= maxRetries)
                {
                    return Response.Fail($"Failed after {maxRetries} retries: {ex.StatusCode} - {ex.Message}");
                }
            }
        }
        
        return Response.Fail($"Exhausted all {maxRetries} retry attempts");
    });
    
    return Response.Ok();
})
.WithClientFactory(S2ClientFactory.CreateStreamServiceFactory("https://s2.platform.com:443"))
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 100, during: TimeSpan.FromMinutes(5))
);

private static AppendRecord CreateTradingRecord(int invocationNumber)
{
    var trade = new
    {
        TradeId = $"TRADE{invocationNumber:D8}",
        Symbol = "AAPL",
        Price = 150.00m + (decimal)(Random.Shared.NextDouble() * 10),
        Quantity = Random.Shared.Next(100, 1000),
        Side = Random.Shared.NextDouble() > 0.5 ? "BUY" : "SELL",
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
    };
    
    return new AppendRecord
    {
        Timestamp = (ulong)trade.Timestamp,
        Headers = 
        {
            new Header { Name = Encoding.UTF8.GetBytes("symbol"), Value = Encoding.UTF8.GetBytes("AAPL") },
            new Header { Name = Encoding.UTF8.GetBytes("trade-type"), Value = Encoding.UTF8.GetBytes("market") }
        },
        Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(trade))
    };
}
```

**🔍 gRPC Error Handling Patterns:**

1. **StatusCode-Specific Handling** - Different strategies for different gRPC errors
2. **Exponential Backoff** - Increasing delays between retries
3. **Circuit Breaking** - Fail fast on ResourceExhausted
4. **Auto-Recovery** - Handle NotFound by allowing stream creation
5. **Contextual Logging** - Rich error information for debugging

---

## Production gRPC Load Testing {#production-grpc}

### Enterprise gRPC Testing Framework

```csharp
// Production-ready gRPC load testing base class
public abstract class S2LoadTestBase
{
    protected readonly ILogger _logger;
    protected readonly S2LoadTestConfiguration _config;
    protected readonly gRPCMetricsCollector _metricsCollector;
    
    protected S2LoadTestBase(IConfiguration configuration)
    {
        _logger = LoggerFactory.Create(builder =>
            builder.AddConsole(options => options.IncludeScopes = true)
                   .AddStructuredLogging() // Custom structured logging
                   .SetMinimumLevel(LogLevel.Information))
            .CreateLogger(GetType().Name);
            
        _config = configuration.GetSection("S2LoadTest").Get<S2LoadTestConfiguration>()
            ?? throw new InvalidOperationException("S2LoadTest configuration required");
            
        _metricsCollector = new gRPCMetricsCollector(_config.PrometheusEndpoint);
    }
    
    protected NBomberRunner CreateS2Runner(params Scenario[] scenarios)
    {
        return NBomberRunner
            .RegisterScenarios(scenarios)
            .WithTestName($"S2 {_config.TestName}")
            .WithTestDescription($"S2 Stream Store load test - {_config.Environment}")
            .WithReportFolder(_config.ReportDirectory)
            .WithWorkerPlugins(
                CreatePrometheusPlugin(),
                CreateS2MetricsPlugin(),
                new PingPlugin(PingPluginConfig.CreateDefault(_config.S2Endpoints))
            );
    }
    
    private IWorkerPlugin CreateS2MetricsPlugin()
    {
        return new S2StreamMetricsPlugin(new S2MetricsConfig
        {
            StreamNamePatterns = _config.MonitoredStreamPatterns,
            CollectSequenceMetrics = true,
            CollectTimestampMetrics = true,
            ReportingInterval = TimeSpan.FromSeconds(30)
        });
    }
    
    protected async Task<S2TestResult> ExecuteWithS2Validation(NBomberRunner runner)
    {
        _logger.LogInformation("🚀 Starting S2 Stream Store load test: {TestName}", _config.TestName);
        
        var stopwatch = Stopwatch.StartNew();
        var stats = runner.Run();
        stopwatch.Stop();
        
        // S2-specific validation
        var validationResult = await ValidateS2Performance(stats);
        
        // Generate S2-specific reports
        await GenerateS2PerformanceReport(stats, validationResult);
        
        return new S2TestResult
        {
            Stats = stats,
            ValidationResult = validationResult,
            Duration = stopwatch.Elapsed,
            S2Metrics = await _metricsCollector.GetS2Metrics()
        };
    }
    
    private async Task<S2ValidationResult> ValidateS2Performance(NodeStats stats)
    {
        var result = new S2ValidationResult();
        
        foreach (var scenario in stats.ScenarioStats)
        {
            // Standard performance validation
            var standardValidation = ValidateStandardMetrics(scenario);
            
            // S2-specific validation
            var s2Validation = await ValidateS2Metrics(scenario);
            
            result.ScenarioResults[scenario.ScenarioName] = new S2ScenarioValidation
            {
                StandardValidation = standardValidation,
                S2Validation = s2Validation
            };
        }
        
        return result;
    }
    
    private async Task<S2MetricsValidation> ValidateS2Metrics(ScenarioStats scenario)
    {
        var validation = new S2MetricsValidation();
        
        // Check stream sequence integrity
        var sequenceGaps = await _metricsCollector.GetSequenceGaps(scenario.ScenarioName);
        if (sequenceGaps.Any())
        {
            validation.Violations.Add($"Sequence gaps detected: {string.Join(", ", sequenceGaps)}");
        }
        
        // Validate timestamp ordering
        var timestampViolations = await _metricsCollector.GetTimestampViolations(scenario.ScenarioName);
        if (timestampViolations > _config.S2Thresholds.MaxTimestampViolations)
        {
            validation.Violations.Add($"Timestamp ordering violations: {timestampViolations}");
        }
        
        // Check stream throughput
        var streamThroughput = await _metricsCollector.GetStreamThroughput(scenario.ScenarioName);
        if (streamThroughput < _config.S2Thresholds.MinStreamThroughputRps)
        {
            validation.Violations.Add($"Stream throughput too low: {streamThroughput:F1} RPS");
        }
        
        validation.IsValid = validation.Violations.Count == 0;
        return validation;
    }
}

// S2-specific configuration
public class S2LoadTestConfiguration
{
    public string TestName { get; set; } = "S2 Load Test";
    public string Environment { get; set; } = "development";
    public string ReportDirectory { get; set; } = "S2Reports";
    public string PrometheusEndpoint { get; set; } = "http://localhost:9090";
    public string[] S2Endpoints { get; set; } = Array.Empty<string>();
    public string[] MonitoredStreamPatterns { get; set; } = Array.Empty<string>();
    
    public S2PerformanceThresholds S2Thresholds { get; set; } = new();
}

public class S2PerformanceThresholds
{
    public double MaxAverageLatencyMs { get; set; } = 100; // S2 is fast
    public double MaxP95LatencyMs { get; set; } = 250;
    public double MaxErrorRate { get; set; } = 0.001; // 0.1% for S2
    public double MinStreamThroughputRps { get; set; } = 1000;
    public int MaxTimestampViolations { get; set; } = 10;
    public int MaxSequenceGaps { get; set; } = 0; // S2 guarantees no gaps
}
```

### Comprehensive S2 Trading Platform Test

```csharp
// Real-world S2 trading platform load test
public class S2TradingPlatformLoadTest : S2LoadTestBase
{
    public S2TradingPlatformLoadTest(IConfiguration configuration) : base(configuration) { }
    
    public async Task<S2TestResult> ExecuteComprehensiveTradingTest()
    {
        _logger.LogInformation("🏦 Starting comprehensive S2 trading platform test");
        
        var scenarios = new[]
        {
            CreateMarketDataIngestionScenario(),
            CreateHighFrequencyTradingScenario(),
            CreateRiskManagementScenario(),
            CreateComplianceAuditingScenario()
        };
        
        var runner = CreateS2Runner(scenarios)
            .WithMinimumRunDuration(TimeSpan.FromMinutes(10))
            .WithMaximumRunDuration(TimeSpan.FromMinutes(60));
            
        return await ExecuteWithS2Validation(runner);
    }
    
    private Scenario CreateMarketDataIngestionScenario()
    {
        var streamClientFactory = S2ClientFactory.CreateStreamServiceFactory(_config.S2Endpoints[0]);
        
        return Scenario.Create("s2_market_data_ingestion", async context =>
        {
            var streamClient = streamClientFactory.GetClient(context.ScenarioInfo);
            
            try
            {
                var symbols = new[] { "AAPL", "GOOGL", "MSFT", "TSLA", "AMZN", "NVDA", "SPY", "QQQ", "BTC/USD", "ETH/USD" };
                var symbol = symbols[context.InvocationNumber % symbols.Length];
                var streamName = $"market-data/equities/{symbol}/Level1";
                
                // Step 1: Ingest real-time market data
                await Step.Run("ingest_market_data", context, async () =>
                {
                    var marketDataBatch = GenerateMarketDataBatch(symbol, 10); // 10 ticks per batch
                    
                    var appendRequest = new AppendRequest
                    {
                        Input = new AppendInput
                        {
                            Stream = streamName,
                            Records = { marketDataBatch }
                        }
                    };
                    
                    var response = await streamClient.AppendAsync(appendRequest);
                    
                    return Response.Ok(
                        payload: new { 
                            Symbol = symbol,
                            RecordCount = marketDataBatch.Count,
                            StartSeqNum = response.Output.StartSeqNum,
                            StreamLatency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.Output.StartTimestamp
                        },
                        sizeBytes: marketDataBatch.Sum(r => r.Body.Length)
                    );
                });
                
                return Response.Ok();
            }
            catch (RpcException ex)
            {
                return Response.Fail($"Market data ingestion failed: {ex.StatusCode}");
            }
        })
        .WithClientFactory(streamClientFactory)
        .WithLoadSimulations(
            // Market data ingestion - high sustained throughput
            Simulation.RampingInject(rate: 1000, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2)),
            Simulation.InjectPerSec(rate: 1000, during: TimeSpan.FromMinutes(20)),
            Simulation.RampingInject(rate: 500, interval: TimeSpan.FromSeconds(1), during: TimeSpan.FromMinutes(2))
        );
    }
    
    private Scenario CreateHighFrequencyTradingScenario()
    {
        var streamClientFactory = S2ClientFactory.CreateStreamServiceFactory(_config.S2Endpoints[0]);
        
        return Scenario.Create("s2_high_frequency_trading", async context =>
        {
            var streamClient = streamClientFactory.GetClient(context.ScenarioInfo);
            
            try
            {
                var algorithmId = $"ALGO_{context.InvocationNumber % 10}";
                var streamName = "trading/orders/high-frequency";
                
                // Step 1: HFT order submission burst
                await Step.Run("hft_order_burst", context, async () =>
                {
                    using var streamingCall = streamClient.AppendSession();
                    
                    var orderCount = Random.Shared.Next(20, 100);
                    var orders = new List<AppendRecord>();
                    
                    // Generate HFT order burst
                    for (int i = 0; i < orderCount; i++)
                    {
                        var order = CreateHFTOrder(context.InvocationNumber, i, algorithmId);
                        orders.Add(new AppendRecord
                        {
                            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                            Headers = 
                            {
                                new Header { Name = Encoding.UTF8.GetBytes("algorithm-id"), Value = Encoding.UTF8.GetBytes(algorithmId) },
                                new Header { Name = Encoding.UTF8.GetBytes("order-type"), Value = Encoding.UTF8.GetBytes("HFT") }
                            },
                            Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(order))
                        });
                    }
                    
                    // Submit all orders in a single batch for maximum performance
                    await streamingCall.RequestStream.WriteAsync(new AppendSessionRequest
                    {
                        Input = new AppendInput
                        {
                            Stream = streamName,
                            Records = { orders }
                        }
                    });
                    
                    await streamingCall.RequestStream.CompleteAsync();
                    
                    // Process confirmation
                    await foreach (var response in streamingCall.ResponseStream.ReadAllAsync())
                    {
                        var orderLatency = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - response.Output.StartTimestamp;
                        
                        return Response.Ok(
                            payload: new { 
                                OrderCount = orderCount,
                                AlgorithmId = algorithmId,
                                LatencyMs = orderLatency,
                                SequenceRange = $"{response.Output.StartSeqNum}-{response.Output.EndSeqNum}"
                            }
                        );
                    }
                    
                    return Response.Fail("No confirmation received");
                });
                
                return Response.Ok();
            }
            catch (RpcException ex)
            {
                return Response.Fail($"HFT trading failed: {ex.StatusCode}");
            }
        })
        .WithClientFactory(streamClientFactory)
        .WithLoadSimulations(
            // HFT pattern - burst of high-frequency activity
            Simulation.InjectPerSec(rate: 200, during: TimeSpan.FromMinutes(15))
        );
    }
    
    private static List<AppendRecord> GenerateMarketDataBatch(string symbol, int tickCount)
    {
        var records = new List<AppendRecord>();
        var basePrice = GetBasePrice(symbol);
        
        for (int i = 0; i < tickCount; i++)
        {
            var tick = new
            {
                Symbol = symbol,
                Price = basePrice + (decimal)(Random.Shared.NextDouble() * 2 - 1), // +/- $1
                Size = Random.Shared.Next(100, 10000),
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Exchange = "NASDAQ",
                ConditionCodes = new[] { "T", "I" } // Regular trade
            };
            
            records.Add(new AppendRecord
            {
                Timestamp = (ulong)tick.Timestamp,
                Headers = 
                {
                    new Header { Name = Encoding.UTF8.GetBytes("symbol"), Value = Encoding.UTF8.GetBytes(symbol) },
                    new Header { Name = Encoding.UTF8.GetBytes("data-type"), Value = Encoding.UTF8.GetBytes("trade") }
                },
                Body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(tick))
            });
        }
        
        return records;
    }
    
    private static decimal GetBasePrice(string symbol) => symbol switch
    {
        "AAPL" => 150m,
        "GOOGL" => 2500m,
        "MSFT" => 300m,
        "TSLA" => 200m,
        "AMZN" => 3000m,
        "NVDA" => 400m,
        "SPY" => 400m,
        "QQQ" => 300m,
        "BTC/USD" => 45000m,
        "ETH/USD" => 3000m,
        _ => 100m
    };
}

public class S2TestResult
{
    public NodeStats Stats { get; set; }
    public S2ValidationResult ValidationResult { get; set; }
    public TimeSpan Duration { get; set; }
    public S2StreamMetrics S2Metrics { get; set; }
}
```

---

## Quick gRPC Reference {#grpc-reference}

### NBomber gRPC Cheat Sheet

**🔧 gRPC Channel Setup:**
```csharp
// Optimized gRPC channel
var channel = GrpcChannel.ForAddress("https://grpc.service.com", new GrpcChannelOptions
{
    MaxReceiveMessageSize = 16 * 1024 * 1024,
    MaxSendMessageSize = 16 * 1024 * 1024,
    HttpHandler = new SocketsHttpHandler
    {
        KeepAlivePingDelay = TimeSpan.FromSeconds(30),
        EnableMultipleHttp2Connections = true
    }
});
```

**📡 gRPC Patterns:**
```csharp
// Unary
var response = await client.UnaryCallAsync(request);

// Server Streaming  
await foreach (var response in client.ServerStreamCall(request).ResponseStream.ReadAllAsync())
{
    // Process streaming data
}

// Client Streaming
using var call = client.ClientStreamCall();
await call.RequestStream.WriteAsync(request);
await call.RequestStream.CompleteAsync();
var response = await call;

// Bidirectional Streaming
using var call = client.BidirectionalStreamCall();
var sendTask = Task.Run(async () => {
    await call.RequestStream.WriteAsync(request);
    await call.RequestStream.CompleteAsync();
});
await foreach (var response in call.ResponseStream.ReadAllAsync())
{
    // Process responses
}
await sendTask;
```

**⚡ Performance Tips:**
- Use ClientFactory for connection reuse
- Set appropriate message size limits
- Enable HTTP/2 multiplexing
- Configure keepalive for long streams
- Handle RpcException with specific StatusCodes
- Use compression for large payloads

**🚨 Common gRPC Issues:**
- **DeadlineExceeded**: Increase timeout or optimize calls
- **Unavailable**: Implement retry with backoff  
- **ResourceExhausted**: Reduce load or increase limits
- **InvalidArgument**: Check protobuf field validation

---

## Conclusion: gRPC Load Testing Mastery

NBomber provides the most comprehensive gRPC load testing capabilities in the .NET ecosystem. By understanding the **four gRPC patterns**, implementing **proper connection management**, and leveraging **S2 Stream Store's advanced features**, you can build sophisticated performance testing strategies that mirror real-world gRPC usage.

**Key gRPC Testing Principles:**
- **Connection Reuse** - Use ClientFactory for realistic connection patterns
- **Stream Lifecycle** - Test complete streaming scenarios, not just individual calls
- **Error Resilience** - Handle gRPC-specific errors with appropriate retry strategies
- **Resource Management** - Properly dispose channels and manage HTTP/2 connections
- **Realistic Data** - Use protobuf payloads that match production sizes and patterns

**The gRPC Advantage with NBomber:**
- Native .NET gRPC client integration
- Full support for all streaming patterns
- Advanced HTTP/2 connection management
- Real-time stream monitoring and metrics
- Production-ready error handling and resilience

🚀 **Master gRPC performance testing with NBomber and unlock the full potential of your streaming architectures!**
