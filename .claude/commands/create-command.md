---
allowed-tools: [Read, Write, Bash, artifacts]
description: Create custom Claude Code commands through guided interactive process using ultrathink mode
version: 1.0.0
created: 2025-07-06
last-updated: 2025-07-06
---

## Arguments
$ARGUMENTS

## Context
- Available Claude Code tools: !`echo "Detecting available tools and MCP servers..."`
- Existing commands: @.claude/commands/
- Current directory: !`pwd`
- Available command examples: !`find .claude/commands -name "*.md" 2>/dev/null | head -5`
- MCP configuration: !`if [ -f .mcp.json ]; then cat .mcp.json; else echo "No local MCP config found"; fi`
- Settings: !`if [ -f .claude/settings.json ]; then cat .claude/settings.json; else echo "No local settings found"; fi`

## Your task

Help users create custom Claude Code commands efficiently and safely using ultrathink mode for enhanced reasoning.

**Think deeply about this command creation task. Use extended thinking to analyze the user's needs, available tools, and optimal command design patterns.**

### Argument Processing

**Parse arguments first:**
```
--help: Show usage information and exit
--dry-run: Preview what would be created without creating it
--simple: Skip detailed questions, create basic command quickly
--output PATH: Specify output location (default: .claude/commands/)
--type TYPE: Command category (analyze|docs|review|optimize|other)
```

**Handle --help:**
```
Command Creator - Create custom Claude Code commands

USAGE:
  /project:create-command [OPTIONS]

OPTIONS:
  --help              Show this help and exit
  --dry-run           Preview what would be created
  --simple            Quick command creation (skip detailed questions)
  --output PATH       Output location (default: .claude/commands/)
  --type TYPE         Command type: analyze, docs, review, optimize, other

AVAILABLE CLAUDE CODE TOOLS:
  Core Tools (no permission needed):
  - Agent: Runs sub-agents for complex tasks
  - Glob: Finds files by pattern
  - Grep: Searches file contents
  - LS: Lists directories
  - Read: Reads file contents
  - TodoRead: Reads task lists
  - NotebookRead: Reads Jupyter notebooks

  Tools requiring permission:
  - Bash: Execute shell commands
  - Edit: Make targeted file edits
  - MultiEdit: Multiple atomic edits
  - Write: Create/overwrite files
  - WebFetch: Fetch web content
  - WebSearch: Search the web
  - NotebookEdit: Edit Jupyter notebooks
  - TodoWrite: Manage task lists

EXAMPLES:
  /project:create-command
    → Interactive guided creation with tool detection
  
  /project:create-command --simple
    → Quick command creation with minimal questions
  
  /project:create-command --type analyze --output .claude/commands/analyze/
    → Create analysis command in specific location
  
  /project:create-command --dry-run
    → Preview the creation process and available tools
```

**Handle --dry-run:**
```
DRY RUN MODE - Preview of command creation process:

1. TOOL DETECTION PHASE:
   - Scan available Claude Code core tools
   - Check for configured MCP servers in .mcp.json
   - Identify available permissions in .claude/settings.json
   - Report which tools would be suggested for different command types

2. COMMAND CREATION PROCESS:
   I would ask you 3 key questions:
   - What task should this command help with?
   - What specific output/analysis do you need?
   - Any existing tools/processes to enhance?

3. ULTRATHINK ANALYSIS:
   Using extended thinking, I would:
   - Analyze optimal command structure for your task
   - Select appropriate tools from available options
   - Design arguments based on command type and complexity
   - Consider error handling and edge cases

4. GENERATE ADAPTIVE COMMAND:
   Create a command file with:
   - Dynamic tool list based on what's actually available
   - Proper frontmatter with detected tools
   - Context section referencing relevant files
   - Clear task instructions with error handling

5. SAVE LOCATION: [detected optimal path based on command type]

No files would be created in dry-run mode.
```

### Command Creation Process

**First, analyze the environment using ultrathink mode:**

Think deeply about the current setup:
- What Claude Code tools are available by default
- What MCP servers are configured 
- What permissions are already granted
- What type of command would be most useful given the context

**If --simple flag provided:**
Ask only essential questions:
1. "What should this command do?"
2. "What should it be called?"
Create basic command with detected tools and save it.

**Default Interactive Process with Tool-Aware Creation:**

#### Step 1: Environment Analysis (Ultrathink)
Before asking user questions, think deeply about:
- Available tools from the context gathered above
- Optimal command patterns for this environment
- What types of commands would be most useful
- How to suggest tools intelligently based on task type

#### Step 2: Understand the Goal
Ask: **"What task do you want this command to help with?"**

Provide examples tailored to available tools:
- If Bash + Git available: "Analyze git history and commit patterns"
- If WebSearch available: "Research best practices and generate reports"
- If Edit/Write available: "Refactor code and update documentation"
- If Agent available: "Orchestrate complex multi-step workflows"
- If MCP tools detected: "Leverage [specific MCP tools] for enhanced analysis"

#### Step 3: Define the Output
Ask: **"What specific output or result should this command produce?"**

Examples based on detected capabilities:
- "A markdown report with actionable recommendations"
- "Modified files with improvements applied"
- "A structured analysis using available MCP data"
- "Generated documentation and code examples"

#### Step 4: Tool-Aware Context Gathering
Ask: **"Are there specific files, directories, or data sources this command should work with?"**

Suggest context based on available tools:
- If Read/Grep available: Source code analysis (`@src/`, `@lib/`)
- If WebFetch available: External documentation and APIs
- If Bash available: Dynamic system information (`!git status`, `!ls -la`)
- If MCP tools available: External system integration

#### Step 5: Intelligent Tool Selection (Ultrathink)
Think deeply about optimal tool combinations:
- Match user's task to available Claude Code tools
- Consider permission requirements and suggest appropriate tools
- Design tool list that's minimal but sufficient
- Account for MCP tools if they enhance the command's capabilities

#### Step 6: Adaptive Argument Design
Based on task complexity and available tools, suggest:
- Always include `--dry-run` for safety
- Task-specific arguments that leverage available tools
- Permission-aware options (e.g., only suggest --modify if Edit/Write available)
- MCP-enhanced options if relevant tools are configured

#### Step 7: Generate Adaptive Command
Create command file with:
- Dynamic tool list based on actual availability
- Context that uses available tools optimally
- Instructions that adapt to the tool ecosystem
- Error handling for missing optional tools

### Dynamic Command Template Structure

When creating the command file, use this adaptive template:

```markdown
---
allowed-tools: [Dynamically selected based on availability and task needs]
description: [Brief description of what the command does]
version: 1.0.0
created: 2025-07-06
last-updated: 2025-07-06
---

## Arguments
$ARGUMENTS

## Context
[Include tool-appropriate context:]
[File references using @filename if Read tool available]
[Dynamic commands using !command if Bash tool available]
[MCP data sources if relevant MCP tools configured]

## Your task
[Clear, step-by-step instructions adapted to available tools]

Handle arguments:
- If $ARGUMENTS contains "--dry-run", explain what would be done without doing it
- [Include other argument handling based on suggested arguments]

Tool availability handling:
[Only include tool-specific instructions if tools are available]
[Graceful degradation if optional tools are missing]
[Clear error messages if required tools are unavailable]

Then proceed with the main task using available tools optimally.
```

### Tool Selection Intelligence

**Core Claude Code Tools (always available):**
- `Read`, `Grep`, `LS`, `Glob` for analysis tasks
- `Agent` for complex multi-step workflows
- `TodoRead` for task management integration

**Permission-Based Tools (suggest based on task):**
- `Write`, `Edit`, `MultiEdit` for file modification tasks
- `Bash` for system integration and dynamic analysis
- `WebSearch`, `WebFetch` for research and external data
- `NotebookEdit` for Jupyter notebook tasks

**MCP Tools (suggest if configured):**
- Include specific MCP tools found in environment
- Design fallback behavior if MCP tools unavailable
- Leverage MCP capabilities for enhanced functionality

### Error Handling & Validation

**Tool Availability Checks:**
- Verify required tools are available before suggesting them
- Provide clear errors if essential tools are missing
- Graceful degradation for optional tool dependencies

**Environment Validation:**
- Check MCP configuration is valid if used
- Verify permissions for tools that require them
- Ensure output directory is writable

**Command Quality Assurance:**
- Ensure commands have clear, specific purposes
- Validate that suggested tools match the task requirements
- Check that argument design is consistent with tool capabilities

### Success Criteria

A well-created command should:
- **Adapt to environment**: Use only available tools intelligently
- **Handle missing tools gracefully**: Provide fallbacks or clear error messages
- **Follow permissions model**: Respect Claude Code's permission system
- **Leverage MCP when appropriate**: Make use of configured external tools
- **Include proper error handling**: Handle edge cases and tool failures
- **Be team-friendly**: Work consistently across different environments

**Final Step**: Create the command file, save it to the appropriate location, and test that it handles tool availability correctly.
