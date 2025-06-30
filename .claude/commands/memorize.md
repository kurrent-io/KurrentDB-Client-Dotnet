# /memorize Command Specification v1.1

**Last Updated**: 2025-06-20  
**Version**: 1.1  
**Changes**: Enhanced pattern extraction for memory management and connection lifecycle patterns

## Overview
The `/memorize` command automatically analyzes recent development work and updates the project's CLAUDE.md memory with discovered patterns, conventions, and architectural decisions.

## Command Syntax
```bash
/memorize [options]
```

## Options
- `--commits N` - Analyze last N commits (default: 10)
- `--section SECTION` - Update only specific CLAUDE.md section
- `--dry-run` - Show suggestions without applying changes
- `--interactive` - Prompt for confirmation on each suggestion, one by one and waiting for user input
- `--scope SCOPE` - Focus analysis on specific areas (architecture|testing|conventions|preferences)

## Analysis Process

### 1. Git History Analysis
- **Recent Commits**: Examine commit messages and diffs for the last N commits
- **File Changes**: Identify new files, major modifications, and deleted files
- **Code Patterns**: Extract recurring patterns in implementations
- **Test Changes**: Analyze test file modifications and new testing approaches

### 2. Code Pattern Extraction
- **Architectural Patterns**: New design patterns (e.g., immutability, factory methods)
- **API Design**: Method signatures, parameter patterns, return types
- **Error Handling**: Exception types, error messages, recovery patterns
- **Performance Patterns**: Memory management, async usage, optimization techniques

### 3. User Preference Detection
- **Naming Conventions**: Preferred terminology and naming patterns
- **Design Decisions**: Explicit choices made during development
- **Rejected Approaches**: Alternatives considered but not implemented
- **Workflow Preferences**: Development and testing approaches favored

### 4. Testing Convention Analysis
- **Framework Usage**: Testing frameworks preferred (TUnit vs xUnit)
- **Test Structure**: Naming conventions, organization patterns
- **Assertion Styles**: Preferred assertion libraries and patterns
- **Test Coverage**: Types of tests written and coverage approaches

## Memory Update Strategy

### Section Mapping
- **Architecture Guidelines** ← Architectural patterns and design decisions
- **Code Standards** ← Coding conventions and style preferences
- **Testing Strategy** ← Test frameworks, naming, and patterns
- **Performance Guidelines** ← Optimization patterns and practices
- **Development Workflow** ← Build processes and development practices

### Update Format
For each discovered pattern:
```markdown
### [Pattern Name] (Added: YYYY-MM-DD)
- **Description**: What the pattern does
- **Rationale**: Why this approach was chosen
- **Implementation**: Key implementation details
- **Usage Guidelines**: When and how to use
- **Examples**: Code examples demonstrating the pattern
```

## Example Output
```
🔍 Analyzing last 10 commits...
📊 Found 15 file changes across 8 files
🎯 Extracted 3 new patterns:

1. Lock/Unlock Immutability Pattern
   - Explicit state management with CreateUnlockedCopy()
   - User preference for explicit over implicit operations
   - Comprehensive mutation protection

2. TUnit Testing Migration
   - Moving from xUnit to TUnit for new tests
   - Snake_case naming convention
   - Shouldly assertion preference

3. Result Type Error Mapping
   - Consistent error mapping patterns
   - Specific exception to domain error conversion
   - MapAsync usage for error transformation

📝 Suggested CLAUDE.md updates:
   - Add "Immutability Patterns" section
   - Update "Testing Strategy" with TUnit guidelines
   - Enhance "Error Handling" with mapping patterns

Apply these changes? [y/N]
```

## Implementation Notes

### Git Integration
- Use `git log --oneline -n N` for commit history
- Use `git diff HEAD~N` for cumulative changes
- Parse commit messages for conventional commit patterns
- Analyze file extensions to categorize changes

### Pattern Recognition
- **Regex Patterns**: Common code patterns (class definitions, method signatures)
- **AST Analysis**: Parse C# syntax trees for structural patterns
- **Test Pattern Detection**: Identify test naming and structure conventions
- **Documentation Extraction**: Parse XML docs and comments for insights

### Memory Preservation
- **Non-destructive Updates**: Add new sections without removing existing content
- **Version Tracking**: Tag updates with dates and commit references
- **Conflict Resolution**: Handle conflicts between new and existing guidelines
- **Backup Strategy**: Create backup before major updates

## Command Flow
1. **Initialization**: Parse command options and validate git repository
2. **Data Collection**: Gather git history, file changes, and conversation context
3. **Pattern Analysis**: Extract patterns using multiple analysis techniques
4. **Change Generation**: Create structured CLAUDE.md updates
5. **User Review**: Present changes for approval (unless --auto flag used)
6. **Application**: Apply approved changes to CLAUDE.md
7. **Verification**: Validate CLAUDE.md syntax and structure

## Error Handling
- **Git Repository Check**: Ensure command runs in valid git repository
- **CLAUDE.md Validation**: Verify CLAUDE.md exists and is writable
- **Pattern Extraction Failures**: Gracefully handle parsing errors
- **Merge Conflicts**: Detect and resolve CLAUDE.md conflicts
- **Rollback Capability**: Provide undo functionality for changes

## Future Enhancements
- **AI-Powered Analysis**: Use LLM to understand semantic patterns
- **Cross-Project Learning**: Share patterns across related projects
- **Integration Hooks**: Trigger on git commit or pull request
- **Pattern Templates**: Predefined templates for common patterns
- **Metric Tracking**: Track pattern adoption and effectiveness
