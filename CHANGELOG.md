# Changelog

## 2026-02-09
- Fixed unit test failures in EmailInboxViewModelAdditionalTests by correcting mediator mock return type from `List<EmailDto>` to `ErrorOr<IReadOnlyList<EmailDto>>`.
- All unit tests now pass (372 total, 346 passed, 26 skipped).
- Integration tests pass (3 LoginIntegrationTests).
- Build succeeds with no warnings.
- Code review and cleanup completed.

## 2026-02-06
- Added System.Security.Cryptography.ProtectedData dependency and ensured DPAPI token protection compiles on Windows.
- Refined Outlook and Gmail OAuth services to use silent refresh, interactive fallback, and token storage persistence.
- Expanded unit coverage with OutlookOAuthServiceTests, GmailOAuthServiceTests, and shared FakeTokenStorageService.
