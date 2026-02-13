# ? QUICK REFERENCE CARD
## The 7 Steps - All Commands & What to Expect

---

## STEP 1: Using Statements ?
**Status:** DONE  
**What was done:** All using statements added to email command files  
**Your action:** NONE

---

## STEP 2: DI Registration ?
**Status:** DONE  
**What was done:** 5 email handlers registered in DependencyInjection.cs  
**Your action:** NONE

---

## STEP 3: Build Windows 11 ?

**Commands to run:**
```
cd C:\MbarieIntelligenceConsole\src\MIC
dotnet clean MIC.slnx
dotnet build MIC.slnx --configuration Debug
```

**What to expect:**
```
...Build succeeded. 0 warnings, 0 errors
```

**Copy/paste output to tell me**

---

## STEP 4: Test Windows 11 ?

**Command to run:**
```
dotnet test MIC.Tests.Unit --filter "Email" --logger:"console;verbosity=normal"
```

**What to expect:**
```
Test run for C:\...\MIC.Tests.Unit.csproj (net9.0)

Passed:  18  Failed:  0  Skipped:  0
Test execution time: 2.XX seconds
```

**Copy/paste output to tell me**

---

## STEP 5: Build & Test macOS ?

**Same as steps 3-4, but on your Mac:**
```
cd ~/path/to/MIC
dotnet clean MIC.slnx
dotnet build MIC.slnx --configuration Debug
dotnet test MIC.Tests.Unit --filter "Email" --logger:"console;verbosity=normal"
```

**Expected:** Same as Windows (Build succeeded + 18/18 tests pass)

**Copy/paste output to tell me**

---

## STEP 6: Commit to GitHub ?

**Commands to run:**
```
git add .
git commit -m "feat: Email send/reply/delete/move functionality (Week 1)"
git push origin develop
```

**What to expect:**
```
Counting objects: XX, done.
...
To https://github.com/gpopzrawproduction-afk/MbarieInsightSuite
   XXXXX..XXXXX  develop -> develop
```

**Copy/paste output to tell me**

---

## STEP 7: Verify CI/CD ?

**Manual verification:**
1. Go to: https://github.com/gpopzrawproduction-afk/MbarieInsightSuite/actions
2. Find your commit (should be at top)
3. Look for: ? Green checkmarks on both "Windows 11" and "macOS" workflows
4. Click on each to see details

**What to expect:**
```
? Windows 11 build succeeded
? macOS build succeeded  
? All tests passed
```

**Report back:** "CI/CD passed" or screenshot

---

## ?? STARTING NOW

**Step 1: Open PowerShell**

**Step 2: Run these three commands:**
```powershell
cd C:\MbarieIntelligenceConsole\src\MIC
dotnet clean MIC.slnx
dotnet build MIC.slnx --configuration Debug
```

**Step 3: Tell me the output** (copy/paste from terminal)

**Step 4: I'll help from there**

---

## ?? IF STUCK

**Build failed?**
- Copy the error message
- Tell me what it says
- I'll help fix it

**Tests failed?**
- Copy the error message  
- Tell me what it says
- I'll help debug

**Don't know next step?**
- Tell me which step you're on
- I'll guide you to the next one

---

## ? SUCCESS INDICATORS

| Step | Success Looks Like |
|------|-------------------|
| 3 | "Build succeeded" |
| 4 | "Passed: 18, Failed: 0" |
| 5 | Same on macOS |
| 6 | "develop -> develop" |
| 7 | ? Green checkmarks on GitHub |

---

## ?? GO NOW

```powershell
cd C:\MbarieIntelligenceConsole\src\MIC
dotnet clean MIC.slnx
dotnet build MIC.slnx --configuration Debug
```

**Report back when done!** ??

