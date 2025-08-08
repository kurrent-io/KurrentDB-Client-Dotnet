# Comprehensive Documentation Standards v2.0

Last updated: 2025-07-01

> **Usage**: Reference this guide when writing XML documentation, creating API references, or establishing documentation patterns for public APIs.

## 🎯 General Documentation Principles

### Quality Standards
- **Every public class, method, property, event, and interface** must have complete XML documentation
- **Documentation must be human-readable and domain-focused** - write as if for a public documentation website
- **Use natural language that feels human-written** - avoid robotic or template-like phrasing
- **Ask clarifying questions when domain context is unclear** to provide better documentation
- **Focus on business value and domain meaning** rather than technical implementation details

### Documentation Philosophy
- **Write for humans first** - documentation should be exportable to public websites
- **Explain the 'why' and domain context**, not just the 'what'
- **Use domain-specific language** that matches the business context
- **Provide practical guidance** that helps developers understand purpose and usage
- **Never expose internal implementation details** in public documentation

## 📋 When to Apply Different Documentation Levels

### Documentation Triggers

#### **Full Documentation Required:**
- **Public methods and properties**
- **Any member marked with `[PublicAPI]` attribute** (overrides complexity considerations)
- **Complex business logic or algorithms**
- **Performance-critical operations**
- **Methods with significant default behaviors**

#### **Minimal Documentation Acceptable:**
- **Internal or private members**
- **Simple property getters/setters** (but still domain-focused)
- **Obvious CRUD operations** (when internal)

### Upfront Guidance for Domain Understanding

When encountering unclear context, ask clarifying questions:

**Domain Context Questions:**
> "I notice this class deals with [observed pattern], but I need to understand the domain better. Is this for:
> - [Option 1 based on code clues]?
> - [Option 2 based on code clues]?
> - [Option 3 based on code clues]?
    > This will help me write more meaningful documentation."

**Business Purpose Questions:**
> "I see this method processes [observed entity], but what's the business goal? Is this:
> - [Business purpose A]?
> - [Business purpose B]?
    > Understanding the purpose will improve the documentation quality."

**Performance Context Questions:**
> "This method looks performance-critical. Should I document:
> - Memory usage patterns?
> - Expected throughput?
> - Concurrency considerations?
    > What performance aspects matter most to users?"

## XML Documentation Requirements

### Special Comment Tags

* `TODO`: For incomplete features or bugs (link to issue tracker)
* `FIXME`: For known issues that need immediate attention
* `NOTE`: For important information or clarifications
* `HACK`: For temporary workarounds
* `REVIEW`: For code that needs review or refactoring
* `OPTIMIZE`: For code that can be improved for performance
* `DEPRECATED`: For code no longer recommended for use

### Core Documentation Tags

#### `<summary>` - Required for All Public Members

**Properties - Domain-Focused Approach:**
```csharp
public class TradingOrder {
    /// <summary>Financial instrument identifier (e.g., AAPL, GOOGL).</summary>
    public string Symbol { get; set; }
    
    /// <summary>Number of shares to trade.</summary>
    public decimal Quantity { get; set; }
    
    /// <summary>Execution strategy for the order.</summary>
    public OrderType Type { get; set; }
}
```

**Methods - Action and Purpose:**
```csharp
/// <summary>
/// Processes high-frequency trading orders with real-time risk validation.
/// </summary>
public async Task<ProcessResult> ProcessHFTOrderAsync(TradingOrder order, MarketConditions conditions) {
    // Implementation
}
```

**Classes - Business Context:**
```csharp
/// <summary>
/// Configuration settings for establishing and managing Elasticsearch connections in Kafka Connect.
/// </summary>
public class ElasticsearchConnectorConfig {
    // Implementation
}
```

**Property Documentation Guidelines:**
- ❌ **Avoid**: "Gets or sets the symbol for this order"
- ❌ **Avoid**: "The string that identifies the Symbol"
- ❌ **Avoid**: "The quantity as a decimal value"
- ✅ **Prefer**: "Financial instrument identifier (e.g., AAPL, GOOGL)"
- ✅ **Prefer**: "Number of shares to trade"
- ✅ **Prefer**: "Maximum allowed slippage percentage"

**Method Documentation Guidelines:**
- **Start with a verb** for methods (e.g., "Calculates", "Retrieves", "Validates")
- **Focus on business purpose** rather than technical implementation
- **Use domain language** that matches the business context
- **Keep summaries under 120 characters** when possible

#### `<remarks>` - Detailed Explanations with Structured Defaults

**Template for Default Values and Importance:**
```csharp
/// <summary>
/// Record batch size for optimized Elasticsearch indexing operations.
/// </summary>
/// <remarks>
/// <para><b>Default:</b> 2000 records</para>
/// <para><b>Importance:</b> Medium</para>
/// <para><b>Valid Range:</b> 1 to 1,000,000 records</para>
/// <para>Larger batches improve throughput but increase memory usage and latency.</para>
/// </remarks>
public int BatchSize { get; set; } = 2000;
```

**Complex Algorithm Documentation:**
```csharp
/// <summary>
/// Calculates portfolio risk score using Monte Carlo simulation.
/// </summary>
/// <remarks>
/// <para>
/// Uses 10,000 simulation iterations to model potential portfolio losses under various market conditions.
/// The algorithm incorporates correlation matrices, volatility clustering, and tail risk measures.
/// </para>
/// <para>
/// <b>Performance:</b> Typical execution time is 200-500ms for portfolios with up to 1000 positions.
/// Consider caching results for portfolios that change infrequently.
/// </para>
/// </remarks>
public decimal CalculateRiskScore(Portfolio portfolio) {
    // Implementation
}
```

**Authentication and Security Context:**
```csharp
/// <summary>
/// Authentication username for Elasticsearch cluster access.
/// </summary>
/// <remarks>
/// <para><b>Default:</b> null (no authentication)</para>
/// <para><b>Importance:</b> Medium</para>
/// <para>Authentication is only performed when both username and password are provided.</para>
/// </remarks>
public string? ConnectionUsername { get; set; }
```

**Remarks Usage Guidelines:**
- Use for **detailed explanations, usage notes, or background information**
- **Break up content with `<para>`** for readable paragraphs
- Include **performance considerations** when relevant
- Document **business rules and constraints**
- Explain **complex algorithms** or domain logic
- **State defaults clearly** using the structured template
- Include **importance levels** to guide configuration priorities

#### `<param>` - Parameter Documentation

```csharp
/// <summary>
/// Executes trade orders with specified risk limits and market conditions.
/// </summary>
/// <param name="orders">Collection of trading orders to execute. Cannot be empty.</param>
/// <param name="riskLimits">Maximum exposure limits per asset class. Uses portfolio defaults if null.</param>
/// <param name="marketConditions">Current market volatility and liquidity data for execution optimization.</param>
/// <param name="cancellationToken">Cancellation token for early termination of long-running operations.</param>
public async Task<ExecutionResult> ExecuteTradesAsync(
    IEnumerable<TradingOrder> orders,
    RiskLimits? riskLimits = null,
    MarketConditions marketConditions = default,
    CancellationToken cancellationToken = default) {
    // Implementation
}
```

**Parameter Documentation Guidelines:**
- **Document every parameter** with its business purpose and constraints
- **Specify valid ranges** for numeric parameters
- **Indicate nullability** and default behavior
- **Use domain language** to explain parameter relationships
- Use **`<paramref>`** for parameter references within other tags

#### `<returns>` - Return Value Documentation

```csharp
/// <summary>
/// Validates trading order compliance with regulatory requirements.
/// </summary>
/// <returns>
/// Compliance result indicating whether the order meets regulatory standards,
/// including specific violation details if applicable.
/// </returns>
public ComplianceResult ValidateOrderCompliance(TradingOrder order) {
    // Implementation
}
```

### Advanced Documentation Patterns

#### Configuration Classes
```csharp
/// <summary>
/// Configuration settings for establishing and managing Elasticsearch connections in Kafka Connect.
/// </summary>
/// <remarks>
/// <para>
/// Defines connection parameters, authentication credentials, and performance tuning options 
/// for the Elasticsearch sink connector. This configuration enables reliable data streaming 
/// from Kafka topics to Elasticsearch indices with optimized batch processing.
/// </para>
/// <para>
/// <b>Usage:</b> Configure through Kafka Connect REST API or properties files for production deployments.
/// Supports both single-node and clustered Elasticsearch environments with automatic failover.
/// </para>
/// </remarks>
public class ElasticsearchConnectorConfig {
    /// <summary>
    /// Elasticsearch cluster endpoints for data ingestion.
    /// </summary>
    /// <remarks>
    /// <para><b>Default:</b> None (required)</para>
    /// <para><b>Importance:</b> High</para>
    /// <para>Supports multiple URLs for load balancing and failover. HTTPS protocol is automatically applied when any URL specifies https://</para>
    /// </remarks>
    public List<string> ConnectionUrl { get; set; }
}
```

#### Service Classes
```csharp
/// <summary>
/// Handles real-time processing and validation of high-frequency trading orders.
/// </summary>
/// <remarks>
/// <para>
/// Processes orders with sub-millisecond latency requirements while maintaining compliance
/// with market regulations. Integrates with risk management systems for real-time position monitoring.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All public methods are thread-safe and optimized for concurrent access.
/// </para>
/// </remarks>
public class HighFrequencyTradingProcessor {
    // Implementation
}
```

### Documentation Review Checklist

#### Before Publishing
- [ ] All public members have domain-focused `<summary>` tags
- [ ] No "gets or sets" language in property documentation
- [ ] Parameters are documented with business context and constraints
- [ ] Return values explain business meaning, not just types
- [ ] Default values use structured template with importance levels
- [ ] Complex algorithms have detailed `<remarks>` explanations
- [ ] Performance considerations are documented where relevant
- [ ] Cross-references use business terminology
- [ ] Grammar and spelling are correct
- [ ] Terminology is consistent throughout
- [ ] Language feels natural and human-written
- [ ] No internal implementation details are exposed

#### Domain Context Validation
- [ ] Documentation uses appropriate business/domain terminology
- [ ] Property descriptions provide meaningful context beyond type information
- [ ] Method purposes are clear from a business perspective
- [ ] Configuration options explain their impact on system behavior
- [ ] Related functionality is cross-referenced appropriately

### Importance Levels for Configuration

**High Importance:**
- Required configurations that prevent system startup
- Security-critical settings
- Primary business functionality enablers

**Medium Importance:**
- Performance tuning parameters
- Authentication credentials (when optional)
- Feature toggles with moderate impact

**Low Importance:**
- Logging levels and debugging options
- Minor performance optimizations
- Cosmetic or convenience settings

## Best Practices Summary

### Writing Human-Readable Documentation
- Write as if the documentation will be published on a public website
- Use natural language that sounds like a human expert wrote it
- Focus on helping developers understand purpose and usage
- Avoid robotic templates except for structured elements like defaults

### Domain-Focused Approach
- Understand the business context before writing documentation
- Use terminology that matches the domain (trading, healthcare, e-commerce, etc.)
- Explain the role and purpose of code elements in business terms
- Ask clarifying questions when domain context is unclear

### Structured Information
- Use the default value template consistently for configuration documentation
- Include importance levels to guide implementation priorities
- Provide performance context for optimization-sensitive code
- Document business rules and constraints clearly

Remember: Great documentation bridges the gap between code and business understanding. It should enable developers to make informed decisions about how to use APIs effectively in their specific contexts.
