---
allowed-tools:
  - Agent
  - Bash(git:*)
  - Bash(find:*)
  - Glob
  - Grep
  - LS
  - Read
  - Write
  - WebSearch
  - Context7:*
  - mcp__microsoft_*

description: Intelligent repository analysis
version: 2.0.0
---

## Context
- Directory: !`pwd`
- Git info: !`git remote -v 2>/dev/null || echo "Not a git repository"`
- Quick size: !`if git rev-parse --git-dir >/dev/null 2>&1; then echo "$(git ls-files | wc -l) files tracked"; else echo "$(find . -type f | wc -l) files found"; fi`

## Your task

Deeply analyze this codebase and create a comprehensive understanding document.

**Performance tip:** Use `git ls-files` instead of `find` when possible - it's 10-100x faster.

### Phase 1: Understand
- What is this? What problem does it solve? Who uses it?
- Search the repo for existing documentation, ADRs, and design decisions
- If this is a business/product, search the web for their website and public information

### Phase 2: Analyze with the right tools
- **Context7**: Get current documentation for all libraries and frameworks you find
- **Microsoft MCP**: For .NET/Azure/TypeScript projects, use official Microsoft docs
- Understand the technology choices and architecture

### Phase 3: Extract insights
- Identify patterns, conventions, and team practices
- Find what makes this codebase unique or interesting
- Assess code quality and technical debt areas

**Output:** Save your findings to `.claude/docs/project-context.md` (or path specified with --output).

### Output Template

```markdown
# Project Context

## Overview
[What is this project? What problem does it solve? Who uses it?]

## Technology & Architecture
[Key technologies, architectural patterns, and design decisions]

## Domain & Business Logic
[Core concepts, workflows, and domain-specific knowledge]

## Development Patterns
[Conventions, practices, and things to know when working here]

## Key Insights
[What's interesting, unique, or important about this codebase?]
```

Adapt the analysis depth based on the repository's complexity. Think deeply about what you discover.
