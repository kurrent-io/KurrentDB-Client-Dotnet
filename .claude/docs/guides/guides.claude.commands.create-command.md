# Claude Code Command Creator - User Guide

Create powerful, environment-aware Claude Code commands with intelligent tool detection and ultrathink-powered analysis. The Command Creator helps you build custom commands that adapt to your specific Claude Code setup and leverage all available tools.

## 🚀 Quick Start

1. **Run the command creator** in your Claude Code session:
   ```
   > /project:create-command
   ```

2. **Answer 3 simple questions** about what you want to build

3. **Watch the magic happen** as it detects your tools, uses ultrathink mode to design the optimal command, and creates a production-ready result

4. **Use your new command** immediately with your team

## 🧠 What Makes This Special

**🔧 Environment Intelligence**
- Automatically detects available Claude Code tools (Bash, Edit, Web tools, etc.)
- Scans for configured MCP servers and integrates them seamlessly
- Adapts tool suggestions based on your permissions and setup

**🧠 Ultrathink-Powered Design**
- Uses Claude's extended thinking to deeply analyze your needs
- Considers optimal command patterns and tool combinations
- Designs sophisticated argument structures and error handling

**⚡ Smart & Simple**
- Default interactive mode for guided creation
- `--simple` flag for quick command generation
- `--dry-run` to preview everything before creating

## 📋 Command Options

```bash
# Interactive guided creation (recommended)
> /project:create-command

# Quick creation with minimal questions
> /project:create-command --simple

# Preview the creation process
> /project:create-command --dry-run

# Create specific command types
> /project:create-command --type analyze
> /project:create-command --type docs --output .claude/commands/documentation/

# See all options
> /project:create-command --help
```

## 🛠️ Intelligent Tool Detection

The Command Creator automatically detects and leverages:

**Core Claude Code Tools (Always Available):**
- `Read`, `Grep`, `LS`, `Glob` - For analysis and file operations
- `Agent` - For complex multi-step workflows
- `TodoRead` - For task management integration

**Permission-Based Tools (Detected & Suggested):**
- `Bash` - System integration and dynamic analysis
- `Edit`, `MultiEdit`, `Write` - File modification capabilities
- `WebSearch`, `WebFetch` - Research and external data
- `NotebookEdit` - Jupyter notebook operations

**MCP Tools (Auto-Configured):**
- Scans `.mcp.json` for configured servers
- Suggests MCP tools when relevant to your task
- Creates fallback behavior for environments without MCP

## 💡 Example Workflows

### Creating a Code Analysis Command

```
You: /project:create-command

Creator: What task do you want this command to help with?

You: Analyze technical debt in my TypeScript codebase

Creator: [Uses ultrathink to analyze your setup]
I see you have Bash, Edit, and Grep available. Perfect for code analysis!

Creator: What specific output should this command produce?

You: A report with prioritized refactoring recommendations

Creator: [Continues guided process...]

Result: Creates `/project:analyze:tech-debt` with:
- Dynamic tool detection
- TypeScript-specific analysis
- Prioritized recommendations
- Built-in --dry-run mode
```

### Quick Documentation Generator

```
You: /project:create-command --simple --type docs

Creator: What should this command do?

You: Generate API documentation from my code comments

Creator: What should it be called?

You: api-docs

Result: Creates `/project:docs:api-docs` in seconds with:
- Intelligent tool selection
- Error handling for missing files
- Team-friendly output format
```

## 🎯 Smart Command Categories

The Creator suggests optimal patterns for different command types:

**📊 Analysis Commands** (`--type analyze`)
- Uses `Grep`, `Bash`, `Read` for code examination
- Suggests arguments like `--scope`, `--format`, `--depth`
- Creates actionable reports with recommendations

**📚 Documentation Commands** (`--type docs`)
- Leverages `Read`, `Write`, `WebFetch` for content generation
- Suggests arguments like `--update`, `--format`, `--include-examples`
- Handles both creation and maintenance workflows

**🔍 Review Commands** (`--type review`)
- Combines `Read`, `Bash`, `Agent` for comprehensive analysis
- Suggests arguments like `--checklist`, `--severity`, `--auto-fix`
- Integrates with existing development workflows

**⚡ Optimization Commands** (`--type optimize`)
- Uses `Edit`, `MultiEdit`, `Bash` for improvements
- Suggests arguments like `--backup`, `--test`, `--incremental`
- Includes safety checks and rollback capabilities

## 🌟 Advanced Features

### Environment Adaptation
Commands automatically adapt to your setup:
- **With MCP**: Leverages external data sources and tools
- **Without MCP**: Uses core Claude Code tools effectively
- **Limited Permissions**: Graceful degradation with clear messaging

### Ultrathink Integration
The Creator uses extended thinking to:
- Analyze your specific use case deeply
- Consider multiple command design approaches
- Select optimal tool combinations
- Design sophisticated argument structures
- Plan error handling and edge cases

### Team Collaboration
Generated commands are designed for team use:
- Clear documentation and help text
- Consistent argument patterns
- Environment-independent operation
- Shared via `.claude/commands/` directory

## 📁 Command Organization

Commands are automatically organized by category:

```
.claude/commands/
├── analyze/
│   ├── tech-debt.md        # /project:analyze:tech-debt
│   ├── performance.md      # /project:analyze:performance
│   └── security.md         # /project:analyze:security
├── docs/
│   ├── api.md              # /project:docs:api
│   └── onboarding.md       # /project:docs:onboarding
├── review/
│   ├── pull-request.md     # /project:review:pull-request
│   └── code-quality.md     # /project:review:code-quality
└── optimize/
    ├── dependencies.md     # /project:optimize:dependencies
    └── build-process.md    # /project:optimize:build-process
```

## ✅ Quality Guarantees

Every generated command includes:

**🛡️ Safety Features**
- Mandatory `--dry-run` mode for preview
- Environment validation before execution
- Clear error messages for missing dependencies
- Graceful handling of permission issues

**🎯 Production Ready**
- Proper frontmatter with accurate tool lists
- Dynamic context loading using `@` and `!` syntax
- Argument validation and help documentation
- Consistent patterns following Claude Code conventions

**🔄 Maintainable**
- Clear, readable command structure
- Environment-independent design
- Easy to modify and extend
- Self-documenting with examples

## 🚀 Getting Started Today

1. **Try the interactive mode**:
   ```
   > /project:create-command
   ```

2. **Start with something simple** like a documentation generator or code formatter

3. **Use `--dry-run`** to see what would be created before committing

4. **Share successful commands** with your team by committing `.claude/commands/`

5. **Iterate and improve** your commands based on real usage

## 💡 Pro Tips

- **Start Simple**: Use `--simple` for your first few commands, then try full interactive mode
- **Think Workflows**: Consider entire development workflows, not just individual tasks
- **Leverage MCP**: If you have MCP servers configured, mention them in your requirements
- **Test Thoroughly**: Use `--dry-run` and test commands in different environments
- **Document Purpose**: Clear command descriptions help team adoption

## 🤝 Team Implementation

**For Team Leads:**
- Use Command Creator to standardize team workflows
- Create commands for common code review patterns
- Build analysis commands that match your quality standards

**For Developers:**
- Generate personal productivity commands
- Create project-specific utilities
- Build commands that automate repetitive tasks

**For DevOps:**
- Create deployment and monitoring commands
- Build infrastructure analysis tools
- Generate compliance and security check commands

Transform your development process with intelligent, adaptive commands that grow with your team and tooling! 🎉
