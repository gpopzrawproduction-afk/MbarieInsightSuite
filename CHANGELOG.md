# Changelog

## 2026-02-06
- Added System.Security.Cryptography.ProtectedData dependency and ensured DPAPI token protection compiles on Windows.
- Refined Outlook and Gmail OAuth services to use silent refresh, interactive fallback, and token storage persistence.
- Expanded unit coverage with OutlookOAuthServiceTests, GmailOAuthServiceTests, and shared FakeTokenStorageService.
