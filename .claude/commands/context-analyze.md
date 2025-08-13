---
allowed-tools:
  - Agent
  - Bash(git:*)
  - Bash(find:*)
  - Bash(wc:*)
  - Bash(head:*)
  - Bash(tail:*)
  - Bash(mkdir:*)
  - Glob
  - Grep
  - LS
  - Read
  - Write
description: Comprehensive repository analysis using multi-agent approach with extended thinking
version: 1.0.0
created: 2025-07-06
last-updated: 2025-07-06
---

## Arguments
$ARGUMENTS

## Context
- **Current directory**: !`pwd`

- **Git repository**: !`if git rev-parse --git-dir >/dev/null 2>&1; then echo "Git repository detected"; git remote -v | head -3; else echo "Not a git repository"; fi`

- **Source files**: !`git ls-files '*.js' '*.ts' '*.py' '*.java' '*.go' '*.rs' '*.cpp' '*.c' '*.php' '*.rb' '*.cs' | wc -l 2>/dev/null || find . -name "*.js" -o -name "*.ts" -o -name "*.py" -o -name "*.java" -o -name "*.go" -o -name "*.rs" -o -name "*.cpp" -o -name "*.c" -o -name "*.php" -o -name "*.rb" -o -name "*.cs" | wc -l`

- **Repository complexity**: !`SOURCE_COUNT=$(git ls-files '*.js' '*.ts' '*.py' '*.java' '*.go' '*.rs' '*.cpp' '*.c' '*.php' '*.rb' '*.cs' | wc -l 2>/dev/null || find . -name "*.js" -o -name "*.ts" -o -name "*.py" -o -name "*.java" -o -name "*.go" -o -name "*.rs" -o -name "*.cpp" -o -name "*.c" -o -name "*.php" -o -name "*.rb" -o -name "*.cs" | wc -l); if [ "$SOURCE_COUNT" -gt 1000 ]; then echo "Large ($SOURCE_COUNT files) - comprehensive analysis recommended"; elif [ "$SOURCE_COUNT" -gt 300 ]; then echo "Medium ($SOURCE_COUNT files) - standard analysis"; else echo "Small ($SOURCE_COUNT files) - focused analysis"; fi`

- **Output path**: !`if echo "$ARGUMENTS" | grep -q -- "--output"; then OUTPUT_PATH=$(echo "$ARGUMENTS" | sed 's/.*--output[[:space:]]*\([^[:space:]]*\).*/\1/'); else OUTPUT_PATH=".claude/docs/project-context.md"; fi; echo "Analysis will be saved to: $OUTPUT_PATH"`

## Your task

Perform comprehensive repository analysis using multi-agent approach with extended thinking for complex architectural insights.

**Think deeply about this codebase analysis. Use extended thinking to reason through complex relationships and provide comprehensive insights.**

**Note**: The --since timeframe argument will be passed to relevant agents for git history analysis. Technology detection and detailed context gathering will be performed by specialized agents as needed.

### Argument Processing

**Handle --help:**
```
Repository Analyzer - Comprehensive codebase analysis

USAGE:
  /project:analyze [OPTIONS]

OPTIONS:
  --since TIME_PERIOD         Analyze git history since specified time
                              (default: "1 month ago")
  --output PATH               Output file path and name
                              (default: ".claude/memory-bank/project-context.md")
  --help                      Show this help

TIME PERIOD FORMATS:
  "3 months ago", "6 months ago", "1 year ago", "2024-01-01", "last week"

OUTPUT:
  Analysis results will be saved as a markdown file at the specified path.
  Directory will be created automatically if it doesn't exist.
```

**Exit if help requested:**
!`if [ "$ARGUMENTS" = "--help" ]; then exit 0; fi`

### Multi-Agent Analysis Strategy

Deploy specialized analysis agents:

**Phase 1: Structure & Discovery Agent**
- Use LS and Glob to map directory structure
- Identify configuration files, build systems, infrastructure
- Catalog documentation and governance files

**Phase 2: Technology Stack Agent**  
- Parse package managers and dependency files
- Identify languages, frameworks, tools using targeted file detection
- Analyze containerization and deployment patterns
- Perform technology-specific file counting as needed

**Phase 3: Domain & Architecture Agent**
- Identify core domain models and entities
- Map business workflows and use cases
- Analyze architectural patterns and design decisions

**Phase 4: Quality & Operations Agent**
- Assess testing strategies and coverage
- Analyze CI/CD pipelines and deployment practices
- Evaluate code quality tools and standards

**Phase 5: Git Evolution Agent**
- Parse --since argument and set timeframe for analysis
- Examine git history for patterns and hotspots using specified timeframe
- Identify areas of high churn and complexity
- Assess technical debt and maintenance concerns
- Generate contributor analysis and recent activity summary
- Identify most changed files within the specified timeframe

**Phase 6: Multi-Perspective Synthesis**
- **Developer Experience**: Onboarding, readability, workflow
- **Maintainer Concerns**: Complexity, debt, refactoring needs  
- **Architect View**: Modularity, scalability, technical decisions
- **Domain Expert**: Business logic clarity and correctness
- **Performance Specialist**: Efficiency and optimization opportunities

### Project-Type Analysis Focus

**Client projects**: UI frameworks, build tools, bundle analysis, user performance, accessibility, client-side storage
**Server projects**: Databases, APIs, security, scalability, data protection, server infrastructure
**Tool/Library projects**: Developer experience, documentation, API design, distribution, package management
**Fullstack projects**: Both client and server aspects plus integration complexity, deployment coordination

### Analysis Instructions

1. **Parse arguments**: Extract --since timeframe (default: "1 month ago") and --output path (default: ".claude/memory-bank/project-context.md")
2. Think deeply about repository structure and project reveals
3. Consider multiple perspectives as you analyze each component
4. Use Agent tool to deploy specialized analysis agents for complex tasks
5. **Technology Stack Agent**: Perform targeted language detection based on initial findings
6. **Git Evolution Agent**: Use parsed --since timeframe for all git history analysis
7. Think thoroughly about architectural decisions and their implications
8. Adapt analysis focus based on project type identified
9. Generate comprehensive insights valuable for different stakeholders
10. **Save results**: Create output directory if needed and write analysis to specified file

### Output Format

```markdown
# 🧭 Project Type & Complexity

[Write 1-3 paragraphs explaining the classification rationale, what this project type typically involves, and why the complexity level matters for stakeholders, development approaches, and resource planning.]

- **Type**: (Client | Server | Tool/Library | Fullstack)
- **Complexity**: (High | Medium | Low)
- **Justification**: (Provide 2-3 concise reasons for your assessment)
- **Scale**: (Estimated LOC, file count, active contributors)

# 📖 Project Overview

[Write 2-3 paragraphs providing comprehensive context about what this project represents, its place in the broader ecosystem, and the strategic value it delivers to users or the organization.]

- **Description & Value Proposition**: [Detailed 2-3 paragraph description covering what the project does, who it serves, what problems it solves, and what value it provides]
- **Key Features & Modules**: 
  - Feature/Module 1
  - Feature/Module 2
  - Feature/Module 3
  - Feature/Module 4 (if applicable)
  - Feature/Module 5 (if applicable)

# 📁 Repository Structure

[Write 1-3 paragraphs analyzing what the repository organization reveals about the team's development practices, architectural decisions, and project maturity. Explain how the structure supports or hinders development workflow.]

- **Complete Directory Tree**: 
```
[Insert complete filesystem tree structure here]
```
- **Key Directories & Purposes**: 
  [List actual directories found, not placeholders]
  - `/actual-directory` - Actual purpose based on analysis
  - `/another-directory` - Actual purpose based on analysis
  - [Continue with all significant directories discovered...]
- **Configuration & Infrastructure Files**: 
  [List actual configuration files found]
  - `actual-config-file` - Actual purpose
  - `build-config` - Actual purpose
- **Documentation & Governance**: 
  - README quality: (Excellent | Good | Basic | Poor)
  - Additional docs: [List actual documentation files found]

# 🛠 Tech Stack & Dependencies

[Write 1-3 paragraphs evaluating the technology choices, their implications for development velocity, scalability, maintenance burden, and technical risk. Assess whether the stack aligns with project requirements and team capabilities.]

- **Languages & Frameworks**: 
  - Primary: [Language + Framework]
  - Secondary: [Language + Framework] (if applicable)
- **Dependency Management**: 
  - Tool: [Package manager]
  - Key dependencies: [List top 5]
- **Data Storage**: 
  [For Client projects: Focus on client-side storage, local storage, IndexedDB, state management]
  [For Server projects: Analyze databases, caching, data persistence, storage engines]
  [For Tool/Library projects: Usually "Not applicable - no data storage" or configuration storage]
  [For Fullstack projects: Analyze both client and server storage patterns]
  - Database: [Type and technology, if applicable]
  - Caching: [Technology or "None identified"]
  - File storage: [Local/Cloud/CDN approach, if applicable]
- **Communication Protocols**: 
  [For Client projects: API consumption, WebSocket clients, GraphQL queries, external service calls]
  [For Server projects: API endpoints, message queues, service communication, protocols]
  [For Tool/Library projects: Usually "Not applicable - consumed by other projects"]
  [For Fullstack projects: Both API consumption and provision, client-server communication]
  - API style: [REST/GraphQL/gRPC/etc., if applicable]
  - Messaging: [Message queues/Event streams/None]
  - Inter-service: [HTTP/Message bus/Direct calls, if applicable]
- **Containerization & Infrastructure**: 
  [For Client projects: Build containers, CDN deployment, static hosting]
  [For Server projects: Runtime containers, orchestration, service mesh]
  [For Tool/Library projects: Package distribution, registry publishing]
  [For Fullstack projects: Coordinate both client and server deployment]
  - Containerization: [Docker/Podman/None]
  - Orchestration: [Kubernetes/Docker Compose/None]
  - Infrastructure as Code: [Terraform/CloudFormation/None]

# 🔧 Core Components

[Write 1-3 paragraphs explaining how the core components work together, their responsibilities, and what this component design reveals about the system's architecture and potential evolution paths.]

- **Component 1**: [Name] - [Brief description of responsibility]
- **Component 2**: [Name] - [Brief description of responsibility] 
- **Component 3**: [Name] - [Brief description of responsibility]
- **Component 4**: [Name] - [Brief description of responsibility] (if applicable)
- **Component 5**: [Name] - [Brief description of responsibility] (if applicable)

# 🎡 Domain & Business Logic

[Write 1-3 paragraphs analyzing how well the code represents the business domain, whether the domain model is clear and consistent, and how domain complexity affects the technical implementation.]

- **Core Domain Models & Entities**: 
  - Entity 1: [Description]
  - Entity 2: [Description]
  - Entity 3: [Description]
- **Main Workflows & Use Cases**: 
  - Workflow 1: [Description]
  - Workflow 2: [Description]
  - Workflow 3: [Description]
- **Domain Terminology & Concepts**: 
  [Comprehensive list of all identifiable domain-specific terms, business concepts, and their definitions/usage. Include as many terms as can be confidently identified from code, documentation, and naming patterns. This vocabulary helps bridge communication between technical and business stakeholders.]
  - Term 1: [Definition/Usage]
  - Term 2: [Definition/Usage]
  - Term 3: [Definition/Usage]
  - [Continue with all identifiable domain concepts...]

# 🏗 Architecture & Patterns

[Write 1-3 paragraphs evaluating the architectural decisions, their appropriateness for the project's requirements, and how they impact maintainability, scalability, and team productivity. Discuss trade-offs and potential evolution paths.]

- **Architecture Type**: (Monolithic | Microservices | Layered | Event-driven | Hexagonal | Other)
- **Key Architectural Patterns**: 
  - Pattern 1: [Implementation details]
  - Pattern 2: [Implementation details]
  - Pattern 3: [Implementation details]
- **Module/Service Boundaries & Communication**: 
  - [Description of how components interact]

# 🔧 Quality, Testing & CI/CD

[Write 1-3 paragraphs assessing the quality practices, testing strategy, and automation maturity. Explain how these practices impact development velocity, deployment confidence, and system reliability.]

- **Testing Frameworks & Coverage**: 
  - Unit testing: [Framework and approach]
  - Integration testing: [Framework and approach]
  - End-to-end testing: [Framework and approach, if applicable]
- **CI/CD Approaches & Tools**: 
  - CI Tool: [GitHub Actions/Jenkins/GitLab CI/etc.]
  - Deployment: [Manual/Automated/Hybrid]
  - Environments: [dev/staging/prod configuration]
- **Code Quality & Static Analysis Tools**: 
  [List only tools actually found in the project]
  - Linting: [Specific tools identified with configuration files]
  - Static analysis: [Specific tools identified]
  - Code formatting: [Specific tools identified]
  - [Or "No static analysis tools identified" if none found]

# 🔒 Security *(Optional - include if security practices identified)*

[Write 1-3 paragraphs evaluating the security posture, identifying potential vulnerabilities, and assessing whether security practices are appropriate for the project's risk profile and compliance requirements.]

- **Security Practices & Tools**: 
  - Authentication: [Implementation approach]
  - Authorization: [Implementation approach]
  - Data protection: [Encryption, sanitization practices]
- **Vulnerability Management**: 
  - Dependency scanning: [Tools/approach]
  - Static security analysis: [Tools/approach]
- **Compliance & Governance**: 
  - Standards: [GDPR/HIPAA/SOX/etc. if applicable]
  - Security policies: [Identified practices]

# ⚡ Performance *(Optional - include if performance patterns identified)*

[Write 1-3 paragraphs analyzing the performance characteristics, optimization strategies, and scalability considerations. Evaluate whether current patterns support expected load and growth requirements.]

- **Performance Patterns & Tools**: 
  [List only patterns actually identified in the codebase]
  [For Client: Bundle size optimization, lazy loading, caching strategies, rendering performance]
  [For Server: Response times, database optimization, caching, resource pooling]
  [For Tool/Library: API performance, memory usage, execution efficiency]
  [For Fullstack: End-to-end performance, client-server optimization]
  - Caching strategies: [Implementation details, if present]
  - Database optimization: [Indexing, query patterns, if applicable]
  - Async processing: [Implementation approach, if present]
  - Memory management: [Patterns identified, if applicable]
  - Resource pooling: [Connection pools, thread pools, if present]
  - Load balancing: [Strategies used, if applicable]
  - Bundle optimization: [For client projects, if present]
- **Monitoring & Observability**: 
  - Logging: [Framework and approach]
  - Metrics: [Tools and key metrics]
  - Tracing: [Implementation if present]
- **Scalability Considerations**: 
  [Project-type specific analysis]
  - Horizontal scaling: [Approach, if designed for]
  - Vertical scaling: [Considerations, if applicable]
  - Bottleneck identification: [Known or potential issues]

# 🌀 Multi-Perspective Analysis

[Write 1-3 paragraphs explaining how different stakeholders would view this codebase, what concerns each perspective raises, and how well the current implementation serves different stakeholder needs.]

- **Developer Experience**: 
  - Code readability: (Excellent | Good | Fair | Poor)
  - Onboarding ease: (Easy | Moderate | Difficult)
  - Development workflow: [Description of dev experience]
- **Maintainer Concerns**: 
  - Complexity hotspots: [Identified areas]
  - Maintainability: (High | Medium | Low)
  - Refactoring needs: [Priority areas]
- **Architect View**: 
  - Modularity: (Excellent | Good | Fair | Poor)
  - Flexibility: [Ability to adapt to changes]
  - Technical decisions: [Key architectural choices assessment]
- **Domain Expert**: 
  - Domain clarity: (Clear | Moderate | Unclear)
  - Business logic correctness: [Assessment based on code review]
  - Terminology alignment: [Code vs business language alignment]
- **Performance Specialist**: 
  - Efficiency: [Overall performance assessment]
  - Bottlenecks: [Identified performance issues]
  - Optimization opportunities: [Specific recommendations]

# ⚡ Git History & Technical Debt

[Write 1-3 paragraphs analyzing the patterns revealed by git history, what they indicate about code stability and team practices, and how technical debt is accumulating over time.]

- **Code Hotspots & Churn**: 
  - Most changed files: [List top 5 with change frequency]
  - High churn areas: [Modules with frequent changes]
- **Technical Debt & Areas of Risk**: 
  - Debt Level: (High | Medium | Low)
  - Risk areas: [Specific components or practices]
- **Evolution Patterns & Refactoring History**: 
  - Major refactorings: [Recent significant changes]
  - Evolution trends: [Growth patterns, architectural changes]

# 🚩 Summary & Recommendations

- **Strengths**: 
  1. [Strength 1]
  2. [Strength 2]
  3. [Strength 3]
- **Risks & Concerns**: 
  1. [Risk 1] - Severity: (High | Medium | Low)
  2. [Risk 2] - Severity: (High | Medium | Low)
  3. [Risk 3] - Severity: (High | Medium | Low)
- **Prioritized Recommendations**: 
  1. **Priority 1 (Critical)**: [Recommendation]
  2. **Priority 2 (High)**: [Recommendation]
  3. **Priority 3 (Medium)**: [Recommendation]
  4. **Priority 4 (Low)**: [Recommendation] (if applicable)
  5. **Priority 5 (Nice-to-have)**: [Recommendation] (if applicable)

# 📎 Appendix *(Optional - include if significant configurations or ADRs identified)*

- **README Highlights**: 
  - Key information: [Summary of important README content]
  - Missing information: [What should be added]
- **Important Configurations & Snippets**: 
  - Config 1: [File and key settings]
  - Config 2: [File and key settings]
- **ADRs (Architecture Decision Records)**: 
  - [List of identified ADRs or note if none found]
```

Think thoroughly about each section and provide comprehensive, actionable insights that would be valuable for developers, maintainers, architects, and stakeholders working with this codebase. Pay special attention to identifying as many domain concepts and terminology as possible - this vocabulary is crucial for bridging technical and business understanding.
