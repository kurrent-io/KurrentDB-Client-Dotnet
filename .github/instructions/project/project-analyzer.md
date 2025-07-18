# Repository Analysis & Project Instructions Generator

## Your Task

Analyze this repository thoroughly and create customized project instructions using the provided Project Instructions Template. You'll need to understand the codebase architecture, patterns, domain concepts, and implementation approaches to fill out the template accurately.

## Project Type Analysis

Before beginning the detailed analysis, identify the project type to adapt your analysis approach:

### **Legacy Codebase**
- **Focus**: Consistency over modernization, gradual improvement patterns
- **Priorities**: Backward compatibility, risk mitigation, incremental refactoring
- **Special Considerations**: Document existing patterns even if not ideal, identify safe modernization paths

### **Microservice Architecture**
- **Focus**: Service boundaries, communication patterns, data consistency
- **Priorities**: API contracts, resilience patterns, observability, deployment independence
- **Special Considerations**: Cross-service communication, distributed transaction patterns, service discovery

### **Library/SDK Projects**
- **Focus**: Public API design, backward compatibility, consumer experience
- **Priorities**: API stability, comprehensive documentation, performance, minimal dependencies
- **Special Considerations**: Breaking change management, semantic versioning, multi-target support

### **Application Projects**
- **Focus**: User workflows, business logic, feature delivery
- **Priorities**: User experience, business value delivery, maintainability, feature velocity
- **Special Considerations**: Business domain modeling, user journey patterns, feature organization

### **Infrastructure/Platform Projects**
- **Focus**: Reliability, scalability, operational concerns
- **Priorities**: Monitoring, deployment, configuration, disaster recovery
- **Special Considerations**: Operational patterns, scaling strategies, monitoring and alerting

### **Data Processing/Analytics Projects**
- **Focus**: Data flow, processing patterns, performance optimization
- **Priorities**: Throughput, data quality, pipeline reliability, resource efficiency
- **Special Considerations**: Data validation, error handling, backpressure, batch vs streaming

## Analysis Phase

### Step 1: Repository Structure Analysis
- Examine the overall directory structure and organization
- Identify the main source directories, test directories, documentation, examples
- Note build configuration files, dependency management files
- Look for configuration directories, deployment files, or infrastructure code

### Step 2: Technology Stack Identification
- Determine the primary programming language(s) and framework versions
- Identify key dependencies and their purposes
- Note communication protocols (REST, gRPC, GraphQL, message queues, etc.)
- Identify data storage technologies and patterns
- Look for deployment/runtime targets

### Step 3: Domain and Business Logic Analysis
- Read README files and documentation to understand the project's purpose
- Examine core domain models, entities, and value objects
- Identify the main business concepts and their relationships
- Look for domain-specific terminology and concepts
- Understand the primary use cases and user workflows

### Step 4: Architecture Pattern Recognition
- Identify architectural patterns (layered, hexagonal, event-driven, microservices, etc.)
- Look for design patterns (repository, factory, strategy, observer, etc.)
- Examine how components are organized and interact
- Identify abstraction layers and their responsibilities
- Note dependency injection patterns and configurations

### Step 5: API and Integration Analysis
- Examine public APIs and their design principles
- Look for integration patterns with external systems
- Identify client libraries, SDKs, or interfaces
- Note authentication, authorization, and security patterns
- Look for versioning strategies

### Step 6: Performance and Scalability Patterns
- Identify performance-critical code paths
- Look for caching strategies, connection pooling, batching
- Examine async/await patterns and concurrency handling
- Note any optimization techniques specific to the domain
- Look for scalability considerations

### Step 7: Testing Strategy Analysis
- Examine test structure and organization
- Identify testing frameworks and patterns used
- Look for unit tests, integration tests, performance tests
- Note mocking strategies and test data management
- Identify testing anti-patterns to avoid

## Template Completion Phase

Using your analysis, fill out the **Project Instructions Template** by replacing all `[BRACKETED_PLACEHOLDERS]` with project-specific information:

### Required Replacements
- `[PROJECT_NAME]` ? Actual project name
- `[Feature 1-6]` ? Key features identified from analysis
- `[Runtime/Framework]` ? Technology stack details
- `[Protocol/Communication]` ? Communication mechanisms
- `[API Design]` ? API design approach
- `[Compatibility]` ? Platform/environment support
- `[Use Case 1-4]` ? Primary use cases for this project
- `[Core Domain Concept 1-3]` ? Main domain concepts
- `[Layer 1-4 Name]` ? Architecture layer names and responsibilities
- `[Component Category 1-3]` ? Component groupings
- `[Pattern 1-3 Name]` ? Integration patterns found
- `[Principle 1-4]` ? Design principles evident in the code
- Directory structure ? Actual project directories with descriptions

### Content Guidelines

**Project Overview Section:**
- Write a compelling description of what the project does
- Focus on developer benefits and business value
- Use the project's own terminology and concepts
- Keep it concise but comprehensive

**Architecture Section:**
- Identify the actual architectural layers and patterns
- Explain how components interact and depend on each other
- Use specific names and concepts from the codebase
- Focus on the most important architectural decisions

**Domain-Specific Principles:**
- Extract principles that are unique to this project's domain
- Look for patterns that repeat throughout the codebase
- Identify constraints that guide implementation decisions
- Note any unique approaches or innovations

**Anti-Patterns Section:**
- Identify patterns that the codebase specifically avoids
- Look for comments or documentation about what NOT to do
- Identify common mistakes that would break the project's design
- Note performance or scalability pitfalls specific to this domain

**Implementation Priorities:**
- Determine what the project values most (performance, simplicity, flexibility, etc.)
- Look at code quality indicators and optimization patterns
- Identify what makes this project successful
- Note any trade-offs that were made consciously
- **Adapt priorities based on project type** (see Project Type Analysis section)

## Quality Criteria

Your analysis should result in project instructions that:

### Accuracy
- Correctly represent the actual codebase structure and patterns
- Use terminology consistent with the project's documentation
- Reflect the actual implementation approaches used

### Completeness
- Cover all major architectural components and patterns
- Include domain-specific knowledge that would be non-obvious to newcomers
- Address both happy-path and edge-case considerations

### Specificity
- Provide concrete, actionable guidance
- Avoid generic advice that could apply to any project
- Include specific examples relevant to this codebase

### Developer Focus
- Help developers understand how to work effectively in this codebase
- Identify the patterns they should follow and pitfalls they should avoid
- Provide context for architectural decisions

## Example Quality Indicators

**Good:** "Events are immutable facts representing changes in the system. The event log is the source of truth, not the current state. Use optimistic concurrency for handling conflicting writes."

**Bad:** "Follow good software engineering practices and write clean code."

**Good:** "Basic client usage follows this pattern: configure client ? connect ? append/read events ? handle responses"

**Bad:** "Use the client library to interact with the system."

## Output Format

Provide the completed Project Instructions by:
1. **Project Type Identification**: Clearly state the project type and analysis approach used
2. **Analysis Summary**: Brief summary of your analysis findings and key architectural decisions
3. **Completed Template**: Present the filled-out template with all placeholders replaced
4. **Unique Aspects**: Highlight any unique or noteworthy aspects of this project's architecture
5. **Assumptions**: Note any areas where you needed to make assumptions due to limited information
6. **Adaptation Notes**: Explain how the analysis was adapted for the specific project type

## Final Checklist

Before submitting, verify that:
- [ ] **Project type has been correctly identified** and analysis adapted accordingly
- [ ] All `[BRACKETED_PLACEHOLDERS]` have been replaced
- [ ] The architecture section accurately reflects the codebase structure
- [ ] Domain concepts are explained in the project's own terminology
- [ ] Anti-patterns section includes project-specific pitfalls
- [ ] Implementation priorities align with evidence from the codebase AND project type
- [ ] The directory structure matches the actual repository layout
- [ ] All sections provide actionable, specific guidance
- [ ] **Project type considerations** are reflected in principles and anti-patterns

## Cross-References

**After completing analysis**, mention relevant specialized guides:
- For performance-critical projects: Recommend loading `performance.instructions.md`
- For projects with extensive testing: Recommend loading `testing.instructions.md` 
- For library/SDK projects: Recommend loading `documentation.instructions.md`

Begin your analysis now.
