---
allowed-tools: [Read, Write, LS, Glob, Grep, Bash]
description: Initialize agentic memory bank system with comprehensive project analysis
version: 1.0.0
created: 2025-07-06
last-updated: 2025-07-06
---

## Arguments
$ARGUMENTS

## Context
Template files: @.claude/memory-bank/templates/project-context.md @.claude/memory-bank/templates/active-context.md @.claude/memory-bank/templates/progress-tracker.md @.claude/memory-bank/templates/decision-log.md @.claude/memory-bank/templates/patterns-learned.md

Git context: !git status --porcelain 2>/dev/null || echo "No git repository"
Current branch: !git branch --show-current 2>/dev/null || echo "No git repository"
Development history: !git log --oneline -10 2>/dev/null || echo "No git repository"

## Your task

Establish the agentic memory bank system through comprehensive project analysis and template population.

Handle arguments:
- If $ARGUMENTS contains "--dry-run", show analysis results and planned file structure without creating files

### Step 1: Environment Validation

Check `.claude/memory-bank/templates/` contains all 6 template files.
If missing, fail: "❌ Templates missing. Reinstall memory bank system."

### Step 2: Project Analysis

Execute comprehensive project analysis:

> ultrathink about this project's architecture, technology stack, development patterns, current state, and team conventions. Generate complete understanding for populating memory bank templates with accurate, specific project information.

**Analysis scope:**
- Project identity, domain, scale, complexity
- Technology ecosystem and architecture patterns  
- Development workflows and team conventions
- Current development state and trajectory
- Codebase patterns and organizational structure

**Research enhancement:**
- Use Context7 to gather documentation for detected technologies, frameworks, or tools
- Fallback to WebSearch for additional context on unfamiliar dependencies
- Analyze git history for development patterns and velocity
- Search codebase for configuration and convention patterns

### Step 3: Memory Bank Creation

Read each template and create populated versions:

#### .claude/memory-bank/project-context.md
- Project identity and strategic context
- Domain classification and user ecosystem
- Success metrics and scope boundaries
- Technology ecosystem and architecture decisions
- Performance characteristics and scalability approach
- Development tooling and deployment strategies

#### .claude/memory-bank/active-context.md  
- Current development state and git context
- Session priorities and immediate objectives
- Blocking factors and active work streams

#### .claude/memory-bank/progress-tracker.md
- Development milestones and completion metrics
- Velocity patterns and forecasting data
- Resource allocation and capacity insights

#### .claude/memory-bank/decision-log.md
- Architectural decisions and rationale
- Technology choices and trade-offs
- Memory bank adoption decision (ADR-001)

#### .claude/memory-bank/patterns-learned.md
- Development patterns and conventions
- Team preferences and workflow optimization
- Continuous learning baseline

### Step 4: System Activation

Report successful establishment:

```
🧠 Memory Bank System Activated

✅ Files Created:
- .claude/memory-bank/project-context.md → Project identity, strategic context and technology and architecture
- .claude/memory-bank/active-context.md → Current development state
- .claude/memory-bank/progress-tracker.md → Development velocity and milestones
- .claude/memory-bank/decision-log.md → Architectural decisions and evolution
- .claude/memory-bank/patterns-learned.md → Adaptive learning and team patterns

📊 Analysis Summary:
- Domain: [detected] | Scale: [assessed] | Phase: [current]
- Technology: [primary-stack] | Architecture: [pattern]
- Team: [workflow] | Velocity: [frequency]

🎯 Agentic Workflow Ready:
- Session start: /project:memory-bank-sync
- Session end: /project:memory-bank-update
- Continuous learning: Automatic pattern adaptation

Persistent memory and adaptive learning established.
```
