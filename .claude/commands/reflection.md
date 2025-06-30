# /reflection Command Specification v3.0

**Last Updated**: 2025-07-23  
**Version**: 2.0  
**Changes**: Custom command for analyzing and improving AI code assistant instructions

## Command Syntax
```bash
/reflection [options]
```

## Options
- `--dry-run` - Show analysis and suggestions without applying any changes to files. Outputs structured recommendations for review.

## Goal
Analyze and improve AI code assistant instructions by examining chat history, current instruction files, and project context to identify inconsistencies, gaps, and optimization opportunities that will enhance the assistant's performance and consistency for the KurrentDB .NET Client project.

## Return format
Structure your analysis and recommendations using the following format:

**Context Discovery Section:**
- List all discovered project files, structure, and available tools
- Identify project type and technology stack
- Document AI agent system components found

**Analysis Section:**
- Enumerate specific issues identified from chat history review
- Detail inconsistencies between instruction files
- Highlight gaps between available tools and documented capabilities
- Note misunderstandings or suboptimal responses patterns

**Improvements Section:**
For each proposed enhancement:
- Target file requiring modification
- Specific section to be updated
- Exact new or revised instruction text
- Clear explanation of how the change addresses identified issues

**Implementation Summary:**
- Overview of files to be updated
- Key changes and their expected impact
- TodoWrite tracking for implementation tasks

## Warnings
- Do not make assumptions about project structure without first performing discovery
- Avoid suggesting changes that contradict the core purpose of the AI assistant
- Ensure all proposed modifications maintain consistency across instruction files
- Do not implement changes without explicit human approval during the interaction phase
- Be cautious of over-engineering solutions - focus on addressing actual observed issues
- Verify that suggested MCP tool integrations align with available server configurations
- Consider that instruction changes may have unintended consequences on other workflows
- Pay special attention to the dual namespace transition (KurrentDB.Client to Kurrent.Client) when analyzing inconsistencies
- Be mindful of the TUnit migration from xUnit when reviewing test-related guidance
- Consider the performance-critical nature of this event-sourcing database client when suggesting improvements
- Ensure any changes align with the documented architectural patterns (Lock/Unlock Immutability, Result Pattern)
- Account for the dual API architecture (legacy exception-based vs modern Result-based) when suggesting improvements
- Be aware of the schema registry integration complexity and ensure guidance covers both JSON and Protobuf serialization patterns
- Consider the gRPC protocol evolution from v1 to v2 and ensure instructions handle both protocol versions appropriately


## Context
You are analyzing an AI code assistant system for the KurrentDB .NET Client project - a production-ready, high-performance .NET SDK for interacting with KurrentDB, an event-native database engineered for modern, event-driven architectures. This library is undergoing a significant architectural transition with dual APIs and advanced functional programming patterns.

The system uses multiple instruction files (CLAUDE.md, CLAUDE.PROJECT.md, core-prompt.md) and may include specialized guides in an ai-agent-system directory. The system potentially integrates with MCP (Model Context Protocol) servers for enhanced capabilities and uses TodoWrite for task management.

Key project characteristics that must inform your analysis:

**Dual API Architecture:**
- **Legacy API (KurrentDB.Client)**: Traditional exception-based patterns for backward compatibility
- **Modern API (Kurrent.Client)**: Fluent, functional design with Result<TValue, TError> pattern and type-safe error handling
- **Namespace Transition**: Active migration requiring consistent guidance across both namespaces

**Advanced Technical Features:**
- **Functional Error Handling**: Source-generated Variant types for discriminated error unions
- **Schema Registry Integration**: First-class support for schema registration, validation, and serialization (JSON/Protobuf)
- **gRPC Protocol Evolution**: V1 (legacy) and V2 (enhanced) protocol definitions with different capabilities
- **Cluster Discovery**: Automatic node discovery via gossip protocol with configurable preferences
- **Performance Optimization**: Memory-efficient, high-performance code using modern .NET features

**Development Standards:**
- **Testing Framework Migration**: TUnit (preferred) vs xUnit (legacy) with specific naming conventions
- **Advanced C# Usage**: C# 14 with experimental features, modern patterns, and performance optimizations
- **Architectural Patterns**: Lock/Unlock Immutability Pattern, comprehensive Result Pattern implementation
- **Documentation Standards**: Functional programming examples with realistic business scenarios

**Technical Dependencies:**
- **.NET 8.0/9.0**: Modern runtime with latest language features
- **gRPC & Protocol Buffers**: High-performance communication with dual protocol versions
- **OpenTelemetry**: Built-in observability and tracing support
- **Complex Testing Stack**: Docker containers, TLS certificates, cross-platform compatibility

The analysis must be project-agnostic yet contextually relevant, meaning you should discover the specific project characteristics first, then tailor your recommendations accordingly. You will need to examine chat history to understand actual performance issues, review current instructions for gaps and inconsistencies, and propose specific improvements that enhance the assistant's ability to handle the sophisticated dual-API architecture, functional programming patterns, and complex event-sourcing scenarios encountered in this high-performance database client project.

Your life depends on you thoroughly examining the chat history to identify real performance issues and ensuring any suggested changes align with both the legacy exception-based patterns and modern functional Result-based patterns while supporting the ongoing architectural transition of this sophisticated event-sourcing database client.
