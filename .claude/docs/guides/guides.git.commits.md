# Git Commit Message Guidelines

## Overview

**Purpose:** Generate high-quality Git commit messages following Conventional Commits specification

**Goal:** Maintain clear, searchable, and automated-friendly commit history

## Structure

### Format
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Character Limits
- **Header:** 50 characters maximum for type + scope + description
- **Body:** 72 characters per line

## Commit Types

| Type       | Description                                         | Use When                                                           |
| ---------- | --------------------------------------------------- | ------------------------------------------------------------------ |
| `feat`     | A new feature for the user                          | Adding functionality that end users can directly benefit from      |
| `fix`      | A bug fix for the user                              | Resolving issues that negatively impact user experience            |
| `docs`     | Documentation changes                               | Updating README, API docs, code comments, or guides                |
| `style`    | Code style changes                                  | Formatting, linting, whitespace (no logic changes)                 |
| `refactor` | Code changes that neither fix bugs nor add features | Improving code structure without changing external behavior        |
| `perf`     | Performance improvements                            | Optimizations that measurably improve speed, memory, or efficiency |
| `test`     | Adding or updating tests                            | New tests, test fixes, or test infrastructure changes              |
| `build`    | Changes to build system or external dependencies    | Build scripts, package.json, Dockerfile, webpack config            |
| `ci`       | Changes to CI configuration files and scripts       | GitHub Actions, Jenkins, CircleCI, deployment scripts              |
| `chore`    | Other changes that don't modify src or test files   | Tooling, maintenance tasks, project setup                          |
| `revert`   | Reverts a previous commit                           | Undoing a previous commit (include original commit hash)           |
| `security` | Security-related changes                            | Vulnerability fixes, dependency security updates                   |

## Breaking Changes (Enhanced)

### Automatic Detection Signals
**API Changes:**
- Removed or renamed public methods, classes, or interfaces
- Changed method signatures, parameter types, or return types
- Modified function parameter order or requirements
- Removed or renamed public properties or configuration options

**Behavioral Changes:**
- Changed default values that affect existing behavior
- Modified error handling or exception types
- Changed data format or structure requirements
- Altered validation rules or constraints

**Infrastructure Changes:**
- Database schema modifications requiring migration
- Configuration file format changes
- Environment variable changes
- Minimum version requirement increases

**Protocol Changes:**
- API endpoint modifications
- Message format changes
- Authentication/authorization requirement changes

### Breaking Change Severity
- **Critical:** Immediate action required (security, data loss risk)
- **Major:** Significant effort to adapt (API restructure)
- **Minor:** Easy to adapt (deprecated with alternatives)

### Breaking Change Template
```
feat!: migrate user authentication to OAuth 2.0

Replace custom JWT implementation with industry-standard OAuth 2.0
for improved security and third-party integration support.

BREAKING CHANGE: Authentication endpoints have changed:
- POST /auth/login → POST /oauth/authorize
- Token format changed from custom JWT to OAuth 2.0 access tokens
- User session management now requires OAuth client credentials

Migration guide: https://docs.example.com/migrate-to-oauth
```

## Intelligent Change Analysis

### Change Impact Assessment
Before generating a commit message, analyze:

1. **User Impact Level**
    - **High:** Changes user-facing functionality, UI, or API behavior
    - **Medium:** Changes internal logic that might affect performance or reliability
    - **Low:** Code style, refactoring, or internal improvements

2. **Change Scope Analysis**
    - **Single Responsibility:** One logical change (preferred)
    - **Related Changes:** Multiple changes supporting one feature/fix
    - **Mixed Changes:** Unrelated changes (suggest splitting)

3. **Technical Complexity**
    - **Simple:** Straightforward change with obvious impact
    - **Complex:** Requires explanation of approach or trade-offs
    - **Critical:** Breaking changes, security fixes, or architectural changes

### Smart Type Detection Rules

| Code Pattern                | Likely Type | Reasoning                                       |
| --------------------------- | ----------- | ----------------------------------------------- |
| New public methods/classes  | `feat`      | Adding user-accessible functionality            |
| Bug fix keywords in changes | `fix`       | Words like "fix", "bug", "issue", "error"       |
| Performance optimizations   | `perf`      | Algorithmic improvements, caching, lazy loading |
| Test files only             | `test`      | Changes exclusively in test directories         |
| Documentation files only    | `docs`      | README, .md files, code comments                |
| Package.json dependencies   | `build`     | Dependency updates or build tool changes        |
| CI config files             | `ci`        | .github/, .circleci/, jenkins files             |
| Code formatting only        | `style`     | No logical changes, just formatting             |
| Security vulnerabilities    | `security`  | CVE fixes, dependency security updates          |

## Advanced Scope Detection

### Dynamic Scope Inference
```
Priority Order:
1. Most modified component/module
2. Feature area affected
3. Layer of architecture changed
4. Fallback to general area
```

### Intelligent Scope Patterns
| Pattern                         | Scope        | Example                                    |
| ------------------------------- | ------------ | ------------------------------------------ |
| `src/components/auth/**`        | `auth`       | Authentication components                  |
| `src/api/users/**`              | `user-api`   | User-related API endpoints                 |
| `src/utils/**` + multiple areas | `utils`      | Utility functions affecting multiple areas |
| `database/migrations/**`        | `db`         | Database schema changes                    |
| `config/**`                     | `config`     | Configuration changes                      |
| `scripts/**`                    | `scripts`    | Build/deployment scripts                   |
| Multiple unrelated areas        | _(no scope)_ | Cross-cutting changes                      |

### Scope Selection Rules
- If 70%+ of changes are in one area → use that scope
- If changes span 2-3 related areas → use broader scope
- If changes span many unrelated areas → omit scope
- For breaking changes → always include affected scope

## Description Rules

- Use imperative mood ("add" not "added" or "adds")
- Start with lowercase letter
- No trailing period
- Be concise but descriptive
- Focus on user-facing impact, not implementation details
- Avoid vague terms like "fix issue" or "update code"

## Body Guidelines

### When to Include
- Complex changes requiring explanation
- Multiple related changes in one commit
- Context about why the change was necessary
- Implementation approach that isn't obvious

### Format Rules
- Separate from description with blank line
- Wrap at 72 characters
- Use imperative mood
- Explain what and why, not how
- Use bullet points for multiple changes

## Footer Guidelines

- **Breaking Changes:** Use "BREAKING CHANGE:" followed by explanation
- **Issue References:**
    - `Fixes #123`
    - `Closes #456`
    - `Resolves #789`
    - `Related to #101`
- **Co-authors:** `Co-authored-by: Name <email@example.com>`

## Advanced Generation Process

### 1. Context Analysis
- **File Analysis:** Examine changed files, additions, deletions, modifications
- **Change Pattern Recognition:** Identify common patterns (new feature, bug fix, refactor)
- **Impact Assessment:** Determine user-facing vs internal changes
- **Complexity Evaluation:** Simple vs complex changes requiring explanation

### 2. Intelligent Type Selection
```
Decision Tree:
├── New functionality visible to users? → feat
├── Fixes user-reported issue? → fix
├── Only documentation changed? → docs
├── Only formatting/style changes? → style
├── Improves performance measurably? → perf
├── Only test files changed? → test
├── Build/dependency changes? → build
├── CI/deployment changes? → ci
├── Security-related? → security
└── Other maintenance? → chore
```

### 3. Smart Scope Detection
- Analyze file paths for primary affected component
- Consider change concentration (70% rule)
- Check for cross-cutting concerns
- Default to broader scope for multi-area changes

### 4. Description Crafting
- Lead with most impactful change
- Use action-oriented language
- Avoid implementation details
- Focus on user/developer benefit

### 5. Body Content Decision
```
Include body if:
├── Breaking changes need explanation
├── Complex technical decisions made
├── Multiple related changes in one commit
├── Non-obvious "why" behind the change
├── Performance impact data available
└── Migration steps required
```

## Commit Templates

### Feature Development
```
feat(scope): add [capability] for [user benefit]

[Optional: Explain approach or design decisions]

[Optional: Performance implications]
[Optional: Related to #123]
```

### Bug Fixes
```
fix(scope): prevent [error condition] when [scenario]

[Optional: Root cause explanation]
[Optional: Impact assessment]

Fixes #123
```

### Performance Improvements
```
perf(scope): optimize [operation] by [improvement]

[Performance metrics if available]
- Before: X ms/operations
- After: Y ms/operations
- Improvement: Z% faster

[Optional: Technical approach]
```

### Security Fixes
```
security(scope): fix [vulnerability type] in [component]

[Impact assessment]
[Affected versions]

[Optional: CVE reference]
[Optional: Credit to reporter]
```

### Breaking Changes
```
feat!(scope): [change description]

[Explanation of new behavior]

BREAKING CHANGE: [What changed and why]
- [Specific change 1]
- [Specific change 2]

Migration: [Steps to adapt]
Related to #123
```

### Dependency Updates
```
build(deps): update [package] from [old] to [new]

[Optional: Notable changes or security fixes]
[Optional: Breaking changes in dependencies]

[Optional: Related security advisory]
```

## Multi-File Handling

- If changes span multiple unrelated areas, suggest splitting the commit
- For related changes across multiple files, choose the most impactful scope
- List secondary changes in commit body when relevant
- Prioritize the most significant change for type determination

## Examples

### Good Examples
- `feat(auth): add JWT token refresh mechanism`
- `fix(api): resolve race condition in concurrent requests`
- `docs: update installation guide with Docker setup`
- `refactor(parser): extract validation logic to separate module`
- `perf(query): optimize database connection pooling`
- `test(integration): add error handling scenarios`

### Bad Examples
- ❌ `feat: Added some new stuff` (too vague)
- ❌ `fix(client): Fixed a bug.` (has period)
- ❌ `FEAT: Add new feature` (wrong case)
- ❌ `feat(client): adds retry mechanism` (wrong tense)
- ❌ `fix: changed variable name from x to userId` (implementation detail)

## Context Patterns

### Feature Development
- `feat(component): add new capability`
- `feat(api): support additional data format`
- `feat(ui): implement user dashboard`

### Bug Fixes
- `fix(component): prevent error condition`
- `fix(validation): handle edge case properly`
- `fix(memory): resolve leak in long-running process`

### Maintenance
- `chore(deps): update framework to latest version`
- `build(docker): optimize container build process`
- `ci(pipeline): add automated security scanning`

## Enhanced Quality Checklist

### Format Validation
- [ ] Follows conventional commits format exactly
- [ ] Type is from approved list (feat, fix, docs, etc.)
- [ ] Scope uses kebab-case and is project-appropriate
- [ ] Description starts with lowercase, no trailing period
- [ ] Total header length ≤ 50 characters
- [ ] Body lines wrapped at 72 characters
- [ ] Blank line separates header from body

### Content Quality
- [ ] Type accurately reflects the primary change
- [ ] Description uses imperative mood ("add" not "added")
- [ ] Focuses on user/developer impact, not implementation
- [ ] Avoids vague terms ("fix issue", "update code")
- [ ] Body explains "why" and "what", not "how"
- [ ] Breaking changes properly flagged and explained
- [ ] Security implications addressed if applicable

### Completeness
- [ ] All significant changes represented
- [ ] Issue references included when applicable
- [ ] Breaking changes documented with migration info
- [ ] Co-authors credited if applicable
- [ ] Performance impacts noted if significant

### Future-Proofing
- [ ] Message will be clear to developers in 6 months
- [ ] Contains enough context for release notes
- [ ] Enables proper semantic versioning
- [ ] Supports automated changelog generation

## Advanced Scenarios & Edge Cases

### Large Refactoring
```
refactor(core): restructure authentication system for maintainability

Extract authentication logic into dedicated service layer to improve
testability and prepare for future OAuth integration.

- Move auth functions from utils to dedicated AuthService
- Separate token validation and user session management
- Add comprehensive unit tests for auth components
- Maintain backward compatibility for existing integrations

No user-facing changes. All existing API endpoints remain functional.
```

### Emergency Hotfix
```
fix!: patch critical security vulnerability in user authentication

Immediately address SQL injection vulnerability in login endpoint
discovered in security audit.

BREAKING CHANGE: Login endpoint now requires additional CSRF token
validation. All clients must update authentication flow.

CVE-2024-XXXX
Credit: Security Researcher Name
```

### Mass File Movement
```
chore: reorganize project structure for better maintainability

Move source files to follow standard project layout:
- src/components → client/components  
- src/api → server/api
- src/shared → shared/lib

No functional changes. All imports updated accordingly.
Update your local development setup: npm run setup
```

### Dependency Security Update
```
security(deps): update lodash to patch prototype pollution vulnerability

Update lodash from 4.17.15 to 4.17.21 to address CVE-2020-8203
prototype pollution vulnerability in zipObjectDeep function.

No breaking changes expected. All existing functionality preserved.
Security advisory: GHSA-35jh-r3h4-6jhm
```

### Multi-Package Monorepo
```
feat(api,client): add real-time notifications system

Implement WebSocket-based notifications across API and client packages:

API changes:
- Add WebSocket server with event broadcasting
- Create notification event types and schemas
- Add user subscription management endpoints

Client changes:  
- Add WebSocket client with auto-reconnection
- Implement notification UI components
- Add user notification preferences

Related to #456
```

## Commit Splitting Guidelines

### When to Split
- **Different Types:** Don't mix feat + fix in same commit
- **Unrelated Scopes:** Changes to auth + docs should be separate
- **Independent Value:** Each commit should provide standalone value
- **Review Complexity:** If review would be easier with separate commits

### How to Split
1. **By Feature Boundary:** Each new capability = separate commit
2. **By Impact Level:** Breaking changes separate from non-breaking
3. **By Scope:** Different components = different commits
4. **By Type:** Tests separate from implementation (if substantial)

### Suggested Split Examples
```
# Instead of one commit with:
feat: add user profiles with validation and tests

# Split into:
feat(user): add user profile management
test(user): add comprehensive profile validation tests
docs(api): document new user profile endpoints
```

## AI-Powered Commit Generation Best Practices

### Context Analysis for AI
When generating commits, always:

1. **Request Full Context**
    - Ask for `git diff --staged` or file changes
    - Inquire about related issues or tickets
    - Understand the broader feature/fix context
    - Check if this is part of a larger change series

2. **Semantic Understanding**
    - Analyze code changes for actual functionality impact
    - Distinguish between refactoring and feature addition
    - Identify performance implications from code patterns
    - Recognize security-sensitive changes

3. **Project-Specific Intelligence**
    - Learn project conventions from existing commit history
    - Adapt scope naming to project patterns
    - Recognize project-specific components and modules
    - Understand team's breaking change thresholds

### Multi-Commit Workflow Intelligence
```
Scenario: User adds feature with tests and documentation

AI Should Suggest:
1. feat(auth): add two-factor authentication support
2. test(auth): add comprehensive 2FA test coverage  
3. docs(security): document 2FA setup and usage

Rather than single commit mixing all types.
```

### Context-Aware Suggestions
- **Large PRs:** Suggest logical commit boundaries
- **Security Changes:** Emphasize security implications
- **Performance Changes:** Request benchmark data for commit body
- **Breaking Changes:** Ensure migration guidance is complete
- **Hotfixes:** Prioritize clarity and urgency indicators

### Validation and Refinement
1. **Validate Against Project History**
    - Check if suggested scope matches existing patterns
    - Ensure type usage aligns with team conventions
    - Verify character limits are respected

2. **Suggest Improvements**
    - Offer alternative phrasings for clarity
    - Recommend additional context for complex changes
    - Suggest commit splitting when beneficial

3. **Educational Feedback**
    - Explain why specific type/scope was chosen
    - Highlight best practices being followed
    - Point out potential improvements

### Advanced Features
- **Semantic Commit Analysis:** Understand change impact beyond file names
- **Release Impact Prediction:** Indicate how commit affects semantic versioning
- **Changelog Readiness:** Ensure commits are changelog-friendly
- **Automation Compatibility:** Structure for CI/CD and tooling integration

## Automation Compatibility (Enhanced)

### Semantic Versioning Integration
```
Type Mapping:
- feat → MINOR version bump
- fix → PATCH version bump  
- BREAKING CHANGE → MAJOR version bump
- perf → PATCH version bump
- security → PATCH version bump (with security flag)
```

### Changelog Generation Support
```
Categories:
- Features (feat)
- Bug Fixes (fix)  
- Performance (perf)
- Security (security)
- Breaking Changes (any type with !)
- Dependencies (build with deps scope)
```

### CI/CD Integration Markers
```
Special Indicators:
- [skip ci] → Skip continuous integration
- [deploy] → Trigger deployment pipeline
- [breaking] → Require manual approval
- [security] → Trigger security scan
- [perf] → Run performance benchmarks
```

### Release Notes Automation
Structure commits to enable automatic generation of:
- User-facing feature lists
- Bug fix summaries
- Breaking change migration guides
- Security update notifications
- Performance improvement metrics
