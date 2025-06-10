# AI Agent Prompt System

A comprehensive, tiered prompt system for enhancing AI agent performance across multiple development tools and scenarios.

## ? Overview

This system provides a structured approach to AI agent prompting with three tiers of resources:

- **Tier 1**: Core prompt with essential coding standards and workflow protocols
- **Tier 2**: Project-specific instructions generated from repository analysis
- **Tier 3**: Specialized deep-dive guides for specific development tasks

## ? File Structure

```
ai-agent-system/
??? init-ai-agent.sh           # ? Automated setup script
??? core-prompt.md              # Tier 1: Always load this
??? project-template.md         # Tier 2: Project template
??? project-analyzer.md         # Tier 2: Repository analysis prompt
??? guides/                     # Tier 3: Specialized references
    ??? performance-guide.md    # Advanced optimization techniques
    ??? testing-guide.md        # TUnit/Shouldly/FakeItEasy patterns
    ??? documentation-guide.md  # XML documentation standards
```

## ? Quick Start

### ? **Automated Setup (Recommended)**

1. **Clone/download** this repository to your development machine
2. **Navigate to your project directory**
3. **Run the init script**:
   ```bash
   # Basic setup - creates tool-specific configuration files
   /path/to/ai-agent-system/init-ai-agent.sh claude     # Claude Code
   /path/to/ai-agent-system/init-ai-agent.sh cline      # Cline
   /path/to/ai-agent-system/init-ai-agent.sh cursor     # Cursor
   /path/to/ai-agent-system/init-ai-agent.sh copilot    # GitHub Copilot
   
   # With project context (if project-instructions-*.md exists)
   /path/to/ai-agent-system/init-ai-agent.sh claude --with-project
   
   # With specialized guides
   /path/to/ai-agent-system/init-ai-agent.sh cline --guides performance,testing
   
   # Full setup
   /path/to/ai-agent-system/init-ai-agent.sh claude --with-project --guides performance --force
   ```
4. **Open your project** in your chosen tool - it will automatically load the configuration!

**Tip**: Add the ai-agent-system directory to your PATH for convenience:
```bash
export PATH="$PATH:/path/to/ai-agent-system"
init-ai-agent.sh claude --with-project
```

### ? **Manual Setup (Alternative)**

For manual configuration or tools not yet supported by the init script:

#### For Daily Development
Load `core-prompt.md` into your AI tool (Claude Code, Cursor, Cline, GitHub Copilot).

#### For New Projects
1. Load `project-analyzer.md` and `project-template.md`
2. Run the analyzer on your target repository
3. Save the result as `project-instructions-[PROJECT_NAME].md`
4. Use: `core-prompt.md` + `project-instructions-[PROJECT_NAME].md`

#### For Specialized Tasks
Add the appropriate guide from the `guides/` directory:

- **Performance optimization**: Add `guides/performance-guide.md`
- **Testing setup**: Add `guides/testing-guide.md`
- **API documentation**: Add `guides/documentation-guide.md`

## ? Activation Prompts

After loading the prompt files, use one of these activation prompts to trigger the agent:

### **Simple Activation**
```
REMEMBER
```

### **Standard Activation**
```
Hello! Please review your instructions and confirm you're ready to assist with development.
```

### **Detailed Activation**
```
Load complete. Please acknowledge your role, confirm your available resources, and let me know how you can help with this project.
```

### **Context Validation Prompt**
```
REMEMBER and confirm:
1. What project am I working on?
2. What specialized guides are loaded?
3. What are the key architectural patterns for this project?
```

### **What the Agent Should Respond With**

A properly activated agent should:
- ? Acknowledge its role as a senior software engineer
- ? Confirm it has loaded the core coding standards
- ? List available specialized resources (if any were loaded)
- ? Ask what you'd like to work on
- ? Mention project-specific context (if project instructions were loaded)

### **Example Agent Response**
```
I've reviewed my instructions and I'm ready to assist as your senior software engineer.

Loaded Resources:
? Core coding standards (C# 14, K&R braces, vertical alignment, etc.)
? Project instructions for [PROJECT_NAME]
? Performance optimization guide

I understand this project is [brief project description] and I'll follow the established 
architectural patterns and domain-specific principles.

What would you like to work on? I can help with:
- Code implementation and review
- Architecture decisions
- Performance optimization
- Testing strategies
- Documentation

What's the current task?
```

## ? Tool Compatibility

This system works with:
- Claude Code
- Cursor
- Cline
- GitHub Copilot
- Any AI tool that accepts markdown prompts

## ? Project Instructions Versioning

### Versioning Strategy
- Use semantic versioning: `project-instructions-[PROJECT]-v1.2.md`
- Update version when architecture changes significantly
- Keep changelog of major updates in project instructions file
- Archive old versions when creating new major versions

### Version Examples
```
project-instructions-kurrentdb-v1.0.md     # Initial version
project-instructions-kurrentdb-v1.1.md     # Minor updates
project-instructions-kurrentdb-v2.0.md     # Major architecture change
```

### When to Update Version
- **Major (v2.0)**: Architecture changes, new layers, different patterns
- **Minor (v1.1)**: New components, updated principles, refined guidance
- **Patch (v1.0.1)**: Typo fixes, clarifications, minor corrections

## ? Usage Examples

### ? **Automated Setup Examples**

```bash
# Navigate to your project directory
cd /path/to/your/project

# Basic tool setup
/path/to/ai-agent-system/init-ai-agent.sh claude
/path/to/ai-agent-system/init-ai-agent.sh cline

# With project context
/path/to/ai-agent-system/init-ai-agent.sh claude --with-project

# With specialized guides
/path/to/ai-agent-system/init-ai-agent.sh cline --guides performance,testing

# Force overwrite existing configurations
/path/to/ai-agent-system/init-ai-agent.sh claude --force

# Full setup with all options
/path/to/ai-agent-system/init-ai-agent.sh claude --with-project --guides performance,testing,documentation --force
```

### **What Gets Created**

| Tool | File/Directory Created | Auto-loaded |
|------|----------------------|-------------|
| **Claude Code** | `CLAUDE.md` | ? Yes |
| **Cline** | `.clinerules/` directory with multiple `.md` files | ? Yes |
| **Cursor** | `.cursor/rules/` directory | ? Yes |
| **GitHub Copilot** | `.github/copilot-instructions.md` | ? Yes |

### ? **Manual Setup Examples**

### Claude Code
```bash
# Load core prompt
claude --prompt ./ai-agent-system/core-prompt.md

# Add project context
claude --prompt ./ai-agent-system/core-prompt.md ./project-instructions-myproject.md

# Add specialized guide
claude --prompt ./ai-agent-system/core-prompt.md ./project-instructions-myproject.md ./ai-agent-system/guides/performance-guide.md
```

### Cursor/Cline
Copy and paste the content from the relevant files into your AI chat interface.

### GitHub Copilot
Reference the files in your IDE comments or use them as context for Copilot Chat.

## ? Troubleshooting

### Agent Not Following Standards?
- Try the **REMEMBER** activation prompt to refresh instructions
- Verify all required files are loaded correctly
- Check for conflicting instructions in chat history
- Use the **Context Validation Prompt** to verify understanding

### Generated Code Not Compiling?
- Ensure project instructions match actual codebase structure
- Verify NuGet packages are correctly referenced in suggestions
- Check namespace declarations match your project
- Confirm target framework matches project requirements

### Agent Responses Too Generic?
- Load project-specific instructions if not already loaded
- Request relevant specialized guides for your task
- Provide more specific context about your current problem
- Use the detailed activation prompt for better context

### Performance Suggestions Not Applied?
- Explicitly mention performance requirements in your request
- Load `guides/performance-guide.md` for performance-critical tasks
- Specify if you're working on hot paths or high-throughput scenarios

### Project Instructions Feel Outdated?
- Re-run the project analyzer if architecture has changed significantly
- Update version number and create new project instructions file
- Review and update anti-patterns section for current codebase

## ? Benefits

- **Consistent Quality**: Standardized coding practices across all AI interactions
- **Tool Agnostic**: Works with any AI development tool
- **Scalable**: Easy to add new projects and specialized knowledge
- **Contextual**: Right level of detail for each situation
- **Maintainable**: Single source of truth for coding standards

## ? Contributing

When updating standards or adding new guides:

1. **Core Standards**: Update `core-prompt.md` for changes to coding standards or workflow
2. **Project Templates**: Update `project-template.md` for architectural patterns
3. **Specialized Guides**: Add new files to `guides/` for domain-specific knowledge
4. **Project Instructions**: Version properly and maintain changelog of significant updates

### Quality Guidelines
- Test changes with actual development scenarios before committing
- Ensure cross-references between files remain accurate
- Validate that examples compile and follow current best practices
- Update activation prompts if new capabilities are added

## ? Version History

- **v1.0** (2025-06-09): Initial release with complete tiered system

---

**Note**: This system preserves all personal coding preferences while providing structured, scalable AI agent guidance for development teams.