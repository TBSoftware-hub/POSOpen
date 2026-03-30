# Deferred Work Log

Items deferred during code review — logged for future consideration but not blocking story completion.

---

## Deferred from: code review of 1-5-immutable-audit-trail-for-security-critical-actions

- **`SecurityAuditViewModel` outer catch hardcodes error message** — the `catch` block sets `ErrorMessage` using a string literal that is identical to `ListSecurityAuditTrailConstants.SafeAuditTrailUnavailableMessage`. No user-facing impact, but bypasses the constant. Deferred — cosmetic inconsistency only. [POSOpen/Features/Security/ViewModels/SecurityAuditViewModel.cs]
- **No guard against concurrent `LoadAsync` calls in `SecurityAuditViewModel`** — `OnAppearing` triggers a load without cancellation or mutex, so rapid navigation back/forth could queue multiple in-flight reads. No data-integrity risk. Deferred — minor UX edge case. [POSOpen/Features/Security/ViewModels/SecurityAuditViewModel.cs]
