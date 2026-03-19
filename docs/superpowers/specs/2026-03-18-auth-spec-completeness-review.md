# Authentication Architecture Spec -- Completeness Review

**Date:** 2026-03-18
**Reviewed document:** `docs/superpowers/specs/2026-03-18-authentication-architecture-design.md`
**Cross-referenced:** `docs/superpowers/specs/2026-03-16-dynamic-client-registration-design.md`
**Reviewer role:** Developer experience architect

---

## Summary

The auth architecture spec is thorough and well-structured. It covers three auth paths clearly, explains the middleware pipeline in detail, and provides a useful fork guide. However, there are factual inaccuracies, gaps that would block a developer from integrating without reading source code, and several flows that are either missing or only hinted at. This review catalogs those issues by category and priority.

---

## Findings

### Legend

| Category | Meaning |
|----------|---------|
| **INACCURACY** | Statement in the spec that is factually wrong per the codebase |
| **MISSING** | Information a developer needs that is not present |
| **INCONSISTENCY** | Contradiction between this spec and the DCR spec, or internal contradiction |
| **ENHANCEMENT** | Would meaningfully improve developer experience if added |
| **QUESTION** | Ambiguity that needs a decision, not just documentation |

| Priority | Meaning |
|----------|---------|
| **P0** | Will cause integration failure or security misunderstanding |
| **P1** | Will waste significant developer time or cause confusion |
| **P2** | Nice to have, would improve the doc but not block anyone |

---

### 1. INACCURACY / P0 -- "No AllowAnonymous" claim is false

**Section 9 states:** "There is no `[AllowAnonymous]` attribute on any Foundry endpoint (except the root health/info endpoint which returns no data)."

**Reality:** `ShowcasesController.cs` has `[AllowAnonymous]` on both `GetAll` and `GetById` endpoints. This directly contradicts the "no anonymous access" guarantee that the entire spec is built around.

**Impact:** A developer reading the spec will believe every request requires authentication. When they discover showcases work without a token, they will question the reliability of the rest of the spec. Worse, the security model section uses the "no anonymous access" claim to justify why even public content requires a service account -- but it does not actually require one for showcases.

**Fix:** Either remove `[AllowAnonymous]` from those endpoints (enforcing the spec's stated guarantee) or update Section 9 to accurately describe which endpoints are anonymous and why. If anonymous showcase access is intentional, explain the design rationale and list the complete set of anonymous endpoints.

---

### 2. MISSING / P0 -- Error response format specification

The spec mentions RFC 7807 Problem Details exactly once (API key validation, Section 5) but does not define the error response contract for auth failures across all three paths. A developer integrating with Foundry needs to know:

- What is the response body shape for 401 from JWT validation? (ASP.NET default `WWW-Authenticate` challenge? Problem Details? Empty body?)
- What is the response body for 403 from `PermissionAuthorizationHandler`?
- What is the response body for 429 from each rate limiting layer?
- Does the API consistently use RFC 7807 Problem Details for all error responses, or only some?

**What a developer would encounter without this info:** They would get a 401, try to parse the body, find it is either empty or in an unexpected format, and have to read ASP.NET Core default behavior docs plus the Foundry `GlobalExceptionHandler` source code to understand the contract.

**Recommendation:** Add a section (or a table in Section 2) that specifies the exact HTTP status code and response body format for each failure mode in the pipeline:

| Failure | Status | Response Body | Headers |
|---------|--------|---------------|---------|
| No auth header and no API key | 401 | `WWW-Authenticate: Bearer` (empty body) | |
| Invalid API key format | 401 | Problem Details: `"Invalid API key format"` | |
| Expired API key | 401 | Problem Details: `"API key expired"` | |
| Valid auth but missing permission | 403 | Problem Details: `"Insufficient permissions"` | |
| Rate limit exceeded (global) | 429 | ? | `Retry-After` header? |
| Rate limit exceeded (module) | 429 | ? | |
| Invalid JWT (expired, bad sig) | 401 | ? | |
| Valid JWT but no tenant org claim | ? | ? | |

---

### 3. MISSING / P1 -- No curl examples for the complete happy path

The DCR spec (older doc) has curl examples for registration. The auth architecture spec has ASCII flow diagrams but no copy-paste-ready curl commands for the most common integration path. A developer should be able to follow a single sequence of curl commands from zero to a successful API call.

**Recommendation:** Add an appendix or inline examples:

```
# 1. Register a client (dev mode, no auth)
curl -X POST http://localhost:8080/realms/foundry/clients-registrations/openid-connect \
  -H 'Content-Type: application/json' \
  -d '{"client_id":"sa-my-app","grant_types":["client_credentials"],"token_endpoint_auth_method":"client_secret_basic"}'

# 2. Get an access token
curl -X POST http://localhost:8080/realms/foundry/protocol/openid-connect/token \
  -d 'grant_type=client_credentials&client_id=sa-my-app&client_secret=SECRET_FROM_STEP_1'

# 3. Call the API
curl http://localhost:5000/api/v1/showcases \
  -H 'Authorization: Bearer TOKEN_FROM_STEP_2'
```

Also include a curl example for API key usage (`X-Api-Key` header) and for the OIDC token exchange.

---

### 4. INCONSISTENCY / P1 -- DCR spec vs auth spec: scope assignment narrative

**DCR spec (Section "Scope Assignment") says:** "Scopes like `inquiries.write`, `showcases.read` are NOT realm defaults. They are optional client scopes that must be assigned per-client by an admin after registration."

**Auth spec (Section 3, Mode B) says:** Keycloak "Client Scope Policy" whitelists which scopes DCR clients can request, implying developers can request scopes during registration.

These describe two different models:
1. DCR spec: all clients start with zero scopes, admin assigns them post-registration
2. Auth spec: policy controls which scopes are available, suggesting clients might request scopes during registration

**Question:** Can a DCR client request scopes in the registration payload, or are scopes always assigned post-registration by an admin? If the Client Scope Policy whitelists scopes, does that mean DCR clients can include a `scope` field in their registration request and get those scopes automatically?

**Fix:** Align both documents on the exact mechanism. If both flows are valid (admin-assigned for operator apps, policy-whitelisted for developer apps), say so explicitly.

---

### 5. INCONSISTENCY / P1 -- DCR spec vs auth spec: TenantId.Platform sentinel

**DCR spec says:** `TenantId.Platform => new(Guid.Parse("00000000-0000-0000-0000-000000000001"))` and mentions "EF Core global tenant query filter in `TenantAwareDbContext` must be updated to include `TenantId.Platform` records when the caller is a platform admin."

**Auth spec says:** Lazy metadata sync uses `TenantId.Platform` sentinel and notes "The existing `IServiceAccountUnfilteredRepository` (used by `ServiceAccountTrackingMiddleware`) already bypasses the tenant filter, so lazy sync will work without filter changes."

Wait -- the DCR spec says the filter must be updated; the auth spec references the DCR spec but the DCR spec itself says filter changes are needed. The auth spec's middleware description (Section 2, step 10) does not mention any filter update requirement. A developer implementing this will be confused about whether they need to update the tenant filter or not.

**Fix:** Clarify in both documents: lazy sync works via `IServiceAccountUnfilteredRepository` (no filter change needed). Admin querying of platform-scoped service accounts via normal endpoints does need the filter update. Make the distinction explicit.

---

### 6. MISSING / P1 -- Lightweight user creation flow (inquiry submission auto-creates user)

The spec describes how a BFF submits inquiries on behalf of anonymous visitors using its service account (Section 9: "A visitor browsing your site -> the BFF's service account authenticates on their behalf"). But it does not address:

- What happens to the visitor's identity? The inquiry presumably contains an email address. Does Foundry create a Keycloak user for that email?
- If the visitor later signs up with the same email, is their inquiry linked to their new account?
- What claims does the inquiry carry? Just the service account's identity, or also the visitor's email as metadata?

This is a common pattern discussed in Foundry's issue tracker (found references to "lightweight user creation" in the codebase). Without documenting this, a BFF developer will not know:
- Whether to pass the visitor's email as a request body field or as a header
- Whether Foundry will auto-provision a user
- How to design their signup flow to link pre-existing inquiries

**Recommendation:** Add a subsection under "Auth Path 1" or a new Section 6.5 titled "Pre-Authentication Actions" that explains:
1. The BFF authenticates as its service account
2. The visitor's email is passed as request body data (not auth context)
3. Whether auto-provisioning exists or is planned
4. How account linking works when the visitor later registers

If this flow is not yet implemented, state that explicitly so developers know not to depend on it.

---

### 7. MISSING / P1 -- Mobile OIDC flow (authorization_code + PKCE from native app)

The spec mentions mobile backends use DCR (Section 3: "Your mobile backend") but never describes the complete mobile authentication flow. Mobile apps have unique requirements:

- Native apps use system browsers or ASWebAuthenticationSession/Custom Tabs for the OAuth redirect
- The redirect URI is a custom scheme (`com.example.app://callback`) or universal link, not an HTTP URL
- Token storage on mobile is in the Keychain (iOS) or EncryptedSharedPreferences (Android), not cookies
- The mobile app itself is a public client (cannot hold a client_secret) -- it needs its own Keycloak client separate from the BFF's confidential client
- Refresh token rotation and offline_access scope behavior differs on mobile

The spec conflates the mobile backend (server-side, confidential client, DCR service account) with the mobile app itself (client-side, public client, user sessions). A mobile developer reading this spec would not know:
- Do I register one Keycloak client or two? (Answer: typically two -- one public client for the app's OIDC flow, one confidential client for the backend)
- Which client does the user authenticate through?
- How does the mobile app's user JWT reach the Foundry API -- directly or via the mobile backend?

**Recommendation:** Add a subsection under Section 4 or Section 6 that covers the mobile-specific flow:
1. Mobile app registers a public client in Keycloak (authorization_code + PKCE, custom redirect URI)
2. Mobile backend registers a confidential client via DCR (client_credentials)
3. User authenticates through the mobile app's public client
4. Mobile app sends user JWT to its own backend
5. Backend forwards user JWT to Foundry API (or acts on user's behalf via its service account + X-Tenant-Id)

---

### 8. MISSING / P1 -- Password reset, email verification, account linking, social login, MFA

The spec's OIDC section (Section 4) says "User authenticates (email/password, social login, SSO federation)" in one line of the flow diagram but never elaborates on any of these. A developer needs to know:

| Flow | Developer question | Current spec coverage |
|------|-------------------|----------------------|
| **Password reset** | How does my app trigger/handle it? Does Keycloak handle the entire flow? | Not mentioned |
| **Email verification** | Is it required? When is it enforced? Can the user access the API before verification? | Mentioned once in production checklist ("Enable email verification") but not explained |
| **Social login** | Which providers are pre-configured? How do I add one? Does it affect claims or tenant assignment? | Not mentioned |
| **Account linking** | If a user signs up with email, then later with Google, are they linked? | Not mentioned |
| **MFA/2FA** | Is it supported? How does it affect token issuance? Is it per-tenant configurable? | Not mentioned |

These are all Keycloak-handled flows, so the spec can be brief -- but it should at least state: "These flows are handled entirely by Keycloak. Foundry receives the same JWT regardless of how the user authenticated. See Keycloak documentation for configuring [each flow]. The `amr` claim in the JWT indicates the authentication method used."

**Recommendation:** Add a "Keycloak-Managed Authentication Flows" subsection to Section 4 that covers these at the "what you need to know" level, not implementation detail.

---

### 9. MISSING / P2 -- Keycloak version-specific behaviors

Section 4 correctly notes the Keycloak 26+ JSON format for organization claims: `{"orgId": {"name": "orgName"}}`. Other version-specific behaviors that should be noted:

- **Keycloak 25 vs 26 organization model**: Keycloak 26 introduced the Organizations feature as GA. The spec assumes organizations are used for multi-tenancy but does not state the minimum required Keycloak version. A fork operator deploying on Keycloak 24 or 25 would have a broken tenant resolution.
- **DCR policy component model**: The DCR spec already notes "The exact API paths and payload shapes should be verified against the installed Keycloak 26 version." The auth spec should state the minimum Keycloak version explicitly.
- **Token exchange**: If the mobile flow or BFF flow ever needs token exchange (RFC 8693), Keycloak's support for this has changed across versions.
- **FAPI compliance**: Keycloak 26 has improved FAPI (Financial-grade API) profile support, which affects client authentication methods and token binding.

**Recommendation:** Add a "Prerequisites" section at the top of the spec that states: "Requires Keycloak 26.0+ for organization support and DCR policy configuration."

---

### 10. INCONSISTENCY / P2 -- Duplicate content between DCR spec and auth spec

The auth spec's Section 3 substantially overlaps with the DCR spec:
- Registration flow diagrams (both specs have them)
- `sa-` prefix convention (explained in both)
- Lazy metadata sync (both)
- Scope assignment (both, with slightly different framing as noted in finding #4)
- Registration access token lifecycle (both)
- Implementation steps (DCR spec has 11 steps; auth spec references DCR spec but also inlines the same concepts)

**Risk:** When both documents describe the same thing, they will drift. The scope assignment inconsistency (finding #4) is already an example.

**Recommendation:** The auth spec should be the authoritative reference for the overall architecture. The DCR spec should be narrowed to implementation-specific details (Keycloak configuration, shell scripts, realm-export changes) and explicitly defer to the auth spec for architecture decisions. Add a note at the top of the DCR spec: "For the overall authentication architecture, see [auth-architecture-design.md]. This document covers DCR implementation details only."

---

### 11. MISSING / P2 -- Token caching guidance for service accounts

Section 3 shows the runtime flow where the app gets a token from Keycloak on "every API request." In practice, apps should cache the access token and reuse it until near-expiry. The spec does not mention:

- That Keycloak returns `expires_in` (e.g., 300 seconds) and the app should cache the token
- A recommended caching strategy (cache with a buffer, e.g., refresh 30 seconds before expiry)
- That `client_credentials` tokens are not tied to a user session and can be freely cached server-side
- That hitting Keycloak's token endpoint on every request is an anti-pattern that will degrade performance and potentially hit Keycloak rate limits

**Recommendation:** Add a "Token Caching" subsection to Section 3 with guidance. This is a common mistake for developers new to OAuth2.

---

### 12. MISSING / P2 -- CORS implications

The spec discusses three auth paths but does not mention CORS at all (except one line in the production checklist). A developer building a browser-based SPA that talks directly to the Foundry API needs to know:

- Does Foundry set `Access-Control-Allow-Origin` headers?
- Are `Authorization` and `X-Api-Key` headers allowed in preflight?
- Is the CORS policy configurable per-tenant or global?
- Does the `foundry-spa` client imply that SPAs talk directly to the API, or must they always go through a BFF?

**Recommendation:** Add a note in Section 4 about CORS configuration for SPAs, or explicitly state that SPAs must use the BFF pattern and never call the Foundry API directly from the browser.

---

### 13. ENHANCEMENT / P2 -- SDK / client library patterns

The spec is HTTP-level documentation. For developer experience, it would benefit from showing patterns for common client libraries:

- .NET: `HttpClient` with `DelegatingHandler` for automatic token refresh
- Node.js/TypeScript: `openid-client` library usage
- Python: `requests-oauthlib` or `httpx` with token management

Even a single example in one language would help. The DCR spec is closer to this level of detail (it shows app startup logic pseudocode), but a real code snippet would be more useful.

---

### 14. QUESTION / P1 -- What happens when a user belongs to multiple organizations?

The spec says TenantResolutionMiddleware reads the `organization` claim. The Keycloak 26+ JSON format is `{"orgId": {"name": "orgName"}}`. But:

- Can a user belong to multiple Keycloak organizations?
- If so, what does the `organization` claim contain? Multiple entries? Only the "default"?
- How does the user select which tenant context to operate in?
- Is there a mechanism like a `X-Tenant-Id` header for users (not just service accounts and admins)?

The spec says `X-Tenant-Id` override is allowed only for "realm admins and service accounts." If a user belongs to multiple orgs, they appear to be locked to whichever org Keycloak puts in the claim, with no way to switch.

**Recommendation:** Document multi-org user behavior explicitly. If Keycloak 26 only puts one org in the claim, say so. If users can be in multiple orgs, explain the selection mechanism.

---

### 15. QUESTION / P2 -- API key creation requires an active OIDC session

Section 5 states developers must "log in via OIDC session (Path 2)" to create API keys. This means:

- A developer cannot create an API key purely via API call with an existing API key
- There is no CLI-based flow for key creation without a browser
- Automated key rotation requires either a browser session or an admin API

Is this intentional? For a developer-friendly platform, a CLI flow like `foundry auth login` (device code grant) followed by `foundry keys create` would be more ergonomic.

**Recommendation:** At minimum, document this constraint explicitly. Consider mentioning whether device_code grant (RFC 8628) could be supported via Keycloak for CLI-based authentication.

---

### 16. ENHANCEMENT / P2 -- Troubleshooting section

The spec describes what happens when things go right. It would benefit from a "Troubleshooting" section covering the most common failure scenarios:

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| 401 on every request with a valid-looking JWT | Missing `aud: foundry-api` claim | Check that `foundry-api-audience` is in realm default scopes |
| 403 on every request with `sa-*` client | Client has no scopes assigned | Admin must assign scopes via Keycloak or Foundry API |
| 403 on every request without `sa-*` prefix | Client registered without `sa-` prefix | Re-register with `sa-` prefix |
| 401 with API key | Key expired, revoked, or malformed | Check key starts with `sk_live_`, check expiry |
| Tenant resolution fails | User not in a Keycloak organization | Assign user to an organization in Keycloak |
| Rate limited (429) unexpectedly | Sharing credentials across processes | Each process needs its own identity or key |

---

### 17. MISSING / P2 -- Webhook authentication (outbound)

The spec covers inbound authentication (requests TO Foundry) but does not mention outbound webhook authentication. Foundry has a `WebhooksManage` permission and presumably sends webhooks to external URLs. How are those requests authenticated?

- Does Foundry sign webhook payloads (HMAC-SHA256)?
- Is there a shared secret per webhook subscription?
- How does the receiving server verify the webhook is from Foundry?

This is adjacent to the auth architecture and could be a separate doc, but it should at least be mentioned in the "Out of Scope" or "Related Documents" section.

---

### 18. INCONSISTENCY / P2 -- "foundry-spa" is a pre-configured client, not DCR-registered

Section 4 uses `foundry-spa` as the client for OIDC user sessions. This is a public client (no secret). The spec never clarifies:

- `foundry-spa` is pre-configured in realm-export.json, not registered via DCR
- It is the ONLY pre-configured public client
- Third-party developers who want user login in their apps need to register their own public client via DCR (with `grant_types: ["authorization_code"]` instead of `["client_credentials"]`)

The DCR sections only discuss `client_credentials` grant type. A developer building a third-party app with user login would not know how to register a public client via DCR.

**Recommendation:** Add a note in Section 3 or Section 4 covering DCR registration for public clients (authorization_code + PKCE), including the required payload fields (`redirect_uris`, `grant_types: ["authorization_code"]`, `token_endpoint_auth_method: "none"`).

---

## Priority Summary

| Priority | Count | Impact |
|----------|-------|--------|
| P0 | 2 | Will cause integration failure or security misunderstanding |
| P1 | 6 | Will waste significant developer time |
| P2 | 10 | Improvements for completeness and polish |

### Recommended fix order

1. **P0 #1** -- Fix the AllowAnonymous inaccuracy (code or doc change, either way it needs resolving)
2. **P0 #2** -- Add error response format specification
3. **P1 #4** -- Align DCR spec and auth spec on scope assignment
4. **P1 #5** -- Clarify TenantId.Platform filter requirements
5. **P1 #6** -- Document lightweight user creation flow (or explicitly mark it as future work)
6. **P1 #7** -- Add mobile OIDC flow description
7. **P1 #8** -- Add Keycloak-managed auth flows section
8. **P1 #14** -- Document multi-org behavior
9. Everything else in P2 order listed above
