---
allowed-tools: Context7(*), Web_Search(*), Microsoft_Docs_MCP(*), Filesystem(*), Memory_Bank(*)
description: Create comprehensive documentation guides using natural language requests with intelligent research and memory bank integration
version: 1.0.0
created: 2025-07-06
last-updated: 2025-07-06
---

# Generate Documentation Guide Command

**Usage**: `/project:create-docs-guide [topic description]`

## Context

**Arguments**: $ARGUMENTS

**Memory Bank Status**: !`ls -la .claude/memory-bank/ 2>/dev/null || echo "Memory bank not initialized"`
**Project Standards**: .claude/docs/standards/standards.documentation.md (if exists)
**Existing Guides**: !`find .claude/docs/guides -name "*.md" 2>/dev/null | head -10 || echo "No existing guides found"`
**Git Status**: !`git status --porcelain | head -5`

## Memory Bank Context Loading

**Project Context**: @.claude/memory-bank/project-context.md
**Patterns Learned**: @.claude/memory-bank/patterns-learned.md
**Recent Decisions**: @.claude/memory-bank/decision-log.md

## Your Task

Generate a comprehensive guide based on the request: "$ARGUMENTS"

**CRITICAL**: Use the loaded Memory Bank context to ensure the guide integrates perfectly with our project patterns, technical decisions, and team preferences.

## Processing Workflow

### Phase 1: Request Intelligence & Analysis
```
1. PARSE NATURAL LANGUAGE:
   - Extract key technologies and components mentioned
   - Identify guide type intent using full language understanding
   - Recognize @ file references for context analysis
   - Determine scope and complexity level needed

2. MEMORY BANK INTEGRATION:
   - Load project context and domain knowledge
   - Review team patterns and learned conventions
   - Check recent technical decisions for relevance
   - Understand current technical stack and architecture

3. CURRENT STATE ANALYSIS:
   - Review existing guides to avoid duplication
   - Check git status for recent relevant changes
   - Analyze project standards for compliance requirements
   - Assess current session conversation for additional context

4. RESEARCH PLANNING:
   - Determine which MCP tools are needed for research
   - Plan research depth based on request complexity
   - Identify key information gaps to address
   - Prioritize research sources based on relevance
```

### Phase 2: Comprehensive Research
```
5. EXECUTE COMPREHENSIVE RESEARCH:
   
   **Primary Research (always execute):**
   - **Memory Bank Context**: Project patterns, technical decisions, team preferences for alignment
   - **Context7**: Official documentation, API references, and examples for the main technology
   
   **Conditional Research (based on request characteristics):**
   
   **For .NET, Azure, or Microsoft technologies:**
   - Use Microsoft_Docs_MCP to get official guidance, best practices, and integration patterns
   
   **For current community practices, troubleshooting, or real-world examples:**
   - Use Web_Search to find recent solutions, community insights, and practical implementations
   
   **For project-specific integration patterns:**
   - Use Filesystem to analyze any @ referenced files for existing code patterns and conventions
   
   **Research Quality Validation:**
   - Verify information currency (prefer latest versions and practices)
   - Cross-reference findings with Memory Bank patterns and technical decisions
   - Prioritize official documentation while validating against project standards
   - Ensure examples align with project's technical stack and team conventions
   - Filter research through Memory Bank context for project relevance
   
   **Always include:** Memory Bank context for project-specific alignment throughout all research phases

6. SYNTHESIZE FINDINGS:
   - Consolidate research from all sources
   - Identify most relevant and current information
   - Select best examples and patterns for the guide
   - Validate information accuracy and project fit
   - Cross-reference with Memory Bank patterns and decisions
```

### Phase 3: Guide Generation
```
7. DETERMINE CATEGORY & NAMING:
   - Analyze request intent using full language understanding
   - Generate smart filename based on content and scope
   - Confirm category choice fits the actual need
   
   Conceptual Category Classification:
   - quick-start: User expresses need for rapid setup, getting operational quickly
   - comprehensive: User indicates desire for complete understanding, thorough coverage  
   - reference: User needs lookup/consultation material, API information, daily use docs
   - integration: User wants to combine/connect technologies or systems
   - troubleshooting: User has problems to solve, needs diagnostic guidance
   
   Use full Claude intelligence to understand intent, not keyword matching.

8. GENERATE CONTENT:
   - Select appropriate template based on category
   - Fill template with researched content
   - Integrate Memory Bank patterns and project standards
   - Include working examples following project conventions
   - Add project integration guidance
   - Reference relevant project standards and decisions

9. SAVE & MAKE AVAILABLE:
   - Save guide to .claude/docs/guides/{smart_filename}
   - Announce guide creation and location
   - Make content immediately available for current response
```

## Template Structure

### Guide Template (Applied to All Categories)
```markdown
# {Topic} Guide for Project

**Generated**: {Date} **Source**: {Research tools used}
**Category**: {Determined category}
**Memory Bank Integration**: ✅ Project patterns and decisions applied

## Overview
{Brief description and project context from Memory Bank}

## Project Integration  
**Related Standards**: @.claude/docs/standards/standards.{relevant}.md
**Architecture Alignment**: {How this fits project-context.md}
**Team Patterns**: {Reference patterns-learned.md}
**Decision Context**: {Reference relevant decision-log.md entries}

## Setup
{Complete setup with project defaults and team patterns}

## Examples
{Working code following project conventions from Memory Bank}

## Troubleshooting
{Common issues and project-specific solutions}

## References
{Official docs + project standards + Memory Bank context}
```

## Quality Standards

### Research Quality
- **Comprehensive**: Cover all aspects relevant to the request
- **Current**: Use latest versions and best practices
- **Project-Relevant**: Focus on what's applicable to the project using Memory Bank
- **Accurate**: Validate information from multiple sources

### Content Quality  
- **Clear Intent Match**: Guide serves the exact requested purpose
- **Memory Bank Integration**: Leverages all project context, patterns, and decisions
- **Practical Examples**: Working code that fits project patterns from Memory Bank
- **Project Integration**: Shows how to integrate with existing codebase and architecture
- **Complete Coverage**: Addresses the full scope of the request

### Naming Quality
- **Discoverable**: Filename clearly indicates content and purpose
- **Consistent**: Follows established project naming patterns
- **Logical**: Name makes sense based on actual content generated
- **Specific**: Appropriately scoped (not too broad or too narrow)

## Success Criteria
✅ **Request correctly analyzed** using Memory Bank context for project alignment
✅ **Comprehensive research** completed using relevant MCP tools  
✅ **Category and filename** intelligently determined based on actual content
✅ **Guide content** matches request intent and integrates with Memory Bank patterns
✅ **Project integration** included with relevant standards and team decisions
✅ **Working examples** that follow established project conventions from Memory Bank
✅ **Guide saved** and made available for immediate use

**Remember**: This guide should feel like it was written by someone who deeply understands our project, team patterns, and technical decisions thanks to the Memory Bank context.
