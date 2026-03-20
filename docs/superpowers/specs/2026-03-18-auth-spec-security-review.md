# Security Review: Authentication & Authorization Architecture Spec

**Date:** 2026-03-18
**Reviewer:** Security Architecture Review
**Spec Under Review:** `docs/superpowers/specs/2026-03-18-authentication-architecture-design.md`
**Status:** Review Complete

---

## Summary

The authentication architecture is well-designed with strong fail-safe defaults and clear separation of concerns. The three-path model (DCR service accounts, OIDC user sessions, API keys) is sound. However, there are several security gaps that range from a critical implementation issue to lower-severity hardening opportunities. This review covers findings from both the spec and cross-referencing the actual implementation code.

---

## Findings

### FINDING-01: API Key Validation Vulnerable to Timing Attack

**Severity:** High

**Description:** The `RedisApiKeyService.ValidateApiKeyAsync` method computes `SHA256(apiKey)` and performs a Valkey key lookup (`GET apikey:<hash>`). The spec correctly notes that SCIM token validation uses `CryptographicOperations.FixedTimeEquals` for constant-time comparison. However, the API key path does not perform a constant-time comparison at any point. Instead, it relies on Valkey's `GET` returning null for a miss.

While the SHA256 hash acts as a layer of indirection (the attacker cannot observe character-by-character comparison timing), there is a subtler issue: Valkey `GET` on an existing key vs. a nonexistent key may return at different speeds depending on memory layout and hash table internals. An attacker who somehow obtains the hash prefix could theoretically use timing differences to narrow down valid hashes.

In practice, this is mitigated by SHA256's avalanche property (any change to the input completely changes the hash), which makes incremental guessing infeasible. The real risk is low, but the spec should explicitly document why constant-time comparison is unnecessary here (hash lookup, not string comparison), whereas it is necessary for SCIM tokens (prefix-based lookup followed by hash comparison).

**Recommendation:**
- Document in the spec that API key validation is timing-safe by design due to the hash-then-lookup pattern (no string comparison occurs).
- No code change needed, but the asymmetry with SCIM should be explained.

---

### FINDING-02: API Key Scope Subset Validation Not Implemented

**Severity:** Critical

**Description:** The spec explicitly calls this out as a "Required Implementation Gap" in Section 5. The `ApiKeysController.CreateApiKey` endpoint validates that requested scopes exist in `ApiScopes.ValidScopes` but does NOT validate that the scopes are a subset of the creating user's permissions.

This means a `user`-role account (9 permissions) can create an API key with `billing.manage`, `users.manage`, `scim.manage`, or any other scope in `ValidScopes`. The key itself will only grant permissions that the scopes map to, but those permissions may far exceed what the user's role grants. This is a **privilege escalation** vulnerability.

The spec proposes this fix but marks it as a future item. It should be treated as a blocker before any production deployment that enables API key self-service.

**Recommendation:**
- Implement scope subset validation immediately: at key creation, verify that every requested scope maps to a `PermissionType` that the creating user's role currently grants.
- Add a test case: `user` role attempts to create a key with `billing.manage` scope, expects 403.
- Consider also implementing lazy enforcement at validation time (check if key scopes still fall within the owner's current role) to handle role downgrades.

---

### FINDING-03: No Rate Limiting on DCR Endpoint

**Severity:** High

**Description:** The spec defines rate limits for global requests, auth endpoints, uploads, and SCIM. The DCR endpoint (`POST /realms/foundry/clients-registrations/openid-connect`) is hosted by Keycloak, not Foundry, so Foundry's rate limiter does not apply to it. The spec mentions a "Max Clients Policy" (default: 200 for anonymous, 10 per developer recommended) but this is a total count limit, not a rate limit.

An attacker with valid credentials (or in an open-ecosystem deployment with `create-client` auto-assigned) could rapidly register hundreds of clients, consuming Keycloak resources, polluting the client list, and potentially causing denial of service.

**Recommendation:**
- Add a Keycloak-level rate limit on the DCR endpoint. This can be done via a reverse proxy (nginx/envoy) rate limit on the `/realms/foundry/clients-registrations/` path.
- Alternatively, implement a Foundry-side DCR proxy endpoint that applies Foundry's rate limiting before forwarding to Keycloak.
- Add this to the Production Hardening Checklist in Section 10.

---

### FINDING-04: Service Account `sa-` Prefix Not Enforced at Registration

**Severity:** High

**Description:** The spec acknowledges that "Keycloak DCR has no built-in client_id prefix enforcement" and argues the convention is fail-safe because a missing prefix results in zero permissions. This is correct for the case where a developer accidentally omits the prefix.

However, the reverse is dangerous: a non-service-account client (e.g., an authorization_code client registered by an attacker) could register with an `sa-` prefix. This would cause `PermissionExpansionMiddleware` to treat it as a service account and use scope-based expansion instead of role-based expansion. If the attacker can also get scopes assigned (via Keycloak Client Scope Policy misconfiguration), they get scope-based permissions on what should be a user-facing client.

More importantly, a developer could register a client named `sa-foundry-api` or `sa-admin-tool` to impersonate operator-provisioned clients. While the `client_id` must be unique in Keycloak (so exact duplicates fail), similar names like `sa-foundry-api-2` or `sa-admin-t00l` could cause confusion in audit logs and admin dashboards.

**Recommendation:**
- Implement a Keycloak Client Registration Policy (via SPI or scripted policy) that enforces the `sa-` prefix on DCR-registered clients.
- Reserve a set of client_id patterns (e.g., `sa-foundry-*`, `sa-admin-*`) that cannot be registered via developer self-service DCR. Only Initial Access Token registrations should be able to use these patterns.
- Add `client_id` naming validation to the DCR configuration script (`configure-dcr.sh`).

---

### FINDING-05: X-Tenant-Id Header Override Allows Service Account Cross-Tenant Access

**Severity:** High

**Description:** The `TenantResolutionMiddleware` allows the `X-Tenant-Id` header to override the JWT-derived tenant for two principal types: realm admins and service accounts (`azp` starts with `sa-`). This means ANY service account can set `X-Tenant-Id` to ANY tenant GUID and operate in that tenant's context.

For operator-provisioned service accounts (trusted), this is a useful feature for multi-tenant operations. For developer-registered service accounts (untrusted), this is a cross-tenant access vulnerability. A third-party developer's app could set `X-Tenant-Id` to another organization's GUID and access their data, limited only by the scopes the app was granted.

The spec documents that lazily-created `ServiceAccountMetadata` uses `TenantId.Platform` sentinel, suggesting service accounts are intended to be tenant-agnostic. But granting all service accounts the ability to impersonate any tenant is a significant trust boundary violation.

**Recommendation:**
- Restrict `X-Tenant-Id` override for service accounts to only operator-provisioned clients. One approach: maintain a list of trusted client_id prefixes or check against `ServiceAccountMetadata.CreatedByUserId` (operator-created vs. developer-created).
- Alternatively, developer-registered service accounts should be bound to a specific tenant at creation time, with no override capability.
- At minimum, log and alert on `X-Tenant-Id` overrides from developer-registered service accounts.

---

### FINDING-06: Valkey (API Key Store) Durability on Restart

**Severity:** High

**Description:** API keys are stored exclusively in Valkey (Redis-compatible, in-memory by default). The spec mentions "Configure Valkey persistence (RDB or AOF) for API key durability" in the Production Hardening Checklist, but treats it as optional.

If Valkey restarts without persistence configured:
- All API keys are permanently lost. Developers cannot revoke them (the key data is gone) and the keys stop working (validation lookups fail). The raw keys were shown only once at creation and cannot be recovered.
- All rate limit counters are reset, allowing burst abuse immediately after a restart.
- The `apikeys:user:<userId>` sets are lost, so the List API Keys endpoint returns empty results.

This is not a theoretical concern -- Valkey defaults to no persistence, and a Docker container restart or OOM kill would wipe all data.

**Recommendation:**
- Elevate Valkey persistence from "production hardening" to a **deployment requirement**. The spec should state that Valkey MUST be configured with AOF (append-only file) persistence for API key data.
- Consider a hybrid storage model: store API key metadata in PostgreSQL (durable) and cache the hash-to-metadata lookup in Valkey (fast). On Valkey miss, fall back to PostgreSQL and re-populate the cache.
- At minimum, document the blast radius of a Valkey data loss event and the recovery procedure (all users must regenerate keys).

---

### FINDING-07: No Per-IP Rate Limiting Pre-Authentication

**Severity:** Medium

**Description:** The global rate limiter (Layer 1) uses authenticated identity for rate limiting (`api_key_id`, `azp`, or `sub`). Before authentication, there is no IP-based rate limiting. This means an attacker can send unlimited invalid authentication attempts (bad API keys, expired JWTs, malformed tokens) without being throttled, as long as they never successfully authenticate.

The "Auth" rate limit policy (3 req/10min) applies to login/token endpoints, but those are Keycloak endpoints, not Foundry endpoints. Foundry's own API endpoints have no pre-auth rate limiting.

An attacker could:
- Brute-force API key prefixes at high speed (trying `sk_live_AAAA...`, `sk_live_AAAB...`, etc.)
- Flood the API key validation path, causing excessive Valkey lookups
- Perform credential stuffing with stolen API keys against Foundry's endpoint

**Recommendation:**
- Add IP-based rate limiting in the rate limiter middleware that applies BEFORE authentication. A reasonable default: 100 failed auth attempts per IP per 15 minutes.
- Consider using ASP.NET Core's `FixedWindowRateLimiter` with `HttpContext.Connection.RemoteIpAddress` as the partition key for unauthenticated requests.
- Be mindful of shared IPs (corporate NATs, cloud providers) -- the limit should be high enough to avoid false positives but low enough to slow brute-force.

---

### FINDING-08: API Key Revocation Race Condition

**Severity:** Medium

**Description:** The `RevokeApiKeyAsync` method in `RedisApiKeyService` performs three sequential Valkey operations: delete hash entry, delete ID entry, remove from user set. These are not wrapped in a Valkey transaction (`MULTI`/`EXEC`).

If the process crashes between the first and third operations, the key is partially revoked:
- If the hash entry is deleted but the ID entry remains: the key is functionally revoked (validation fails) but still appears in the user's key list. This is a minor UX issue.
- If a validation request arrives between deletion of the hash entry and the `UpdateLastUsedAsync` fire-and-forget for the same key: the update will fail silently (expected behavior).

More concerning: `UpdateLastUsedAsync` rewrites the entire JSON blob for both the hash and ID entries. If a revocation occurs concurrently with a `LastUsedAt` update, the update could re-create the hash entry after revocation deleted it, effectively un-revoking the key.

**Recommendation:**
- Wrap the three deletion operations in `RevokeApiKeyAsync` in a Valkey transaction (`MULTI`/`EXEC` or `ITransaction` in StackExchange.Redis).
- In `UpdateLastUsedAsync`, use `When.Exists` instead of `When.Always` for the `StringSetAsync` calls. This prevents the update from re-creating a deleted entry.

---

### FINDING-09: Lazy Metadata Sync Race Condition

**Severity:** Medium

**Description:** The `ServiceAccountTrackingMiddleware` runs fire-and-forget after each successful response from an `sa-*` client. For a newly-registered DCR client making its first requests, multiple concurrent requests could each detect "no `ServiceAccountMetadata` record" and attempt to create one simultaneously.

This likely results in either duplicate database entries or a unique constraint violation (depending on the schema). The spec says "Errors are logged but never block the response," so the worst case is noisy error logs. But if duplicates are created, subsequent queries on `ServiceAccountMetadata` may return unexpected results.

**Recommendation:**
- Use an upsert pattern (PostgreSQL `INSERT ... ON CONFLICT DO UPDATE`) for the lazy metadata creation.
- Alternatively, use a distributed lock (Valkey `SET NX` with TTL) to ensure only one concurrent request creates the metadata record.
- Document that duplicate creation attempts are expected and handled gracefully.

---

### FINDING-10: DCR Bearer Token Registration -- Developer Impersonation Risk

**Severity:** Medium

**Description:** When a developer uses their JWT to register a client via DCR (Mode B), the resulting client is a standalone entity in Keycloak. There is no persistent link between the developer's user account and the client they registered, except:
- Keycloak's audit log (who created the client)
- The lazily-created `ServiceAccountMetadata` record (which uses `CreatedByUserId: Guid.Empty`)

This means:
1. If a developer's account is disabled or deleted, their registered clients continue to function.
2. There is no programmatic way to list "all clients registered by developer X" without querying Keycloak's audit log.
3. The `ServiceAccountMetadata` record has `CreatedByUserId: Guid.Empty`, so even Foundry's admin dashboard cannot attribute a client to its creator.

**Recommendation:**
- When a client is created via bearer token DCR, store the developer's `sub` claim in the `ServiceAccountMetadata.CreatedByUserId` field. This requires the tracking middleware to have access to Keycloak's audit data or for the DCR flow to go through a Foundry proxy that records the mapping.
- Implement a Keycloak event listener (SPI) that fires on client creation and stores the creator's user ID, or use the Keycloak Admin API to query client creation events.
- When a developer's account is disabled, flag or disable their registered clients (event-driven via Keycloak user lifecycle events).

---

### FINDING-11: Client Scope Policy Default is "No Restrictions"

**Severity:** Medium

**Description:** The spec states in the Fork Guide (Section 10): "Client Scope Policy: No restrictions (all scopes available)" as the default. This means a freshly deployed Foundry instance with open developer registration allows any developer to register a DCR client and request any scope, including `billing.manage`, `users.manage`, `scim.manage`, `serviceaccounts.manage`, etc.

Combined with auto-assignment of the `create-client` role (mentioned as an option for "open ecosystem"), this is a significant default-insecure configuration.

**Recommendation:**
- Change the default Client Scope Policy to a safe whitelist: `showcases.read`, `inquiries.read`, `inquiries.write`, `announcements.read`, `storage.read`. This matches the "developer" tier of access.
- Admin, billing, identity management, and SCIM scopes should require explicit operator action to enable for DCR clients.
- Document this prominently in the deployment guide, not just in the operator fork guide.

---

### FINDING-12: No Maximum API Key Count Per User

**Severity:** Medium

**Description:** The spec does not mention any limit on the number of API keys a single user can create. The `RedisApiKeyService.CreateApiKeyAsync` method has no check against existing key count. A malicious or compromised user could create thousands of API keys, each with its own independent rate limit quota (1,000 req/hr).

With N keys, a single user effectively gets N * 1,000 req/hr, bypassing the per-key rate limit entirely.

**Recommendation:**
- Enforce a maximum API key count per user (e.g., 25 keys). Check the size of the `apikeys:user:<userId>` set before creating a new key.
- Document this limit in the spec and make it configurable for operators.

---

### FINDING-13: API Key `keyId` Generation Uses Truncated Base64

**Severity:** Low

**Description:** The `keyId` is generated by taking 12 random bytes, base64-encoding them, stripping `+`, `/`, and `=` characters, then truncating to 16 characters. The stripping of characters before truncation means the effective entropy varies. In the worst case, if many characters were stripped, the ID could be shorter than 16 characters or have less entropy than expected.

The `keyId` is used as a Redis key (`apikey:id:<keyId>`) and for management operations (revoke by ID). While `keyId` is not a secret (it appears in API responses and logs), low entropy could make it guessable, allowing an attacker to enumerate or revoke other users' keys if they can access the revocation endpoint.

The revocation endpoint does check `data.UserId != userId`, so cross-user revocation is blocked. The risk is limited to information disclosure (confirming key existence) via the List endpoint (which is already scoped to the authenticated user).

**Recommendation:**
- Use `Guid.NewGuid().ToString("N")` or a fixed-length hex string from random bytes for `keyId` generation, avoiding the variable-length stripping issue.
- This is low priority since the ownership check prevents cross-user operations.

---

### FINDING-14: No Explicit Token Replay Protection for JWTs

**Severity:** Low

**Description:** The spec states that JWT validation checks signature, audience, expiry, and issuer. It does not mention `jti` (JWT ID) claim validation or any nonce/replay protection mechanism. Since access tokens are short-lived (5 minutes default), the window for replay is small.

However, if an access token is intercepted (e.g., via a compromised log, a man-in-the-middle on a non-TLS internal network, or a browser extension), it can be replayed from any source within its 5-minute lifetime.

Keycloak includes a `jti` claim in all tokens, but Foundry does not validate or track it.

**Recommendation:**
- For most deployments, the 5-minute token lifetime is sufficient mitigation. Document this as an accepted risk.
- For high-security deployments, consider enabling Keycloak's "Revocation" feature and Foundry periodically fetching the revocation list, or implementing sender-constrained tokens (DPoP, RFC 9449).
- Add "Enforce HTTPS everywhere" to the spec's security guarantees, not just the hardening checklist.

---

### FINDING-15: `UpdateLastUsedAsync` Fire-and-Forget Masks Valkey Failures

**Severity:** Low

**Description:** Both `ApiKeyAuthenticationMiddleware` (via `ValidateApiKeyAsync`) and `ServiceAccountTrackingMiddleware` use fire-and-forget patterns for non-critical writes. In `UpdateLastUsedAsync`, if Valkey is degraded (high latency, connection issues), the exceptions are caught and logged but the authentication succeeds.

This is correct behavior (auth should not block on audit writes). However, if Valkey is consistently failing, the `LastUsedAt` timestamps become stale, and operators lose visibility into key usage patterns. There is no health check or alert for this specific failure mode.

**Recommendation:**
- Add a health check that periodically verifies Valkey write capability (existing health checks may already cover this).
- Consider a counter metric (`foundry.apikey_last_used_update_failures_total`) that operators can alert on.

---

### FINDING-16: SCIM Authentication Queries Across All Tenants

**Severity:** Low

**Description:** The spec notes that `ScimAuthenticationMiddleware` "Uses token prefix (first 8 chars) to query ScimConfiguration across all tenants (`IgnoreQueryFilters`)." This means every SCIM request triggers a database query that scans all tenants' SCIM configurations.

While the constant-time hash comparison prevents timing attacks on the token value itself, the prefix-based lookup could leak information about whether any SCIM configuration exists with a given prefix. Additionally, a high volume of invalid SCIM requests could cause excessive database load.

**Recommendation:**
- The SCIM rate limit (30 req/min) mitigates the database load concern.
- Consider caching the SCIM token prefix-to-config mapping in Valkey with a short TTL to reduce database queries.
- This is informational -- the current design is acceptable given the rate limit.

---

### FINDING-17: Spec Does Not Address CSRF for User Sessions

**Severity:** Info

**Description:** The spec covers OIDC user sessions (Path 2) but does not mention CSRF protection. Since Foundry is an API (not a server-rendered web app) and uses Bearer tokens in the `Authorization` header, traditional CSRF is not a concern -- browsers do not automatically attach `Authorization` headers to cross-origin requests.

However, if any Foundry endpoint accepts cookies for authentication (e.g., SignalR WebSocket upgrade using query string tokens that are then stored as cookies), CSRF could become relevant.

**Recommendation:**
- Document in the spec that Foundry is CSRF-safe because it uses Bearer token authentication exclusively and does not use cookie-based auth.
- Note the exception for SignalR: the JWT is passed as a query string parameter during WebSocket upgrade, not as a cookie. Confirm this is the only authentication path that does not use the `Authorization` header.

---

### FINDING-18: Session Fixation Not Applicable But Should Be Documented

**Severity:** Info

**Description:** Session fixation attacks (where an attacker sets a victim's session ID before authentication) are not applicable to Foundry because Foundry does not maintain server-side sessions. Authentication is stateless (JWT or API key on every request). Keycloak manages sessions for the OIDC flow.

**Recommendation:**
- Add a brief note in the Security Guarantees section: "Foundry is not vulnerable to session fixation because it uses stateless authentication. Session management is delegated to Keycloak, which implements standard session fixation protections (session ID rotation on login)."

---

### FINDING-19: Credential Stuffing Mitigation Incomplete

**Severity:** Info

**Description:** The spec mentions Keycloak's brute-force detection (Production Hardening Checklist) for login attempts. For API keys, the `sk_live_` format with 32 random bytes (256 bits of entropy) makes brute-force infeasible. For service account `client_secret` values, Keycloak generates high-entropy secrets.

The remaining vector is credential stuffing with leaked API keys from other Foundry instances. Since all instances use the same `sk_live_` prefix format, a key leaked from Instance A could be tried against Instance B. This would fail (different Valkey store) but consumes resources.

**Recommendation:**
- The per-IP rate limiting recommended in FINDING-07 would mitigate this.
- Consider including a Foundry instance identifier in the key format (e.g., `sk_live_<instance-hash>_<secret>`) to enable early rejection of keys from other instances. This is a nice-to-have, not a requirement.

---

## Assumptions That Should Be Explicitly Documented

1. **HTTPS is mandatory in production.** The spec mentions it in the hardening checklist but does not state it as a security requirement. All three auth paths transmit secrets (API keys, JWTs, client credentials) over the wire. Without TLS, every credential is trivially interceptable.

2. **Valkey is trusted infrastructure.** API key metadata (including user IDs, tenant IDs, and scope lists) is stored in Valkey in plaintext JSON. Anyone with Valkey access can read all API key metadata, impersonate any API key user (by crafting requests with known hashes), or delete all keys. The spec should document that Valkey must be network-isolated and access-controlled.

3. **Keycloak is the trust root.** The entire auth system trusts Keycloak's JWT signatures and JWKS endpoint. A compromised Keycloak instance compromises all authentication. The spec should document this dependency and recommend Keycloak hardening (separate network, admin access controls, audit logging).

4. **Clock synchronization is required.** JWT expiration checks and API key TTLs depend on synchronized clocks between Keycloak, Foundry, and Valkey. Clock skew could cause premature or delayed token expiration. ASP.NET Core's JWT handler has a default 5-minute clock skew tolerance, but this should be documented.

5. **The `sa-` prefix convention is a social contract, not a technical control.** The spec correctly identifies this but should elevate it to an explicit assumption: "The `sa-` prefix is enforced by convention and fail-safe defaults, not by Keycloak configuration. A Keycloak SPI or registration policy is recommended for production deployments."

---

## Summary Table

| ID | Severity | Finding | Status |
|----|----------|---------|--------|
| FINDING-01 | High | API key timing attack (hash lookup) | Acceptable with documentation |
| FINDING-02 | Critical | API key scope subset validation not implemented | Must fix before production |
| FINDING-03 | High | No rate limiting on DCR endpoint | Must fix before production |
| FINDING-04 | High | `sa-` prefix not enforced, impersonation risk | Must fix before production |
| FINDING-05 | High | X-Tenant-Id override for all service accounts | Must fix before production |
| FINDING-06 | High | Valkey durability on restart | Must fix before production |
| FINDING-07 | Medium | No per-IP rate limiting pre-auth | Should fix |
| FINDING-08 | Medium | API key revocation race condition | Should fix |
| FINDING-09 | Medium | Lazy metadata sync race condition | Should fix |
| FINDING-10 | Medium | DCR developer attribution missing | Should fix |
| FINDING-11 | Medium | Client Scope Policy default insecure | Should fix |
| FINDING-12 | Medium | No max API key count per user | Should fix |
| FINDING-13 | Low | keyId entropy from truncated base64 | Nice to have |
| FINDING-14 | Low | No token replay protection | Acceptable risk |
| FINDING-15 | Low | Fire-and-forget masks Valkey failures | Nice to have |
| FINDING-16 | Low | SCIM queries across all tenants | Acceptable with rate limit |
| FINDING-17 | Info | CSRF not addressed (not applicable) | Document |
| FINDING-18 | Info | Session fixation not applicable | Document |
| FINDING-19 | Info | Credential stuffing cross-instance | Nice to have |

**Critical findings requiring immediate action:** 1
**High findings requiring action before production:** 5
**Medium findings to address in normal priority:** 6
**Low/Info findings for documentation or backlog:** 7
