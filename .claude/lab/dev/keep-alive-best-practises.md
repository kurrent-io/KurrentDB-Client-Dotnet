## Best Practices for Long-Running gRPC Services

**1. Use Streaming for Long-Lived Connections**
- Prefer streaming RPCs (server, client, or bidirectional) for long-running or continuous data flows. This avoids the overhead of repeatedly establishing connections and efficiently manages resources[1][8].

**2. Connection and Channel Management**
- Always reuse gRPC channels and stubs when possible. Creating new channels per request is costly and can exhaust resources quickly[1][8].
- For high-load scenarios, consider using a pool of gRPC channels and randomly distributing calls across them[6].

**3. Keepalive Pings**
- Use keepalive pings to prevent idle connections from being dropped by servers, proxies, or load balancers. This is especially important for long-running or idle connections[1][6][7].
- **Important:** Only enable keepalive pings if the server is configured to accept them, as too frequent pings can cause the server to close connections[4][5][6].

**4. Recommended Default Keepalive Settings (for .NET)**
- A good starting point for keepalive settings in .NET's `SocketsHttpHandler`:
    - `KeepAlivePingDelay = TimeSpan.FromSeconds(60)`
    - `KeepAlivePingTimeout = TimeSpan.FromSeconds(30)`
    - `PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan`
    - `EnableMultipleHttp2Connections = true` (if needed for high concurrency)
- Example configuration:
  ```csharp
  var handler = new SocketsHttpHandler
  {
      KeepAlivePingDelay = TimeSpan.FromSeconds(60),
      KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
      PooledConnectionIdleTimeout = Timeout.InfiniteTimeSpan,
      EnableMultipleHttp2Connections = true
  };
  ```
  This setup sends a keepalive ping every 60 seconds of inactivity and closes the connection if no response is received within 30 seconds[6][7].

**5. Deadlines and Timeouts**
- Always set deadlines (timeouts) for all RPCs. This ensures that both clients and servers can abort operations that take too long, freeing up resources and preventing indefinite hangs[9].
- Too short deadlines can cause premature failures; too long can tie up resources unnecessarily.

**6. Error Handling and Retries**
- Implement robust error handling using gRPC status codes.
- Use automatic retries with exponential backoff for transient errors[8][9].

**7. Monitoring and Logging**
- Monitor key metrics such as latency, error rates, and throughput.
- Use distributed tracing and centralized logging for troubleshooting and performance analysis[8].

**8. Network Infrastructure Awareness**
- Be aware of the idle timeout settings of any load balancers or proxies between your client and server. Adjust your keepalive settings to send pings more frequently than the shortest idle timeout in the path[2][7].

**9. Avoid Aggressive Client-Side Keepalives**
- Do not set client-side keepalive intervals lower than what the server allows. Always coordinate keepalive settings between client and server to avoid unexpected connection closures[2][4][5][6].

## Summary Table: Recommended Defaults for .NET Long-Running gRPC

| Setting                   | Recommended Value            | Notes                                              |
|---------------------------|-----------------------------|----------------------------------------------------|
| KeepAlivePingDelay        | 60 seconds                  | Interval between pings during inactivity           |
| KeepAlivePingTimeout      | 30 seconds                  | Time to wait for ping response                     |
| PooledConnectionIdleTimeout | InfiniteTimeSpan           | Prevents idle pool cleanup for long-lived channels |
| EnableMultipleHttp2Connections | true                   | For high concurrency scenarios                     |
| Deadline/Timeout per RPC  | Appropriate to operation    | Always set a deadline on every call                |

## Key Takeaways

- **Coordinate keepalive settings between client and server.**
- **Use streaming for long-running operations.**
- **Always set deadlines and monitor your services.**
- **Reuse channels and avoid aggressive keepalive intervals.**

These practices will help ensure reliability, performance, and resilience for long-running gRPC services in .NET[1][6][8][9].

Sources
[1] Performance Best Practices - gRPC https://grpc.io/docs/guides/performance/
[2] gRPC is easy to misconfigure (evanjones.ca) https://www.evanjones.ca/grpc-is-tricky.html
[3] Keepalive - gRPC https://grpc.io/docs/guides/keepalive/
[4] Keepalive User Guide for gRPC Core (and dependents) https://grpc.github.io/grpc/core/md_doc_keepalive.html
[5] Document client keepalive as being potentially dangerous #25713 https://github.com/grpc/grpc/issues/25713
[6] Performance best practices with gRPC | Microsoft Learn https://learn.microsoft.com/en-us/aspnet/core/grpc/performance?view=aspnetcore-9.0
[7] gRPC streaming call which takes longer than 2 minutes is killed by ... https://stackoverflow.com/questions/71885593/grpc-streaming-call-which-takes-longer-than-2-minutes-is-killed-by-hardware-rou
[8] How to Use gRPC Effectively: Best Practices You Should Follow https://www.bytesizego.com/blog/effective-grpc-usage-go
[9] Small note of gRPC Best Practice @ CoreOSFest 2017 - GitHub Gist https://gist.github.com/tcnksm/eb78363fda067fdccd06ee8e7455b38b
[10] gRPC: What are the best practices for long-running streaming? https://stackoverflow.com/questions/63633332/grpc-what-are-the-best-practices-for-long-running-streaming
[11] gRPC connection: use keepAlive or idleTimeout? - Stack Overflow https://stackoverflow.com/questions/57930529/grpc-connection-use-keepalive-or-idletimeout
[12] Boosting gRPC Performance | A Practical Guide for High ... https://www.bytesizego.com/blog/grpc-performance
[13] Lessons learned from running a large gRPC mesh at Datadog https://www.datadoghq.com/blog/grpc-at-datadog/
[14] gRPC: Architecture, Security Best Practices, and a Comparison with ... https://schimizu.com/grpc-architecture-security-best-practices-and-a-comparison-with-restful-apis-part-ii-17cce1107af4
[15] Keep alive missing? · Issue #770 · grpc/grpc-dotnet - GitHub https://github.com/grpc/grpc-dotnet/issues/770
[16] configurable HTTP/2 PING timeouts in HttpClient #31198 - GitHub https://github.com/dotnet/runtime/issues/31198
[17] Reliable gRPC services with deadlines and cancellation https://learn.microsoft.com/en-us/aspnet/core/grpc/deadlines-cancellation?view=aspnetcore-9.0
[18] gRPC Keepalive | Xuan Wang - YouTube https://www.youtube.com/watch?v=yPNHn6lXndo
