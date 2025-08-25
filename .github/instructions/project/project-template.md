# Project Instructions Template v2.0

Last updated: 2025-06-09

> **Template Usage**: This file should be customized for each specific project. Replace bracketed placeholders with project-specific information and update the architecture sections to match your project's domain and patterns.

## Version Information

**Instructions Version**: v[MAJOR].[MINOR].[PATCH]  
**Project Phase**: [Development/Beta/Production]  
**Last Updated**: [YYYY-MM-DD]  

### **Changelog**
- **v[X.Y.Z]** ([Date]): [Brief description of changes]

### **Recommended Specialized Guides**
Based on project analysis, consider loading:
- **Performance**: `guides/performance-guide.md` for high-throughput scenarios
- **Testing**: `guides/testing-guide.md` for comprehensive test strategies  
- **Documentation**: `guides/documentation-guide.md` for library/SDK projects
- **C# Details**: `guides/csharp-standards-guide.md` for comprehensive formatting patterns

## Project Overview

**[PROJECT_NAME]** provides [brief description of what the project does and its primary value proposition]. This [library/application/service] enables developers to [main capabilities and benefits].

### Key Features

- **[Feature 1]**: [Description of key feature and its benefits]
- **[Feature 2]**: [Description of key feature and its benefits]
- **[Feature 3]**: [Description of key feature and its benefits]
- **[Feature 4]**: [Description of key feature and its benefits]

### Technical Foundation

- **[Runtime/Framework]**: Built for [.NET 8.0/9.0 / Node.js / etc.] with [language version] features
- **[Protocol/Communication]**: [Description of main communication or data protocol]
- **[API Design]**: [Async-first / REST / GraphQL / etc.] API support for [key characteristics]
- **[Compatibility]**: [Cross-platform / Browser support / etc.] environments

### Ideal Use Cases

- **[Use Case 1]**: [Specific scenario and how the project addresses it]
- **[Use Case 2]**: [Specific scenario and how the project addresses it]
- **[Use Case 3]**: [Specific scenario and how the project addresses it]

### Implementation Goals

[Describe the overarching implementation philosophy and what the project aims to deliver in terms of developer experience, performance, reliability, etc.]

---

## Architecture Overview

### Key Architectural Concepts

#### 1. [Core Domain Concept 1]
* [Explanation of fundamental concept 1]
* [How this concept impacts design decisions]
* [Key constraints or requirements this creates]

#### 2. [Core Domain Concept 2]  
* [Explanation of fundamental concept 2]
* [How this concept impacts design decisions]
* [Key constraints or requirements this creates]

#### 3. [Core Domain Concept 3]
* [Explanation of fundamental concept 3]
* [How this concept impacts design decisions]
* [Key constraints or requirements this creates]

### Layered Architecture

- **[Layer 1 Name]**: [Description of responsibilities and scope]
- **[Layer 2 Name]**: [Description of responsibilities and scope]
- **[Layer 3 Name]**: [Description of responsibilities and scope]
- **[Layer 4 Name]**: [Description of responsibilities and scope]

## Core Components

### [Component Category 1]
- **[Component 1]**: [Description and responsibilities]
- **[Component 2]**: [Description and responsibilities]
- **[Component 3]**: [Description and responsibilities]

### [Component Category 2]
* [Description of component category purpose]
* [Key configuration or setup requirements]
* [Integration points with other components]

### [Component Category 3]
* [Description of component category purpose]
* [Key behavioral characteristics]
* [Performance or scalability considerations]

> **Performance Note**: For performance-critical components, reference `guides/performance-guide.md` for advanced optimization techniques.

## Integration Patterns

### [Pattern 1 Name]
* [Description of when and how to use this pattern]
* [Key responsibilities and boundaries]
* [Common implementation considerations]

### [Pattern 2 Name]
* [Description of when and how to use this pattern]
* [Key responsibilities and boundaries]
* [Common implementation considerations]

### [Pattern 3 Name]
* [Description of when and how to use this pattern]
* [Key responsibilities and boundaries]
* [Common implementation considerations]

## Design Principles

### [Principle 1]
* [Description of the principle]
* [How it applies to this project]
* [Examples of adherence to this principle]

### [Principle 2]
* [Description of the principle]
* [How it applies to this project]
* [Examples of adherence to this principle]

### [Principle 3]
* [Description of the principle]
* [How it applies to this project]
* [Examples of adherence to this principle]

### [Principle 4]
* [Description of the principle]
* [How it applies to this project]
* [Examples of adherence to this principle]

---

## Project Structure

All paths and files in the project's .gitignore file must not be included in the agent's context or memory.
The agent should only focus on the files and directories that are relevant to the [PROJECT_NAME] development.

### Root Level Organization

```
[src/]                         # [Description of source directory]
[test/]                        # [Description of test directory]
[docs/]                        # [Description of documentation]
[examples/]                    # [Description of examples/samples]
[tools/]                       # [Description of build/dev tools]
[config/]                      # [Description of configuration]
[other directories as needed]
```

## [Project Domain]-Specific Principles

### [Domain Concept 1]
* [Key principle or constraint from the domain]
* [How this impacts implementation decisions]
* [Examples of proper adherence]

### [Domain Concept 2]
* [Key principle or constraint from the domain]
* [How this impacts implementation decisions]
* [Examples of proper adherence]

### [Usage Pattern Category]
* [Description of common usage pattern]
* [Key steps or sequence of operations]
* [Important considerations for this pattern]

### API Design Principles
* [Principle 1 for API design specific to this project]
* [Principle 2 for API design specific to this project]
* [Principle 3 for API design specific to this project]

## Common Pitfalls to Avoid

### Anti-Patterns
* **[Anti-pattern 1]**: [Description and why it's problematic]
* **[Anti-pattern 2]**: [Description and why it's problematic]
* **[Anti-pattern 3]**: [Description and why it's problematic]
* **[Anti-pattern 4]**: [Description and why it's problematic]

### Performance Traps
* **[Performance trap 1]**: [Description and impact]
* **[Performance trap 2]**: [Description and impact]
* **[Performance trap 3]**: [Description and impact]
* **[Performance trap 4]**: [Description and impact]

> **Performance Alert**: For detailed optimization strategies specific to these patterns, reference `guides/performance-guide.md`

## Implementation Priorities

1. **[Priority 1]**: [Description of what this means for implementation]
2. **[Priority 2]**: [Description of what this means for implementation]
3. **[Priority 3]**: [Description of what this means for implementation]
4. **[Priority 4]**: [Description of what this means for implementation]
5. **[Priority 5]**: [Description of what this means for implementation]

## Development Workflow Integration

### **With Core Standards**
These project instructions work with `core-prompt.md` which provides:
- C# coding standards and formatting rules
- Adaptive pattern analysis protocol
- General performance guidelines
- Documentation requirements
- Workflow protocols

### **Specialized Resource Integration**
- **C# Formatting**: Reference `guides/csharp-standards-guide.md` for comprehensive formatting and organization patterns
- **Testing**: Reference `guides/testing-guide.md` for TUnit/Shouldly/FakeItEasy patterns
- **Performance**: Reference `guides/performance-guide.md` for SIMD, memory management, concurrent patterns
- **Documentation**: Reference `guides/documentation-guide.md` for comprehensive XML documentation

### **Context Loading Examples**
```bash
# General development
core-prompt.md + project-instructions-[PROJECT].md

# Performance work  
core-prompt.md + project-instructions-[PROJECT].md + guides/performance-guide.md

# Testing setup
core-prompt.md + project-instructions-[PROJECT].md + guides/testing-guide.md

# Detailed C# formatting questions
core-prompt.md + project-instructions-[PROJECT].md + guides/csharp-standards-guide.md
```

---

## Example Implementation (KurrentDB Reference)

### KurrentDB Project Overview
The KurrentDB .NET Client provides a production-ready interface for .NET applications to interact with KurrentDB - an event-native database purpose-built for event sourcing. This library enables developers to efficiently store, retrieve, and process event streams with minimal configuration.

### Event Sourcing Foundation
* Events are immutable facts representing changes
* Streams organize related events logically  
* The global event log maintains consistent ordering
* Events can be replayed to rebuild state at any point in time

### Client Usage Patterns
* Basic pattern: configure client ? connect ? append/read events ? handle responses
* Respect KurrentDB's append-only nature and immutable event model
* Use optimistic concurrency for handling conflicting writes
* Subscribe to streams for real-time event notifications

### KurrentDB Anti-Patterns
* Treating KurrentDB like a traditional database with updates and deletes
* Using event data as a state store instead of deriving state from events
* Ignoring optimistic concurrency in multi-writer scenarios
* Creating overly large events that impact performance

### KurrentDB Performance Traps
* Reading entire streams when only recent events are needed
* Not batching events for high-throughput scenarios
* Subscribing to high-volume streams without proper backpressure
* Not reusing client connections appropriately