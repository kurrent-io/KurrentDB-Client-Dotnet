---
allowed-tools:
  - Bash(git:*)
  - Bash(wc:*)
  - Bash(head:*)
  - Bash(tail:*)
  - Bash(grep:*)
  - Bash(ls:*)
  - Bash(file:*)
  - Bash(du:*)
  - Glob
  - Grep
  - LS
  - Read
  - Write
  - Context7:*
  - mcp__microsoft_*
  - mcp__FileScopeMCP_*
description: Intelligent repository context builder that adapts analysis depth to repository complexity and supports incremental updates
version: 2.0.0
created: 2025-01-07
---

## Arguments
$ARGUMENTS

## Context Analysis Parameters

**Parse Arguments**: !`if echo "$ARGUMENTS" | grep -q -- "--help"; then echo "HELP_REQUESTED"; else echo "ANALYSIS_MODE"; fi`

**Repository Metrics**:
- Location: !`pwd`
- Git Status: !`git rev-parse --git-dir >/dev/null 2>&1 && echo "Git repository detected" || echo "Not a git repository"`
- Since Period: !`if echo "$ARGUMENTS" | grep -q -- "--since"; then echo "$ARGUMENTS" | sed 's/.*--since[[:space:]]*\([^[:space:]]*\).*/\1/'; else echo "1 month ago"; fi`
- Output Path: !`if echo "$ARGUMENTS" | grep -q -- "--output"; then echo "$ARGUMENTS" | sed 's/.*--output[[:space:]]*\([^[:space:]]*\).*/\1/'; else echo ".claude/context/repo-context.md"; fi`

## Quick Repository Assessment

**Initial Scan**: !`echo "=== REPOSITORY COMPLEXITY ASSESSMENT ===" && \
if git rev-parse --git-dir >/dev/null 2>&1; then \
  echo "Using git-optimized analysis..." && \
  SOURCE_COUNT=$(git ls-files '*.js' '*.ts' '*.py' '*.java' '*.go' '*.rs' '*.cpp' '*.c' '*.php' '*.rb' '*.cs' '*.kt' '*.swift' '*.scala' | wc -l) && \
  TOTAL_FILES=$(git ls-files | wc -l) && \
  echo "Git-tracked source files: $SOURCE_COUNT" && \
  echo "Total git-tracked files: $TOTAL_FILES"; \
else \
  echo "Non-git repository, using filesystem scan..." && \
  SOURCE_COUNT=$(find . -name "*.js" -o -name "*.ts" -o -name "*.py" -o -name "*.java" -o -name "*.go" -o -name "*.rs" -o -name "*.cpp" -o -name "*.c" -o -name "*.php" -o -name "*.rb" -o -name "*.cs" -o -name "*.kt" -o -name "*.swift" -o -name "*.scala" | wc -l) && \
  TOTAL_FILES=$(find . -type f | wc -l) && \
  echo "Source files: $SOURCE_COUNT" && \
  echo "Total files: $TOTAL_FILES"; \
fi && \
REPO_SIZE=$(du -sh . 2>/dev/null | cut -f1) && \
GIT_COMMITS=$(git rev-list --count HEAD 2>/dev/null || echo "0") && \
CONTRIBUTORS=$(git shortlog -sn --all 2>/dev/null | wc -l || echo "0") && \
echo "Repository size: $REPO_SIZE" && \
echo "Git commits: $GIT_COMMITS" && \
echo "Contributors: $CONTRIBUTORS" && \
if [ "$SOURCE_COUNT" -gt 500 ] || [ "$TOTAL_FILES" -gt 2000 ] || [ "$GIT_COMMITS" -gt 1000 ]; then \
  echo "COMPLEXITY: HIGH"; \
elif [ "$SOURCE_COUNT" -gt 100 ] || [ "$TOTAL_FILES" -gt 500 ] || [ "$GIT_COMMITS" -gt 200 ]; then \
  echo "COMPLEXITY: MEDIUM"; \
else \
  echo "COMPLEXITY: LOW"; \
fi`

## Task Specification

**Handle Help Request:**
```
Repository Context Builder - Intelligent codebase analysis for AI agents

USAGE:
  /project:repo-context [OPTIONS]

OPTIONS:
  --since TIME_PERIOD         Analyze git history since specified time
                              (default: "1 month ago")
  --output PATH               Output file path for context document
                              (default: ".claude/context/repo-context.md")
  --update                    Incremental update mode (faster)
  --deep-analysis             Enable extended thinking for complex architectural insights
  --help                      Show this help

TIME PERIOD FORMATS:
  "3 months ago", "6 months ago", "1 year ago", "2024-01-01", "last week"

FEATURES:
  ✓ Adaptive analysis depth based on repository complexity
  ✓ Incremental updates for faster subsequent runs  
  ✓ Context7 integration for technology documentation
  ✓ Microsoft MCP for .NET/Azure/TypeScript insights
  ✓ FileScopeMCP for dependency analysis and file importance ranking
  ✓ Agent-optimized output format with extended thinking
  ✓ Business domain extraction and architecture analysis
  ✓ Technical debt and quality assessment

OUTPUT:
  Creates a comprehensive context document optimized for AI agent decision-making.
  Includes executive summary, architecture patterns, domain insights, and actionable intelligence.
```

**Exit if help requested:**
!`if [ "$ARGUMENTS" = "--help" ]; then exit 0; fi`

## Intelligent Repository Context Building

You are building a comprehensive repository context document optimized for AI agent decision-making. This is **not** a human-readable analysis - it's a structured knowledge base that agents will reference to make informed decisions about code changes, architecture, and project strategy.

**IMPORTANT: Always prefer git-based file operations over filesystem operations for performance:**
- Use `git ls-files` instead of `find` for file discovery
- Use `git ls-files '*.ext'` for language-specific file counting
- Only fallback to `find` for non-git repositories
- This provides 10-100x performance improvement on large repositories

**Think deeply and thoroughly about this codebase analysis.** Use extended thinking to reason through complex architectural relationships, identify subtle domain patterns, and provide comprehensive insights that go beyond surface-level analysis. Consider multiple perspectives and analyze the deeper implications of architectural decisions.

### Phase 1: Smart Repository Profiling & MCP Enhancement

**Repository Classification**: Analyze the discovered complexity level and determine:
1. **Project Type**: (Library/Package, Backend Service, Frontend App, Fullstack, Tool/CLI, Data/ML, Infrastructure, Mobile, Desktop, Game, Other)
2. **Architecture Pattern**: Identify from initial file scanning
3. **Analysis Depth**: Based on complexity assessment
4. **MCP Strategy**: Use Context7 for technology documentation, Microsoft MCP for .NET/Azure/TypeScript, FileScopeMCP for dependency analysis

**Technology Documentation Enhancement** (Always use Context7):
- For each detected technology/framework, use Context7 to get current documentation
- Enhance analysis with official best practices and patterns
- Include relevant code examples and architectural guidance

### Phase 2: Advanced Code Analysis with MCP Tools

**FileScopeMCP Integration** (if available):
- Generate file importance rankings (0-10 scale)
- Map dependency relationships
- Identify critical files that require careful handling
- Create dependency diagrams for complex systems

**Business Domain Intelligence** (Priority #1):
- Extract domain concepts from code structure, naming patterns, and documentation
- Identify business entities, workflows, and terminology
- Map business logic locations and patterns
- Analyze API endpoints, database schemas, and data models for domain insights

**Architecture & Design Patterns** (Priority #2):
- Identify architectural patterns (MVC, microservices, layered, event-driven, etc.)
- Map component boundaries and dependencies
- Analyze design patterns in use
- Document architectural decisions and trade-offs

### Phase 3: Deep Technology Stack Analysis

**Microsoft Technology Integration** (if applicable):
- Use Microsoft MCP for .NET, Azure, TypeScript documentation and best practices
- Analyze compliance with Microsoft architectural patterns
- Identify modernization opportunities for Microsoft stack components

**Technology Stack & Dependencies** (Priority #3):
- Parse package managers and dependency files
- Identify frameworks, libraries, and tools with Context7 documentation enhancement
- Assess dependency risks and update needs
- Map technology choices to architectural decisions

### Phase 3: Quality & Risk Assessment

**Code Quality Indicators** (Priority #3):
- Identify testing strategies and coverage patterns
- Analyze code organization and modularity
- Assess documentation quality
- Evaluate coding standards adherence

**Technical Debt Hotspots** (Priority #5):
- Use git history analysis to identify change frequency patterns
- Find files with high churn rates
- Identify complexity hotspots
- Assess maintenance burden areas

### Phase 4: Agent Decision Support

Create structured output sections specifically designed to help agents make better decisions:

1. **Quick Reference** - Essential facts for immediate orientation
2. **Decision Context** - Architecture constraints and patterns to follow
3. **Domain Model** - Business concepts and their relationships
4. **Change Guidelines** - How to make changes safely in this codebase
5. **Risk Factors** - What to be careful about
6. **Incremental Metadata** - For future updates

### Output Format Requirements

```markdown
# 🧠 Repository Context for AI Agents
*Generated: [timestamp] | Complexity: [level] | Update Mode: [full/incremental]*

## 🎯 Executive Summary for Agents
[3-4 sentences providing immediate context: what this codebase does, its architectural approach, primary technology stack, and current development phase. Focus on information agents need to make good decisions.]

**Quick Facts:**
- **Type**: [Project type] | **Complexity**: [High/Medium/Low] | **Scale**: [metrics]
- **Primary Stack**: [languages + frameworks]
- **Architecture**: [main pattern]
- **Domain**: [business domain/purpose]
- **Risk Level**: [High/Medium/Low] with [key risk factors]

## 🏗 Architecture & Decision Context

### Architectural Pattern
[Pattern name and implementation approach - how components are organized and communicate]

### Technology Decisions
[Key technology choices and the reasoning/constraints behind them]

### Component Boundaries
[How the system is modularized and where boundaries exist]

### Integration Points
[External systems, APIs, databases, and how they connect]

## 🏢 Business Domain Model

### Core Entities
[Primary business objects and their relationships]

### Business Workflows
[Key business processes and how they're implemented in code]

### Domain Terminology
[Comprehensive glossary of business terms used throughout the codebase]

### API/Interface Design
[How the domain is exposed through APIs or interfaces]

## 🛠 Agent Guidelines for Changes

### Safe Change Patterns
[How to make changes safely in this codebase - testing approach, deployment pattern, etc.]

### Architecture Constraints
[What patterns to follow, what to avoid, and why]

### Quality Gates
[What quality checks exist and should be maintained]

### Integration Requirements
[How changes need to integrate with other systems]

## ⚠️ Risk Factors & Hotspots

### Technical Debt Areas
[Specific areas that need careful handling]

### High-Churn Files
[Files that change frequently and may be fragile]

### Dependency Risks
[External dependencies that could cause issues]

### Security Considerations
[Security patterns in use and sensitive areas]

## 📊 Development Intelligence

### File Importance & Dependencies
[FileScopeMCP rankings and dependency analysis if available]

### Git Activity Patterns
[Recent development patterns and team velocity]

### Testing Strategy
[How testing is approached and what coverage exists]

### Documentation Status
[Quality and completeness of existing documentation - enhanced with Context7 insights]

### Technology Best Practices
[Context7 and Microsoft MCP recommendations for detected technologies]

### Operational Context
[How the code is deployed and monitored]

## 🔄 Context Metadata
*For incremental updates and agent learning*

**Last Full Analysis**: [timestamp]
**Analysis Scope**: [files/patterns analyzed]
**MCP Tools Used**: [Context7, Microsoft MCP, FileScopeMCP - as available]
**Technology Documentation**: [Context7 libraries referenced]
**Key Metrics**: [LOC, complexity, file importance scores]
**Change Indicators**: [what to watch for updates]
```

### Execution Strategy

1. **Git-Optimized Analysis**: Always prefer `git ls-files` over `find` operations for 10-100x performance gain
2. **Adaptive Depth**: Scale analysis effort based on detected complexity
3. **Extended Thinking**: Use `--deep-analysis` flag to enable ultrathink mode for complex architectural insights
4. **Incremental Support**: If `--update` flag is present, focus on changes since last analysis
5. **MCP Integration**: Always use Context7 for technology documentation; use Microsoft MCP for relevant stacks
6. **Smart Tool Selection**: Leverage FileScopeMCP when available for dependency analysis and file importance ranking
7. **Output Optimization**: Create directory structure and save to specified path

**Success Criteria**: The generated context document should enable any AI agent to quickly understand the codebase's purpose, constraints, safe change patterns, and critical dependencies without needing to analyze the code from scratch.
