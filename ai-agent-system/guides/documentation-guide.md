# Comprehensive Documentation Standards v1.0

Last updated: 2025-06-09

> **Usage**: Reference this guide when writing XML documentation, creating API references, or establishing documentation patterns for public APIs.

## ? Integration with Other Resources

**Core Standards**: This guide extends the basic XML documentation requirements in `core-prompt.md` with comprehensive examples  
**Project Context**: Combine with project-specific instructions for domain terminology and usage patterns  
**Performance Documentation**: Reference `guides/performance-guide.md` for documenting performance characteristics  
**Testing Documentation**: Reference `guides/testing-guide.md` for documenting test APIs and testing strategies

## ? General Documentation Principles

### Quality Standards
- **Every public class, method, property, event, and interface** must have complete XML documentation
- **Documentation must be clear, accurate, and structured** for readability and maintainability
- **Use advanced XML doc features** to organize information and improve discoverability
- **Follow consistent style and formatting** across all documentation
- **Write for the developer audience** - focus on practical usage and implementation guidance

### Documentation Philosophy
- **Explain the 'why', not just the 'what'** - Provide context and reasoning
- **Include realistic examples** that developers can adapt to their scenarios
- **Document edge cases and error conditions** that aren't obvious from the method signature
- **Cross-reference related functionality** to help developers discover related features

## ?? XML Documentation Requirements

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
```csharp
/// <summary>
/// Appends a new event to the specified stream with optimistic concurrency control.
/// </summary>
public async Task<AppendResult> AppendEventAsync(string streamId, EventData eventData, int expectedVersion = -1) {
    // Implementation
}
```

**Guidelines:**
- Provide a **concise, informative summary** of the member's purpose
- Use **active voice** and present tense
- **Start with a verb** for methods (e.g., "Calculates", "Retrieves", "Validates")
- **Describe the purpose** for properties and classes
- **Keep it under 120 characters** when possible

#### `<remarks>` - Detailed Explanations
```csharp
/// <summary>
/// Calculates the area of a rectangle.
/// </summary>
/// <remarks>
/// <para>
/// Multiplies <paramref name="width"/> and <paramref name="height"/> to compute the area.
/// This method uses double precision arithmetic to ensure accuracy with decimal measurements.
/// </para>
/// <para>
/// <b>Usage notes:</b>
/// <list type="bullet">
///   <item><description>Both dimensions must be non-negative values.</description></item>
///   <item><description>Returns zero if either dimension is zero.</description></item>
///   <item><description>For very large values, consider potential overflow conditions.</description></item>
/// </list>
/// </para>
/// <para>
/// For calculating perimeter of the same rectangle, use <see cref="CalculatePerimeter"/>.
/// </para>
/// </remarks>
public double CalculateArea(double width, double height) {
    // Implementation
}
```

**Guidelines:**
- Use for **detailed explanations, usage notes, or background information**
- **Break up content with `<para>`** for real paragraphs
- Include **performance considerations** when relevant
- Document **thread safety** characteristics
- Explain **complex algorithms** or business logic

#### `<param>` - Parameter Documentation
```csharp
/// <summary>
/// Retrieves events from a stream with filtering and pagination options.
/// </summary>
/// <param name="streamId">The unique identifier of the stream to read from. Cannot be null or empty.</param>
/// <param name="startPosition">The position in the stream to start reading from. Use 0 for the beginning, -1 for the end.</param>
/// <param name="maxEvents">The maximum number of events to retrieve. Must be between 1 and 1000. Default is 100.</param>
/// <param name="direction">The direction to read events. Forward reads from start position onwards, backward reads towards the beginning.</param>
/// <param name="cancellationToken">A token to cancel the operation. Defaults to <see cref="CancellationToken.None"/>.</param>
public async Task<EventBatch> ReadEventsAsync(
    string streamId, 
    int startPosition = 0, 
    int maxEvents = 100, 
    ReadDirection direction = ReadDirection.Forward,
    CancellationToken cancellationToken = default) {
    // Implementation
}
```

**Guidelines:**
- **Document every parameter** with its purpose and constraints
- **Specify valid ranges** for numeric parameters
- **Indicate nullability** and empty value handling
- **Document default values** and their meaning
- Use **`<paramref>`** for parameter references within other tags

#### Documentation Review Checklist

### Before Publishing
- [ ] All public members have `<summary>` tags
- [ ] Parameters are documented with constraints and valid ranges
- [ ] Return values are clearly explained
- [ ] All possible exceptions are documented with conditions
- [ ] Examples are realistic and compilable
- [ ] Cross-references are accurate and helpful
- [ ] Grammar and spelling are correct
- [ ] Terminology is consistent throughout
- [ ] No references to "this method" or "this class"
- [ ] Property descriptions avoid "Gets or sets" language