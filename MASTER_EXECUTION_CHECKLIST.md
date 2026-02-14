# ? MASTER EXECUTION CHECKLIST
## Mbarie Insight Suite — Complete Implementation Roadmap

**Status:** Ready to Execute  
**Timeline:** 4-6 weeks  
**Target:** Production v1.0 (Windows 11 + macOS)  
**Platforms:** 20 Windows 11 + 20 macOS (Intel + Apple Silicon)

---

## ?? PRE-EXECUTION PHASE (This Week)

### ? CERTIFICATES & SIGNING (CRITICAL - DO NOW)

- [ ] **Windows Certificate**
  - [ ] Run: `create-windows-cert.ps1`
  - [ ] Copy `MIC-CodeSign.pfx` to `src/MIC/certs/windows/`
  - [ ] Copy `MIC-CodeSign.cer` to `src/MIC/certs/windows/`
  - [ ] Record thumbprint: `_________________________________`
  - [ ] Test certificate import: `Import-Certificate -FilePath ...`

- [ ] **macOS Certificate**
  - [ ] Run: `create-macos-cert.sh` on macOS machine
  - [ ] Copy `MIC-CodeSign.p12` to `src/MIC/certs/macos/`
  - [ ] Copy `MIC-CodeSign.cer` to `src/MIC/certs/macos/`
  - [ ] Record signing identity: `_________________________________`
  - [ ] Verify keychain access

- [ ] **Project Configuration**
  - [ ] Copy `build-config.json` ? `build-config.local.json`
  - [ ] Update with YOUR certificate paths
  - [ ] Update with YOUR certificate passwords
  - [ ] Add to `.gitignore`
  - [ ] Verify `.gitignore` includes: `*.pfx`, `*.p12`, `*.key`, `build-config.local.json`

- [ ] **Build Scripts**
  - [ ] Copy Windows build script to `scripts/build-windows-msix.ps1`
  - [ ] Copy macOS build script to `scripts/build-macos-dmg.sh`
  - [ ] Make macOS script executable: `chmod +x build-macos-dmg.sh`
  - [ ] Test on Windows: `.\scripts\build-windows-msix.ps1` (should complete)
  - [ ] Test on macOS: `./scripts/build-macos-dmg.sh` (should complete)

---

### ? NuGET PACKAGES (CROSS-PLATFORM)

```powershell
# Run these commands on Windows (or macOS with Mono)
dotnet add MIC.Infrastructure.Data package SixLabors.ImageSharp
dotnet add MIC.Infrastructure.Data package PdfPig
dotnet add MIC.Infrastructure.Data package DocumentFormat.OpenXml
dotnet add MIC.Infrastructure.Data package QuestPDF
dotnet add MIC.Infrastructure.Data package ClosedXML
```

- [ ] SixLabors.ImageSharp added (avatar upload)
- [ ] PdfPig added (PDF extraction)
- [ ] DocumentFormat.OpenXml added (Word extraction)
- [ ] QuestPDF added (PDF reports)
- [ ] ClosedXML added (Excel reports)
- [ ] Verify on both platforms: `dotnet restore` succeeds

---

### ? CI/CD SETUP (GITHUB ACTIONS)

- [ ] Create `.github/workflows/cross-platform-build.yml`
- [ ] Configure to build on: `windows-11`, `macos-latest`
- [ ] Test on main branch
- [ ] Verify both platform builds pass

---

### ? DOCUMENTATION CREATED

- [ ] ? `CROSS_PLATFORM_STRATEGY.md` (created)
- [ ] ? `CERTIFICATE_GENERATION_GUIDE.md` (created)
- [ ] ? `copilot_master_prompt.md` (created)
- [ ] ? `IMPLEMENTATION_STRATEGY.md` (created)
- [ ] Update `README.md` with platform support info
- [ ] Create `INSTALLATION_GUIDE.md` (platform-specific)

---

## ?? WEEK 1: EMAIL MODULE + CROSS-PLATFORM BASELINE

### Days 1-3: Email Send/Compose (Platform-Agnostic)

**Files to Create:**

- [ ] `MIC.Core.Application/Features/Email/Commands/SendEmail/SendEmailCommand.cs`
  - [ ] Record, To/Cc/Bcc, Subject, Body, Attachments
  - [ ] Implement IRequest<Result<string>>

- [ ] `MIC.Core.Application/Features/Email/Commands/SendEmail/SendEmailCommandValidator.cs`
  - [ ] Validate recipients not empty
  - [ ] Validate email addresses valid
  - [ ] Validate subject not empty
  - [ ] Validate body not empty

- [ ] `MIC.Core.Application/Features/Email/Commands/SendEmail/SendEmailCommandHandler.cs`
  - [ ] Resolve email provider (Gmail vs Outlook) from AccountId
  - [ ] Call provider's SendAsync()
  - [ ] Save copy to SentItems folder
  - [ ] Log with Serilog
  - [ ] Handle errors with IErrorHandlingService
  - [ ] Return Result<string> with message ID

- [ ] `MIC.Desktop.Avalonia/ViewModels/EmailComposeViewModel.cs`
  - [ ] ObservableCollection<string> ToAddresses
  - [ ] ObservableCollection<string> CcAddresses
  - [ ] ObservableCollection<string> BccAddresses
  - [ ] string Subject property
  - [ ] string Body property
  - [ ] bool IsSending property
  - [ ] ICommand SendCommand
  - [ ] ICommand DiscardCommand
  - [ ] ICommand AddAiAssistCommand

- [ ] `MIC.Desktop.Avalonia/Views/EmailComposeView.xaml`
  - [ ] To/Cc/Bcc fields (tag input style)
  - [ ] Subject field
  - [ ] Body TextBox (monospace, dark theme)
  - [ ] Toolbar: [Send] [AI Draft ?] [Attach] [Discard]
  - [ ] Character count indicator
  - [ ] Loading spinner while sending

- [ ] `MIC.Desktop.Avalonia/Views/EmailComposeView.xaml.cs`
  - [ ] Code-behind for view

**Tests to Create:**

- [ ] `MIC.Tests.Unit/Features/Email/SendEmailCommandValidatorTests.cs`
  - [ ] Test empty recipients
  - [ ] Test invalid email address
  - [ ] Test empty subject
  - [ ] Test empty body
  - [ ] Test valid command passes

- [ ] `MIC.Tests.Unit/Features/Email/SendEmailCommandHandlerTests.cs`
  - [ ] Test successful send
  - [ ] Test provider resolution
  - [ ] Test error handling
  - [ ] Test logging

**Platform Testing (Critical):**

- [ ] **Windows 11**
  - [ ] `dotnet build MIC.slnx` succeeds
  - [ ] `dotnet test MIC.Tests.Unit` all pass
  - [ ] `dotnet run --project .\MIC.Desktop.Avalonia` launches
  - [ ] Send email manually through UI

- [ ] **macOS Intel**
  - [ ] `dotnet build MIC.slnx` succeeds
  - [ ] `dotnet test MIC.Tests.Unit` all pass
  - [ ] `dotnet run --project MIC.Desktop.Avalonia` launches
  - [ ] Send email manually through UI

- [ ] **macOS Apple Silicon**
  - [ ] Same as Intel (binary handles both)

**Deliverable:**
- [ ] Email send works identically on Windows 11, macOS Intel, macOS M1/M2
- [ ] All tests pass on all platforms
- [ ] No platform-specific code needed

---

### Days 3-5: Email Reply/Forward + Cross-Platform CI/CD

**Files to Create/Extend:**

- [ ] `MIC.Core.Application/Features/Email/Commands/ReplyEmail/ReplyEmailCommand.cs`
- [ ] `MIC.Core.Application/Features/Email/Commands/ReplyEmail/ReplyEmailCommandHandler.cs`
- [ ] `MIC.Core.Application/Features/Email/Commands/ForwardEmail/ForwardEmailCommand.cs`
- [ ] `MIC.Core.Application/Features/Email/Commands/ForwardEmail/ForwardEmailCommandHandler.cs`

- [ ] Extend `EmailComposeViewModel` with:
  - [ ] ReplyCommand
  - [ ] ReplyAllCommand
  - [ ] ForwardCommand

- [ ] Extend `EmailDetailViewModel` with:
  - [ ] Reply, Reply All, Forward buttons
  - [ ] Open compose dialog pre-populated

**Tests:**

- [ ] `MIC.Tests.Unit/Features/Email/ReplyEmailCommandTests.cs`
- [ ] `MIC.Tests.Unit/Features/Email/ForwardEmailCommandTests.cs`

**CI/CD:**

- [ ] Create `.github/workflows/cross-platform-build.yml`
- [ ] Configure matrix: Windows 11, macOS-latest
- [ ] Push to `develop` branch
- [ ] Verify both build jobs pass
- [ ] Check: "Build Status: All platforms ?"

**Deliverable:**
- [ ] Email reply/forward works on all platforms
- [ ] CI/CD pipeline reports success on both Windows & macOS

---

## ?? WEEK 2-4: COMPLETE ALL 5 MODULES (PLATFORM-AGNOSTIC)

**For each module below:**
1. Create all files
2. Run tests locally (Windows + macOS)
3. Push to develop
4. Wait for CI/CD to pass both platforms
5. Merge to main

### EMAIL MODULE - COMPLETE (Days 1-7)

- [ ] Delete emails (soft-delete to Trash)
- [ ] Move emails to folders (Archive, custom)
- [ ] Mark as read/unread
- [ ] Tests for all operations
- [ ] **Test on Windows 11 + macOS**

---

### USER PROFILE - COMPLETE (Days 8-14)

**Files:**

- [ ] `MIC.Core.Application/Features/UserProfile/Commands/ChangePassword/ChangePasswordCommand.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/ChangePassword/ChangePasswordCommandValidator.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/ChangePassword/ChangePasswordCommandHandler.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/UpdateAvatar/UpdateAvatarCommand.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/UpdateAvatar/UpdateAvatarCommandValidator.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/UpdateAvatar/UpdateAvatarCommandHandler.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/UpdatePreferences/UpdateUserPreferencesCommand.cs`
- [ ] `MIC.Core.Application/Features/UserProfile/Commands/UpdatePreferences/UpdateUserPreferencesCommandHandler.cs`

**UI:**

- [ ] Add "Change Password" tab to `UserProfileView.xaml`
- [ ] Add "Avatar" tab
- [ ] Add "Preferences" tab (notifications, theme, timezone)

**Tests:**

- [ ] Password validation tests
- [ ] Avatar upload tests
- [ ] Preferences save/load tests

**Deliverable:**
- [ ] Full user profile management on both platforms

---

### KNOWLEDGE BASE - COMPLETE (Days 15-21)

**Domain:**

- [ ] `KnowledgeDocument` entity
- [ ] `DocumentChunk` entity
- [ ] `IKnowledgeDocumentRepository`
- [ ] `IDocumentChunkRepository`

**Commands:**

- [ ] `UploadDocumentCommand`
- [ ] `DeleteDocumentCommand`
- [ ] `TagDocumentCommand`

**Queries:**

- [ ] `SearchKnowledgeBaseQuery`
- [ ] `GetDocumentsQuery`

**Services:**

- [ ] `IDocumentTextExtractorService` (PDF, DOCX, TXT, CSV)
- [ ] Extract text with PdfPig, DocumentFormat.OpenXml

**Tests:**

- [ ] Upload tests
- [ ] Search tests
- [ ] Extraction tests

**Deliverable:**
- [ ] Document upload and semantic search on both platforms

---

### PREDICTIONS - COMPLETE (Days 22-28)

**Algorithm:**

- [ ] `IPredictionService` interface
- [ ] Linear regression (trend detection)
- [ ] Seasonality detection (7-day patterns)
- [ ] Anomaly threshold (? + 2?)
- [ ] Confidence intervals

**UI:**

- [ ] Metric selector
- [ ] Date range picker
- [ ] Forecast chart (historical + predicted + confidence band)
- [ ] Insight cards (trend, forecast, confidence score)

**Tests:**

- [ ] Prediction algorithm tests
- [ ] Edge case tests (zero values, single data point)

**Deliverable:**
- [ ] Real predictive analytics on both platforms

---

### REPORTS - COMPLETE (Days 29-35)

**Domain:**

- [ ] `Report` entity
- [ ] `ReportType` enum
- [ ] `IReportRepository`

**Commands:**

- [ ] `GenerateReportCommand`
- [ ] Report template implementations (Alert Summary, Email Activity, Metrics Trend)

**NuGet:**

- [ ] Use QuestPDF (PDF), ClosedXML (Excel), CsvHelper (CSV)

**UI:**

- [ ] Report type selector
- [ ] Date range picker
- [ ] Format selector (PDF, XLSX, CSV)
- [ ] Progress indicator
- [ ] Recent reports list

**Tests:**

- [ ] Report generation tests
- [ ] Format export tests

**Deliverable:**
- [ ] Full report generation on both platforms

---

## ?? WEEK 5: PACKAGING FOR BOTH PLATFORMS

### Days 1-2: Windows MSIX Signing

- [ ] Execute: `.\scripts\build-windows-msix.ps1`
- [ ] Verify MSIX created in output directory
- [ ] Verify signature with signtool.exe
- [ ] Test installation on clean Windows 11 machine
  - [ ] Install certificate
  - [ ] Double-click MSIX
  - [ ] App launches
  - [ ] First-run setup works
  - [ ] All features functional

**Deliverable:**
- [ ] Signed, installable MSIX for Windows 11

---

### Days 3-4: macOS DMG Creation

- [ ] Execute: `./scripts/build-macos-dmg.sh`
- [ ] Verify universal binary created (Intel + M1/M2)
- [ ] Verify codesign succeeds
- [ ] Test DMG on macOS Intel machine
  - [ ] Mount DMG
  - [ ] Drag to Applications
  - [ ] App launches
  - [ ] All features functional
- [ ] Test DMG on macOS M1/M2 machine
  - [ ] Mount DMG
  - [ ] Drag to Applications
  - [ ] App launches (via Rosetta if needed, or native)
  - [ ] All features functional

**Deliverable:**
- [ ] Signed, installable DMG for macOS (universal binary)

---

### Day 5: Create Installer Scripts & Documentation

- [ ] `install-windows.ps1` (with certificate installation)
- [ ] `install-macos.sh` (with app drag-to-Applications)
- [ ] `INSTALLATION_GUIDE.md` (step-by-step for both platforms)
- [ ] `SYSTEM_REQUIREMENTS.md` (OS versions, .NET runtime)
- [ ] Release notes v1.0.0

**Deliverable:**
- [ ] Professional installer experience for both platforms

---

## ?? WEEK 6: REAL-WORLD TESTING (20 WINDOWS + 20 MAC)

### Pre-Testing Preparation

- [ ] Set up test coordination email list (40 testers)
- [ ] Create feedback form (Google Forms/Typeform)
- [ ] Prepare installation instructions
- [ ] Prepare system requirements checklist

### Windows 11 Testing (20 Machines)

- [ ] Distribute MSIX + MIC-CodeSign.cer + install-windows.ps1
- [ ] Each tester follows `INSTALLATION_GUIDE.md`
- [ ] Tester completes checklist:
  - [ ] Installation successful
  - [ ] First-run setup works
  - [ ] User registration works
  - [ ] All 5 features functional
  - [ ] Performance acceptable
  - [ ] No crashes
  - [ ] Uninstall clean

- [ ] Collect feedback via form
- [ ] **Expected outcome:** 100% installation success, identify any bugs

### macOS Testing (20 Machines)

- [ ] 10 Intel machines
- [ ] 10 Apple Silicon machines
- [ ] Distribute DMG + install-macos.sh
- [ ] Each tester follows `INSTALLATION_GUIDE.md`
- [ ] Same checklist as Windows
- [ ] **Critical:** Test both architectures

### Feedback Analysis

- [ ] Aggregate feedback
- [ ] Identify critical bugs
- [ ] Identify performance issues
- [ ] Categorize as:
  - [ ] Critical (blocker)
  - [ ] Major (should fix)
  - [ ] Minor (nice to have)

### Bug Fixes (if needed)

- [ ] Fix critical bugs
- [ ] Re-test on both platforms
- [ ] Re-distribute fixed build

**Deliverable:**
- [ ] Tested on 40 real machines (Windows + macOS)
- [ ] Zero critical bugs
- [ ] User feedback incorporated

---

## ?? WEEK 7: RELEASE v1.0.0

### GitHub Release

- [ ] Create GitHub Release v1.0.0
- [ ] Upload artifacts:
  - [ ] `MIC-v1.0.0-Windows11-x64.msix`
  - [ ] `MIC-v1.0.0-macOS-Universal.dmg`
  - [ ] `MIC-CodeSign.cer` (for Windows users)
  - [ ] `INSTALLATION_GUIDE.md`
  - [ ] `SYSTEM_REQUIREMENTS.md`
  - [ ] `RELEASE_NOTES.md`

### Distribution Website (Simple)

- [ ] Create landing page with:
  - [ ] Download buttons (Windows + macOS)
  - [ ] System requirements
  - [ ] Installation instructions
  - [ ] Support email
  - [ ] Feature list

### Documentation

- [ ] Update `README.md` with cross-platform support
- [ ] Finalize `USER_GUIDE.md`
- [ ] Finalize `ADMIN_GUIDE.md`
- [ ] Create troubleshooting section

### Public Announcement

- [ ] Email to all 40 testers: "v1.0.0 released!"
- [ ] Announcement (blog, social media, etc.)
- [ ] Thank you message to testers

**Deliverable:**
- [ ] Production v1.0.0 released on both Windows 11 and macOS
- [ ] Ready for installation on 40 machines (and beyond)

---

## ?? QUALITY GATES CHECKLIST

Before releasing each module, verify:

### Code Quality
- [ ] All new tests written (happy path + edge cases)
- [ ] All tests pass on Windows 11
- [ ] All tests pass on macOS
- [ ] Code coverage > 70% for new code
- [ ] No hardcoded secrets
- [ ] Serilog logging in all handlers
- [ ] Error handling with IErrorHandlingService
- [ ] Notifications via INotificationService

### Platform Compatibility
- [ ] Build succeeds on Windows 11
- [ ] Build succeeds on macOS Intel
- [ ] Build succeeds on macOS M1/M2 (via cross-compilation or native)
- [ ] No platform-specific code (unless necessary)
- [ ] Cross-platform CI/CD passes

### Architecture
- [ ] Domain layer (no external dependencies)
- [ ] Application layer (CQRS commands/queries)
- [ ] Repository pattern (no DbContext in handlers)
- [ ] DI registration (all new services)
- [ ] DTOs (no domain entities exposed)

### UI/UX
- [ ] Uses BrandColors (no hardcoded hex values)
- [ ] Consistent with existing UI
- [ ] Keyboard navigation works
- [ ] Loading indicators for async operations
- [ ] Error messages user-friendly

---

## ?? CRITICAL SUCCESS FACTORS

1. **Generate certificates NOW** — Don't delay
2. **Test on real hardware** — Virtual machines miss issues
3. **Both platforms from day 1** — Don't build Windows-only then port
4. **CI/CD automated** — Every commit tested on both platforms
5. **No platform-specific code** — Keep .NET/Avalonia abstractions clean
6. **Extensive testing** — 40 testers is excellent
7. **Feedback loop** — Incorporate feedback quickly

---

## ?? TROUBLESHOOTING

**If Windows build fails:**
```powershell
dotnet clean MIC.slnx
dotnet restore MIC.slnx
dotnet build MIC.slnx --configuration Release --runtime win-x64 -v diagnostic
```

**If macOS build fails:**
```bash
dotnet clean MIC.slnx
dotnet restore MIC.slnx
dotnet build MIC.slnx --configuration Release --runtime osx-universal -v diagnostic
```

**If certificate fails:**
- [ ] Verify certificate path exists
- [ ] Verify certificate password correct
- [ ] Verify certificate valid (not expired)
- [ ] Regenerate if needed

**If tests fail on one platform:**
- [ ] Run locally on that platform
- [ ] Check for platform-specific code
- [ ] Add platform guards if needed

---

## ? FINAL SIGN-OFF

**When entire checklist complete, sign here:**

```
Developer: ________________________  Date: __________

Tech Lead: ________________________  Date: __________

QA Lead: __________________________  Date: __________

Product Manager: __________________  Date: __________
```

---

**Ready to execute?**

Start with **PRE-EXECUTION PHASE** (this week):
1. Generate certificates (Windows + macOS)
2. Add NuGet packages
3. Create build scripts
4. Set up CI/CD

Then **WEEK 1: Begin Email Module** with cross-platform testing from day 1!

?? **Let's build a professional, cross-platform product!**

