# ?? Perfect Software Development Prompt Pack

**Version:** 1.0.0  
**Created:** February 14, 2026  
**Purpose:** Master templates and patterns for AI-assisted software development  
**Framework:** .NET 9.0, Avalonia, Clean Architecture, CQRS  

---

## ?? TABLE OF CONTENTS

1. [Introduction](#introduction)
2. [Prompt Engineering Principles](#prompt-engineering-principles)
3. [Prompt Templates](#prompt-templates)
4. [Real-World Examples](#real-world-examples)
5. [Best Practices](#best-practices)
6. [Anti-Patterns to Avoid](#anti-patterns-to-avoid)
7. [Quick Reference](#quick-reference)

---

## ?? INTRODUCTION

This document contains proven prompt templates and patterns that accelerate software development when working with AI assistants (Copilot, ChatGPT, Claude, etc.).

**Key Benefits:**
- ? 5-10x faster feature implementation
- ? Consistent code quality
- ? Reduced debugging time
- ? Better architectural decisions
- ? Automated documentation

**Project Context:**
- Framework: Avalonia (Cross-Platform)
- Architecture: Clean Architecture + CQRS
- Language: C# 13
- Build System: .NET 9.0
- Modules: 4 complete (Email, Users, KB, Predictions)

---

## ?? PROMPT ENGINEERING PRINCIPLES

### **1. Context is King**
Always provide:
- File paths and structure
- Framework/language version
- Project conventions
- Existing patterns
- Specific requirements

### **2. Specificity > Vagueness**
? Bad: "Add a feature"  
? Good: "Create DeleteDocumentCommand with validation, error handling using ErrorOr, logging with Serilog"

### **3. Show, Don't Tell**
Provide examples of existing patterns they should follow:
```csharp
// Reference existing pattern
// Existing: SendEmailCommand (Week 1)
// New: DeleteDocumentCommand (Week 3)
// Follow same structure...
```

### **4. Break Down Complex Tasks**
Don't ask for everything at once. Sequence:
1. Create command (5 min)
2. Create validator (3 min)
3. Create handler (10 min)
4. Test (5 min)

### **5. Clear Success Criteria**
Always define what "done" looks like:
- ? Build succeeds
- ? All tests pass
- ? No regressions
- ? Error handling complete

---

## ?? PROMPT TEMPLATES

### **TEMPLATE 1: Add New Feature (Command/Query)**

```markdown
## Task: Implement [FeatureName] Command/Query

### Context
- **Project:** Mbarie Insight Suite v1.0.0
- **Framework:** .NET 9.0, Avalonia, Clean Architecture
- **Pattern:** CQRS with ErrorOr<T>
- **Module:** [ModuleName]

### Existing Pattern Reference
See: MIC.Core.Application/Emails/Commands/SendEmail/
- Command: ICommand<T> record
- Validator: FluentValidation
- Handler: ICommandHandler<T, TResponse>
- Error Handling: ErrorOr<T>
- Logging: Serilog via ILogger<T>

### Requirements
1. Create command/query class inheriting from ICommand/IQuery
2. Create validator with FluentValidation
3. Create handler implementing ICommandHandler/IQueryHandler
4. Use ErrorOr pattern for error handling
5. Integrate Serilog logging
6. Return proper DTOs

### Files to Create
- `MIC.Core.Application/[Module]/Commands/[Action]/[Action]Command.cs`
- `MIC.Core.Application/[Module]/Commands/[Action]/[Action]CommandValidator.cs`
- `MIC.Core.Application/[Module]/Commands/[Action]/[Action]CommandHandler.cs`

### Success Criteria
- [ ] Build succeeds (0 errors)
- [ ] All tests pass
- [ ] ErrorOr pattern consistent
- [ ] Logging integrated
- [ ] No hardcoded values

### Additional Notes
[Any specific requirements or edge cases]
```

---

### **TEMPLATE 2: Fix Build Errors**

```markdown
## Task: Fix Build Errors

### Error Details
```
[Paste full error message]
```

### Context
- **Project File:** [path]
- **Affected Files:** [list files]
- **Framework:** .NET 9.0
- **Last Working State:** [describe when it worked]

### What Changed
- [List recent changes]
- [Any new dependencies added]
- [File structure changes]

### Investigation Steps Completed
- [x] Checked file paths
- [x] Verified namespaces
- [x] Reviewed imports
- [ ] [Next step]

### Success Criteria
- [ ] Build succeeds
- [ ] No new warnings
- [ ] All tests pass
- [ ] No regressions
```

---

### **TEMPLATE 3: Integrate UI Component**

```markdown
## Task: Wire [Component] to [View]

### Context
- **View File:** MIC.Desktop.Avalonia/Views/[ViewName].axaml
- **ViewModel:** [ViewModelName].cs
- **Framework:** Avalonia XAML
- **Binding Type:** ReactiveUI/MVVM

### Requirements
1. Create XAML binding to ViewModel
2. Wire command/event handlers
3. Integrate with existing assets
4. Follow Avalonia/Avalonia UI patterns
5. Ensure cross-platform compatibility

### Existing Pattern Reference
See: MainWindow.axaml
- Binding syntax: {Binding PropertyName}
- Command binding: Command="{Binding CommandName}"
- Image references: Source="/Assets/[Path]/[Image].png"

### Files to Update
- `MIC.Desktop.Avalonia/Views/[ViewName].axaml`
- `MIC.Desktop.Avalonia/ViewModels/[ViewModelName].cs`

### Success Criteria
- [ ] UI renders correctly
- [ ] Bindings work
- [ ] Commands fire properly
- [ ] No binding errors
- [ ] Cross-platform verified
```

---

### **TEMPLATE 4: Add Unit Tests**

```markdown
## Task: Create Unit Tests for [Feature]

### Context
- **Feature:** [Feature Name]
- **Test Framework:** xUnit + Moq
- **Project:** MIC.Tests.Unit
- **Namespace:** MIC.Tests.Unit.Features.[Module]

### What to Test
1. Valid input ? Success
2. Invalid input ? Validation error
3. Business logic ? Correct behavior
4. Error handling ? ErrorOr handling
5. Edge cases ? [List specific cases]

### Existing Pattern Reference
See: MIC.Tests.Unit/Features/Email/SendEmailCommandTests.cs
- Test class naming: [Feature]Tests
- Test method naming: [Scenario]_[Action]_[Expected]
- Arrange-Act-Assert pattern
- Mock dependencies

### Files to Create
- `MIC.Tests.Unit/Features/[Module]/[Feature]Tests.cs`

### Success Criteria
- [ ] All tests pass
- [ ] >80% code coverage
- [ ] Mocks properly configured
- [ ] Edge cases covered
- [ ] No flaky tests
```

---

### **TEMPLATE 5: Debug Performance Issue**

```markdown
## Task: Debug [PerformanceIssue]

### Symptoms
- [Describe what's slow/broken]
- [When does it happen]
- [Performance metrics if available]

### Context
- **Affected Component:** [Component]
- **Framework:** .NET 9.0
- **Profiler Used:** [None yet / Built-in / dotTrace]
- **Expected Performance:** [X ms / Y calls/sec]

### Initial Investigation
- [ ] Profiled the code
- [ ] Identified hotspots
- [ ] Checked database queries
- [ ] Reviewed async/await patterns

### Suspected Root Cause
[Description based on investigation]

### What We're Looking For
- SQL query optimization opportunities
- Unnecessary allocations
- Blocking calls in async code
- N+1 query problems
- [Other specific issues]

### Success Criteria
- [ ] Performance improved to [target]
- [ ] Profiler confirms improvement
- [ ] No regressions
- [ ] Tests still pass
```

---

### **TEMPLATE 6: Review Architecture Decision**

```markdown
## Task: Architecture Review - [Topic]

### Current State
[Describe current implementation]

### Problem
[What's not working or what we want to improve]

### Current Approach
```csharp
[Code showing current pattern]
```

### Project Context
- **Architecture:** Clean Architecture + CQRS
- **Pattern:** ErrorOr<T> for error handling
- **Framework:** .NET 9.0, Avalonia
- **Constraints:** [Any technical/business constraints]

### Existing Patterns in Project
- [List relevant patterns used elsewhere]

### Alternatives Considered
1. [Option A]
2. [Option B]
3. [Option C]

### Recommendation Needed
- Consistency with project patterns
- Performance implications
- Maintainability
- Testing considerations

### Success Criteria
- [ ] Decision documented
- [ ] Team consensus
- [ ] Implementation plan clear
```

---

## ?? REAL-WORLD EXAMPLES

### **Example 1: Perfect Feature Prompt**

```markdown
## Task: Implement GetAllDocumentsQuery for Knowledge Base

### Context
Project: Mbarie Insight Suite v1.0.0
Module: Knowledge Base (Week 3)
Framework: .NET 9.0, Avalonia, Clean Architecture

### Requirements
Implement a query to retrieve all documents from the knowledge base with:
- Pagination support (PageNumber, PageSize)
- Optional sorting (SortBy parameter)
- ErrorOr<List<DocumentDto>> return type

### Pattern Reference
Use existing pattern from EmailInboxViewModel:
- GetEmailsQuery: Returns paginated results
- Queries use IQuery<T> interface
- Handlers use IQueryHandler<TQuery, TResponse>
- Return ErrorOr<T> with proper error codes

### Files to Create
1. MIC.Core.Application/KnowledgeBase/Queries/GetAllDocuments/GetAllDocumentsQuery.cs
2. MIC.Core.Application/KnowledgeBase/Queries/GetAllDocuments/GetAllDocumentsQueryHandler.cs

### Implementation Details

**GetAllDocumentsQuery.cs:**
```csharp
using MIC.Core.Application.Common.Interfaces;
using MIC.Core.Application.KnowledgeBase.Common;
using ErrorOr;

namespace MIC.Core.Application.KnowledgeBase.Queries.GetAllDocuments;

public record GetAllDocumentsQuery : IQuery<List<DocumentDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
}
```

**GetAllDocumentsQueryHandler.cs:**
- Validate pagination parameters
- Query repository with pagination
- Return ErrorOr<List<DocumentDto>>
- Log with ILogger<GetAllDocumentsQueryHandler>

### Success Criteria
- ? Build succeeds (0 errors)
- ? Validation catches invalid input
- ? Pagination works correctly
- ? Proper error handling
- ? Logging integrated
```

### **Example 2: Perfect Build Fix Prompt**

```markdown
## Task: Fix Build Error CS0246

### Error
```
error CS0246: The type or namespace name 'ErrorOr' could not be found
(are you missing a using directive or an assembly reference?)
Location: MIC.Core.Application/Predictions/Commands/GeneratePrediction/GeneratePredictionCommand.cs
```

### Context
- Project: MIC.Core.Application
- File: GeneratePredictionCommand.cs
- Error: Missing namespace reference to ErrorOr
- Framework: .NET 9.0

### Analysis
The file uses `ErrorOr<T>` but doesn't have `using ErrorOr;`

### Solution Required
1. Add missing using statement: `using ErrorOr;`
2. Verify NuGet package is installed: `dotnet package-search ErrorOr`
3. Run build to verify fix

### Files to Update
- MIC.Core.Application/Predictions/Commands/GeneratePrediction/GeneratePredictionCommand.cs
- Add line at top: `using ErrorOr;`

### Success Criteria
- ? Build succeeds
- ? No CS0246 errors
- ? No new warnings
```

---

## ? BEST PRACTICES

### **1. File Organization**
```
MIC.Core.Application/
??? [Feature]/
?   ??? Commands/
?   ?   ??? [Action]/
?   ?       ??? [Action]Command.cs
?   ?       ??? [Action]CommandValidator.cs
?   ?       ??? [Action]CommandHandler.cs
?   ??? Queries/
?   ?   ??? [Query]/
?   ?       ??? [Query]Query.cs
?   ?       ??? [Query]QueryHandler.cs
?   ??? Common/
?       ??? [Feature]Dto.cs
```

### **2. Naming Conventions**
- Commands: `[VerbNoun]Command` (e.g., `SendEmailCommand`)
- Queries: `[NounReturnType]Query` (e.g., `GetEmailsQuery`)
- Validators: `[CommandName]Validator`
- Handlers: `[CommandName]Handler`
- DTOs: `[Entity]Dto`

### **3. Error Handling Pattern**
```csharp
// Always use ErrorOr<T>
public async Task<ErrorOr<DocumentDto>> Handle(...)
{
    // Validation
    var validationResult = validator.Validate(request);
    if (!validationResult.IsValid)
        return Error.Validation(...);
    
    try
    {
        // Business logic
        var result = await DoSomething();
        return result;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error message");
        return Error.Unexpected(...);
    }
}
```

### **4. Logging Pattern**
```csharp
_logger.LogInformation("Starting operation for {EntityId}", entityId);
_logger.LogError(ex, "Failed to process {EntityId}", entityId);
_logger.LogWarning("Unexpected state in {Method}", nameof(Handle));
```

### **5. Testing Pattern**
```csharp
[Fact]
public async Task Handle_WithValidInput_ReturnsSuccess()
{
    // Arrange
    var command = new [Command] { /* valid data */ };
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.False(result.IsError);
    Assert.NotNull(result.Value);
}

[Fact]
public async Task Handle_WithInvalidInput_ReturnsValidationError()
{
    // Arrange
    var command = new [Command] { /* invalid data */ };
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    Assert.True(result.IsError);
    Assert.Equal(ErrorType.Validation, result.FirstError.Type);
}
```

---

## ?? ANTI-PATTERNS TO AVOID

### **? Anti-Pattern 1: Vague Requirements**
```markdown
// BAD
"Add a feature to upload documents"

// GOOD
"Create UploadDocumentCommand with:
- Validation for file size (50MB max)
- Content type verification
- Error handling via ErrorOr
- Logging for debugging"
```

### **? Anti-Pattern 2: Missing Context**
```markdown
// BAD
"Fix the build error"

// GOOD
"Fix CS0246 error in GeneratePredictionCommand.cs:
- Missing using ErrorOr; statement
- ErrorOr package installed but not referenced"
```

### **? Anti-Pattern 3: Mixing Concerns**
```csharp
// BAD
public class CommandHandler
{
    // Does validation, business logic, logging, AND database calls
    public async Task<ErrorOr<Result>> Handle(Command request)
    {
        // 50 lines of mixed concerns
    }
}

// GOOD
public class CommandHandler
{
    // Only orchestrates: validate ? execute ? return
    public async Task<ErrorOr<Result>> Handle(Command request)
    {
        // Validation via validator
        // Business logic via service
        // Logging via ILogger
        // Clean separation
    }
}
```

### **? Anti-Pattern 4: No Error Handling**
```csharp
// BAD
public async Task<DocumentDto> Handle(Command request)
{
    var document = await _repository.SaveAsync(request);
    return MapToDto(document);
    // What if SaveAsync throws? No error handling!
}

// GOOD
public async Task<ErrorOr<DocumentDto>> Handle(Command request)
{
    try
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
            return Error.Validation(...);
        
        var document = await _repository.SaveAsync(request);
        return MapToDto(document);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error handling command");
        return Error.Unexpected(...);
    }
}
```

### **? Anti-Pattern 5: Tests Without Arrangement**
```csharp
// BAD
[Fact]
public async Task Handle_Works()
{
    var result = await handler.Handle(new Command(), CancellationToken.None);
    Assert.True(result.IsSuccess);
}

// GOOD
[Fact]
public async Task Handle_WithValidCommand_ReturnsSuccess()
{
    // Arrange: Set up specific test data
    var command = new Command { Id = Guid.NewGuid(), ... };
    
    // Act: Execute the handler
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert: Verify specific behavior
    Assert.False(result.IsError);
    Assert.NotNull(result.Value);
}
```

---

## ?? QUICK REFERENCE

### **Creating a New Command (5 minutes)**
```markdown
1. Create [Action]Command.cs
   - public record [Action]Command : ICommand<[ReturnType]>
   - Include all required properties with init

2. Create [Action]CommandValidator.cs
   - public class [Action]CommandValidator : AbstractValidator<[Action]Command>
   - Add RuleFor for each property

3. Create [Action]CommandHandler.cs
   - public class [Action]CommandHandler : ICommandHandler<[Action]Command, [ReturnType]>
   - Validate ? Execute ? Return ErrorOr<T>

4. Add logging via ILogger<T>
   - LogInformation on entry
   - LogError on exceptions
```

### **Common Error Messages to Use**
```csharp
Error.Validation(code: "Entity.ValidationFailed", description: message)
Error.NotFound(code: "Entity.NotFound", description: $"Entity {id} not found")
Error.Conflict(code: "Entity.AlreadyExists", description: "Entity already exists")
Error.Unexpected(code: "Entity.UnexpectedError", description: ex.Message)
Error.Failure(code: "Operation.Failed", description: "Operation could not complete")
```

### **Common Validation Rules**
```csharp
RuleFor(x => x.Id).NotEqual(Guid.Empty)
RuleFor(x => x.Name).NotEmpty().MaximumLength(255)
RuleFor(x => x.Email).EmailAddress()
RuleFor(x => x.Age).GreaterThan(0).LessThan(150)
RuleFor(x => x.Password).MinimumLength(8)
RuleFor(x => x.ToDate).GreaterThan(x => x.FromDate)
```

---

## ?? METRICS TO TRACK

When using these prompts, track:

| Metric | Target | Current |
|--------|--------|---------|
| **Time per feature** | <30 min | - |
| **Build succeeds** | 100% | - |
| **Test pass rate** | 100% | - |
| **Code review iterations** | 1-2 | - |
| **Integration time** | <15 min | - |
| **Regressions** | 0 | - |

---

## ?? NEXT STEPS

1. **Copy a template** that matches your task
2. **Fill in specifics** from your project
3. **Reference existing patterns** (see Examples section)
4. **Provide context** about your framework/architecture
5. **Be specific** about success criteria

---

## ?? SUPPORT

**Need help with a prompt?**
- Review existing examples in this file
- Check anti-patterns section
- Verify you're following naming conventions
- Provide full context and error messages

**Common Issues:**
- ? "Prompt too vague" ? Use templates
- ? "Missing context" ? Include project details
- ? "Build fails" ? Provide full error message
- ? "Unclear output" ? Define success criteria

---

**Document Version:** 1.0.0  
**Last Updated:** February 14, 2026  
**Project:** Mbarie Insight Suite v1.0.0  
**Status:** ? Production Ready

