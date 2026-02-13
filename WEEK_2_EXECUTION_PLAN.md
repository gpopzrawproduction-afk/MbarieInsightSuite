# ?? WEEK 2 EXECUTION PLAN - USER PROFILE MODULE
## Mbarie Insight Suite v1.0 - Production Build

**Timeline:** 4 days (Days 1-4 of Week 2)  
**Based on:** Week 1 Email Module Pattern (Proven & Working)  
**Status:** Ready to Execute

---

## ?? SCOPE: USER PROFILE OPERATIONS

### **Commands to Implement**

1. **UpdateUserProfileCommand**
   - Update user name, email, avatar
   - Update notification preferences
   - Update theme/display settings
   - Validation: non-empty fields, valid email

2. **GetUserProfileQuery**
   - Retrieve current user profile
   - Return user details + settings
   - Cache-friendly

3. **ChangePasswordCommand**
   - Current password validation
   - New password requirements (complexity, length)
   - Password hashing via IPasswordHasher
   - Audit logging

4. **LogoutCommand**
   - Clear session
   - Remove tokens
   - Log user activity

5. **UpdateNotificationPreferencesCommand**
   - Email notifications on/off
   - Toast notifications on/off
   - Alert preferences
   - Per-category settings

---

## ??? ARCHITECTURE (EXACT SAME AS WEEK 1)

```
User Input ? Command (DTO)
           ?
        Validator (FluentValidation)
           ?
        Handler (ICommandHandler<T, TResponse>)
           ?
        Return ErrorOr<T>
           ?
      UI/ViewModel uses result
```

**Files to Create:**

```
MIC.Core.Application/Users/Commands/
?? UpdateUserProfile/
?  ?? UpdateUserProfileCommand.cs
?  ?? UpdateUserProfileCommandValidator.cs
?  ?? UpdateUserProfileCommandHandler.cs
?? ChangePassword/
?  ?? ChangePasswordCommand.cs
?  ?? ChangePasswordCommandValidator.cs
?  ?? ChangePasswordCommandHandler.cs
?? Logout/
?  ?? LogoutCommand.cs
?  ?? LogoutCommandHandler.cs
?? UpdateNotificationPreferences/
   ?? UpdateNotificationPreferencesCommand.cs
   ?? UpdateNotificationPreferencesCommandValidator.cs
   ?? UpdateNotificationPreferencesCommandHandler.cs

MIC.Core.Application/Users/Queries/
?? GetUserProfile/
?  ?? GetUserProfileQuery.cs
?  ?? GetUserProfileQueryHandler.cs

MIC.Tests.Unit/Features/User/
?? UpdateUserProfileCommandTests.cs
?? ChangePasswordCommandTests.cs
?? NotificationPreferencesCommandTests.cs
```

---

## ?? WEEK 2 SCHEDULE

### **Day 1: Commands (Commands layer)**
- Create 5 commands
- Create 3 validators
- 30-45 minutes

### **Day 2: Handlers**
- Create 5 handlers
- Proper error handling
- Logging integration
- 45 minutes

### **Day 3: Tests + Integration**
- Write unit tests
- DI registration
- Build verification
- 45 minutes

### **Day 4: Cross-Platform + Commit**
- Test on Windows 11 ?
- Test on macOS ?
- Commit to GitHub ?
- 30 minutes

---

## ? WEEK 2 SUCCESS CRITERIA

- [ ] 5 commands created
- [ ] 4 handlers created (GetUserProfile is a query)
- [ ] All validators created
- [ ] Build succeeds (0 errors)
- [ ] All tests pass (100%)
- [ ] DI registration complete
- [ ] Committed to GitHub (main branch)
- [ ] CI/CD ready

---

## ?? SAME PATTERN AS WEEK 1

**Quick Reference:**

```csharp
// 1. COMMAND (record)
public record UpdateUserProfileCommand : ICommand<UserProfileDto>
{
    public string UserId { get; init; }
    public string Name { get; init; }
    // ... properties
}

// 2. VALIDATOR
public class UpdateUserProfileCommandValidator : AbstractValidator<UpdateUserProfileCommand>
{
    public UpdateUserProfileCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        // ... more rules
    }
}

// 3. HANDLER
public class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, UserProfileDto>
{
    public async Task<ErrorOr<UserProfileDto>> Handle(
        UpdateUserProfileCommand request, 
        CancellationToken cancellationToken)
    {
        // Validate
        // Process
        // Return ErrorOr<T>
    }
}

// 4. DI REGISTRATION (in DependencyInjection.cs)
services.AddScoped<ICommandHandler<UpdateUserProfileCommand, UserProfileDto>, UpdateUserProfileCommandHandler>();
```

---

## ?? READY TO BUILD

**Starting with:** 
1. Commands definitions
2. Validators
3. Handlers (logging + error handling)
4. Tests
5. DI registration
6. Build + test
7. Commit to GitHub

**Expected completion:** 4 hours total

Let's go! ??

