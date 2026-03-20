# Foundry Authentication & Authorization Architecture

**Date:** 2026-03-18
**Status:** Draft
**Module:** Identity (primary), all modules (consumers)

## Problem

Foundry is an open-source modular monolith platform that teams fork and extend. External applications (BFFs, mobile backends, third-party developer apps) and individual developers need to call the Foundry API. Every request must carry an identity — there is no anonymous access. The authentication architecture must support three distinct caller types (apps operated by the platform owner, apps built by third-party developers, and individual developers/scripts) while remaining secure, auditable, and open-source-friendly across forked instances.

This document is the definitive reference for how every request to the Foundry API gets authenticated, authorized, and rate-limited. It covers the complete auth ecosystem: Keycloak DCR service accounts, OIDC user sessions, and user API keys.

---

## Table of Contents

1. [Mental Model](#1-mental-model)
2. [Middleware Pipeline](#2-middleware-pipeline)
3. [Auth Path 1: DCR Service Accounts](#3-auth-path-1--dcr-service-accounts)
4. [Auth Path 2: User Sessions (OIDC)](#4-auth-path-2--user-sessions-oidc)
5. [Auth Path 3: User API Keys](#5-auth-path-3--user-api-keys)
6. [The Open Ecosystem](#6-the-open-ecosystem)
7. [Rate Limiting](#7-rate-limiting)
8. [Permission Model](#8-permission-model)
9. [Security Guarantees](#9-security-guarantees)
9.5. [Error Response Specification](#95-error-response-specification)
10. [For Platform Operators (Fork Guide)](#10-for-platform-operators-fork-guide)

---

## 1. Mental Model

Every request to Foundry must carry an identity. There are no anonymous endpoints (see [Security Guarantees §9](#9-security-guarantees) for the one documented exception). Three authentication paths exist, each for a different caller type:

| Path | Caller | Auth Method | Identity | Example |
|------|--------|-------------|----------|---------|
| **Operator Service Account** | Your apps (BFF, mobile backend) | OAuth2 `client_credentials` via Keycloak | The app itself (`sa-personal-site`) | BFF submitting inquiry on behalf of a visitor |
| **Developer App** | Third-party apps | OAuth2 `client_credentials` via Keycloak | The app (`app-cool-viewer`) | Third-party showcase aggregator |
| **User Session** | Logged-in humans via browser | OIDC authorization code + PKCE via Keycloak | The user (`sub` claim = Keycloak user ID) | User managing their profile in a web app |
| **API Key** | Developers, scripts, integrations | `X-Api-Key` header with `sk_live_*` key | The developer who created the key (user ID + tenant) | Developer testing endpoints, CI/CD automation |

**Key principle**: Service accounts represent *apps*. API keys represent *people accessing programmatically*. User sessions represent *people using apps interactively*. All three resolve to a `ClaimsPrincipal` with permissions — the rest of the pipeline treats them identically.

Service accounts use Keycloak DCR but with **different prefixes** to distinguish trust levels:

| Mode | Prefix | Auth | Who | Trust Level |
|------|--------|------|-----|-------------|
| **Operator-provisioned** | `sa-` | Initial Access Token | Platform operator (you) | Full trust. Can override tenant via `X-Tenant-Id`. |
| **Developer self-service** | `app-` | Bearer Token (developer's JWT) | Any user with `create-client` role | Restricted. Policy-limited scopes. No tenant override. |

The prefix distinction is critical for security. `sa-*` clients are trusted infrastructure you control. `app-*` clients are third-party and constrained by Keycloak policies. See [Security Guarantees §9](#9-security-guarantees) for enforcement details.

```
                    ┌─────────────────────┐
                    │   Foundry API        │
                    │                      │
   sa-* token ──────►  Operator apps      │
   (your infra)     │                      │
                    │  All paths resolve   │
  app-* token ──────►  to the same:       │──► Same authorization pipeline
   (3rd-party)      │  → ClaimsPrincipal  │
                    │  → TenantContext     │
   User JWT ────────►  → Permissions      │
                    │                      │
   X-Api-Key ───────►                     │
                    │                      │
                    └─────────────────────┘
```

---

## 2. Middleware Pipeline

The middleware executes in strict order in `src/Foundry.Api/Program.cs` (lines 398–427). Each layer adds context for the next. Reordering breaks authentication.

```
Request arrives
  │
  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ 1. Rate Limiter (app.UseRateLimiter)                                        │
│    ASP.NET Core built-in. Applies global and policy-specific rate limits    │
│    before any authentication occurs. Identity for rate limiting is resolved │
│    after auth — this layer catches volumetric abuse pre-auth.               │
│    Source: RateLimitDefaults.cs                                             │
├─────────────────────────────────────────────────────────────────────────────┤
│ 2. ApiKeyAuthenticationMiddleware                                           │
│    Checks for X-Api-Key header.                                             │
│    If present: validate key via Valkey (SHA256 hash lookup), create         │
│    ClaimsPrincipal with sub, organization, scope, auth_method=api_key       │
│    claims. Sets TenantContext directly. Request proceeds as authenticated.  │
│    If absent or empty: fall through to next middleware (JWT auth).           │
│    If invalid: return 401 immediately with RFC 7807 Problem Details.        │
│    Source: ApiKeyAuthenticationMiddleware.cs                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│ 3. Authentication (app.UseAuthentication)                                    │
│    ASP.NET Core + Keycloak.AuthServices. Validates JWT Bearer tokens:       │
│    - Signature verified against Keycloak's public keys (JWKS endpoint)     │
│    - Audience must include "foundry-api" (configured in appsettings.json)  │
│    - Token must not be expired                                              │
│    - Issuer must match the configured Keycloak authority URL               │
│    If API key auth already set ClaimsPrincipal, this layer is a no-op.      │
│    Source: Keycloak.AuthServices.Authentication NuGet package                │
├─────────────────────────────────────────────────────────────────────────────┤
│ 4. TenantResolutionMiddleware                                               │
│    Reads the "organization" claim from JWT. Supports two formats:           │
│    - Simple GUID: "550e8400-e29b-41d4-a716-446655440000"                   │
│    - Keycloak 26+ JSON: {"orgId": {"name": "orgName"}}                     │
│    Parses the org GUID and name, populates ITenantContext via               │
│    ITenantContextSetter.SetTenant().                                        │
│    X-Tenant-Id header override: allowed only for realm admins (admin role  │
│    in realm_access) and OPERATOR service accounts (azp starts with "sa-"). │
│    Developer apps (azp starts with "app-") are NOT allowed to override     │
│    tenant context — they operate within the tenant of the user who         │
│    authorized them, or are tenant-agnostic (TenantId.Platform).            │
│    Region resolution: JWT tenant_region claim > X-Tenant-Region header >   │
│    RegionConfiguration.PrimaryRegion default.                               │
│    Pushes TenantId and UserId into Serilog LogContext for structured        │
│    logging. Increments foundry.requests_by_tenant_total OTel counter.       │
│    For API key auth: tenant was already set by ApiKeyAuthenticationMiddleware│
│    — this middleware reads it from the organization claim that was set.      │
│                                                                             │
│    **Multi-org behavior (undefined)**: If a user belongs to multiple        │
│    Keycloak organizations, the current implementation takes the first org   │
│    from the claim. The intended behavior (user picks at login, app          │
│    specifies, default org) is not yet designed. Tracked as foundry-9po7.   │
│    Source: TenantResolutionMiddleware.cs                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│ 5. TenantBaggageMiddleware                                                  │
│    Sets OpenTelemetry Activity tags and W3C Baggage for downstream          │
│    propagation of tenant context. Observability only — no auth logic.       │
│    Source: TenantBaggageMiddleware.cs                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│ 6. ScimAuthenticationMiddleware                                             │
│    Only applies to /scim/v2/* endpoints (except discovery endpoints:        │
│    /ServiceProviderConfig, /Schemas, /ResourceTypes).                       │
│    Extracts Bearer token from Authorization header. Uses token prefix       │
│    (first 8 chars) to query ScimConfiguration across all tenants            │
│    (IgnoreQueryFilters). Validates full token hash with                     │
│    CryptographicOperations.FixedTimeEquals (constant-time comparison to     │
│    prevent timing attacks). Sets TenantContext from matched config.          │
│    Creates minimal ClaimsPrincipal: scim_client, auth_method=scim_bearer,  │
│    tenant_id.                                                               │
│    SCIM tokens are NOT JWTs — they are opaque bearer tokens stored as      │
│    SHA256 hashes in the ScimConfiguration table.                            │
│    Source: ScimAuthenticationMiddleware.cs                                   │
├─────────────────────────────────────────────────────────────────────────────┤
│ 7. PermissionExpansionMiddleware                                            │
│    Transforms auth-path-specific claims into unified "permission" claims.   │
│    Three code paths based on the authenticated principal:                   │
│                                                                             │
│    a) Service accounts (azp starts with "sa-" or "app-"):                  │
│       Reads "scope" claims (space-separated). Maps each scope to a         │
│       PermissionType constant via MapScopeToPermission(). Unknown scopes    │
│       are silently ignored. Both operator (sa-*) and developer (app-*)     │
│       clients use the same scope-based expansion path.                      │
│                                                                             │
│    b) API keys (auth_method == "api_key"):                                 │
│       Same logic as service accounts — reads "scope" claims, maps to       │
│       PermissionType. Uses the same ExpandServiceAccountScopes() method.    │
│                                                                             │
│    c) Regular users (everything else):                                      │
│       Reads ClaimTypes.Role claims. Falls back to parsing Keycloak's       │
│       realm_access JSON claim for roles array. Expands roles to            │
│       permissions via RolePermissionMapping.GetPermissions().               │
│                                                                             │
│    All three paths produce the same output: "permission" claims on the     │
│    ClaimsIdentity. Downstream authorization sees a uniform interface.       │
│    Source: PermissionExpansionMiddleware.cs, RolePermissionMapping.cs        │
├─────────────────────────────────────────────────────────────────────────────┤
│ 8. Authorization (app.UseAuthorization)                                     │
│    ASP.NET Core authorization. Controllers use [HasPermission(X)]           │
│    attribute. PermissionAuthorizationHandler checks if a "permission"       │
│    claim matching the required PermissionType exists on ClaimsPrincipal.    │
│    No claim → 403 Forbidden.                                                │
│    Source: PermissionRequirement.cs, PermissionAuthorizationHandler.cs       │
├─────────────────────────────────────────────────────────────────────────────┤
│ 9. ModuleTaggingMiddleware                                                  │
│    Tags HTTP requests with foundry.module for observability routing.        │
│    No auth logic.                                                           │
│    Source: ModuleTaggingMiddleware.cs                                        │
├─────────────────────────────────────────────────────────────────────────────┤
│ 10. ServiceAccountTrackingMiddleware                                        │
│     Runs AFTER the response. Fire-and-forget background task.               │
│     Only tracks successful requests (HTTP 200-299) from sa-* clients.       │
│     If the sa-* client has a ServiceAccountMetadata record: updates         │
│     LastUsedAt timestamp.                                                   │
│     If the sa-* client has NO record (e.g., newly DCR-registered):          │
│     lazily creates a ServiceAccountMetadata with TenantId.Platform          │
│     sentinel (00000000-0000-0000-0000-000000000001), the client ID as name, │
│     empty scopes, and Guid.Empty as CreatedByUserId.                        │
│     Errors are logged but never block the response.                         │
│     Source: ServiceAccountTrackingMiddleware.cs                              │
└─────────────────────────────────────────────────────────────────────────────┘
  │
  ▼
Controller action executes
```

**Critical ordering rules:**
- API key middleware runs *before* JWT auth — if a key is present, JWT validation is bypassed entirely. This is correct because API keys are self-contained (one Valkey lookup) while JWT validation requires Keycloak JWKS verification.
- Tenant resolution must happen *after* authentication (it needs claims to read the organization).
- Permission expansion must happen *after* tenant resolution (some permission logic may depend on tenant context).
- Authorization must happen *after* permission expansion (it checks permission claims that expansion produces).
- Service account tracking runs *after* authorization — only tracks requests that passed auth. Runs post-response to avoid adding latency.

---

## 3. Auth Path 1 — DCR Service Accounts

This path is for **any app that needs to call the Foundry API** — whether you built it or a third-party developer did. Both modes use Keycloak's OpenID Connect Dynamic Client Registration endpoint ([RFC 7591](https://datatracker.ietf.org/doc/html/rfc7591), [Keycloak docs](https://www.keycloak.org/securing-apps/client-registration)).

**For DCR implementation details** (realm export changes, configure-dcr.sh, docker compose integration), see the DCR implementation spec: `docs/superpowers/specs/2026-03-16-dynamic-client-registration-design.md`. This section covers the architecture and flows; the DCR spec covers the implementation steps.

### Mode A: Operator-Provisioned (Your Apps)

For apps you control — BFFs, mobile backends, internal tools. You create an Initial Access Token in the Keycloak Admin Console (`Clients → Initial Access Token → Create`) and use it to register the client.

**Registration flow:**

```
App startup (one-time):
┌──────────────┐  POST /realms/foundry/clients-registrations/openid-connect  ┌──────────┐
│  Your App    │──────────────────────────────────────────────────────────────►│ Keycloak │
│  (BFF)       │  Authorization: Bearer <initial-access-token>                │          │
│              │  Content-Type: application/json                               │          │
│              │  Body: {                                                      │          │
│              │    "client_id": "sa-personal-site",                           │          │
│              │    "client_name": "Personal Site BFF",                        │          │
│              │    "grant_types": ["client_credentials"],                     │          │
│              │    "token_endpoint_auth_method": "client_secret_basic"        │          │
│              │  }                                                            │          │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  Response: {                                                  │          │
│              │    "client_id": "sa-personal-site",                           │          │
│              │    "client_secret": "<generated>",                            │          │
│              │    "registration_access_token": "<generated>"                 │          │
│              │  }                                                            └──────────┘
└──────────────┘
  ↓ stores client_secret and registration_access_token locally
```

**Initial Access Token characteristics:**
- Created by admin via Keycloak Admin Console
- Single-use: one token registers one client, then the token is consumed
- Time-limited: configurable expiry (recommended: 24 hours for deployment windows)
- Configurable max client count per token (recommended: 1)

**After registration, admin assigns scopes** to the new client via:
- Keycloak Admin Console (`Clients → sa-personal-site → Client Scopes`)
- Foundry API: `PUT /api/v1/identity/service-accounts/{id}/scopes`

**Pre-configured clients** like `sa-foundry-api` (the personal site BFF) can be defined directly in `docker/keycloak/realm-export.json` with scopes pre-assigned. They exist on every fresh deployment without requiring DCR.

### Mode B: Developer Self-Service (Third-Party Apps)

For apps built by anyone in the community. The developer registers as a Foundry user, authenticates via Keycloak (getting their own JWT), and uses that JWT as the Bearer token to register a client via the same DCR endpoint.

Keycloak supports this natively: "[The bearer token can be issued on behalf of a user or a Service Account](https://www.keycloak.org/securing-apps/client-registration)." The user must have the `create-client` or `manage-client` realm role.

**Registration flow:**

```
Step 1: Developer authenticates (standard OIDC)
┌──────────────┐  authorization_code + PKCE  ┌──────────┐
│  Developer   │◄───────────────────────────►│ Keycloak │
│              │  Result: developer's JWT     └──────────┘
└──────────────┘

Step 2: Developer registers their app via Foundry DCR proxy
┌──────────────┐  POST /api/v1/identity/apps/register                         ┌──────────┐
│  Developer   │──────────────────────────────────────────────────────────────►│ Foundry  │
│              │  Authorization: Bearer <developer's own JWT>                  │ API      │
│              │  Content-Type: application/json                                │          │
│              │  Body: {                                                       │          │
│              │    "client_id": "app-cool-showcase-viewer",                    │          │
│              │    "client_name": "Cool Showcase Viewer",                      │          │
│              │    "requested_scopes": ["showcases.read"]                      │          │
│              │  }                                                             │          │
│              │                                                               │          │
│              │  Foundry validates:                                            │          │
│              │    ✓ client_id starts with "app-"                             │          │
│              │    ✓ rate limit: 5 registrations/hr per user                  │          │
│              │    ✓ requested_scopes are in allowed whitelist                │          │
│              │  Then forwards to Keycloak DCR endpoint                       │          │
│              │                                                               │          │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  Response: {                                                   │          │
│              │    "client_id": "app-cool-showcase-viewer",                    │          │
│              │    "client_secret": "<generated>",                             │          │
│              │    "registration_access_token": "<generated>"                  │          │
│              │  }                                                             └──────────┘
└──────────────┘
  ↓ developer stores credentials securely
```

**Requirements for developer self-service:**
- Developer must be a registered Foundry user (Keycloak account)
- Developer must have the `create-client` Keycloak realm role
- Assignment policy: auto-assign to all registered users (open ecosystem) or grant on request (controlled ecosystem) — this is a per-instance operator decision

**Keycloak policies that constrain developer-registered clients:**

| Policy | Purpose | Recommended Configuration |
|--------|---------|--------------------------|
| **Client Scope Policy** | Whitelists which scopes DCR clients can request | Allow: `showcases.read`, `inquiries.write`, `inquiries.read`. Block: all admin, billing, identity management scopes. |
| **Max Clients Policy** | Limits total registered clients | Default: 200 for anonymous, configurable for authenticated. Recommended: 10 per developer. |
| **Full Scope Policy** | Controls whether "Full Scope Allowed" is enabled | Disabled by default for DCR clients. Clients only get explicitly assigned scopes. |
| **Client Disabled Policy** | Optionally requires admin approval | New clients created in disabled state. Admin activates after review. Recommended for production. |
| **Consent Required Policy** | Shows user consent screen | Automatically enabled if the app later uses authorization_code flow (user login through the app). Users see what they're authorizing. |
| **Trusted Hosts Policy** | Restricts which hosts can register | Dev: allow localhost/127.0.0.1. Prod: remove or restrict to known deploy hosts. |

These policies are configured via the Keycloak Admin REST API (component model) since they are **not configurable in realm-export.json**. See `docker/keycloak/configure-dcr.sh` for the setup script.

### Client Management via Registration Access Token

When a client is registered (either mode), Keycloak returns a `registration_access_token`. This token allows the registrant to manage their own client:

- **Read**: `GET /realms/foundry/clients-registrations/openid-connect/{client_id}` with `Authorization: Bearer <registration_access_token>`
- **Update**: `PUT /realms/foundry/clients-registrations/openid-connect/{client_id}` (e.g., update redirect URIs, rotate secret)
- **Delete**: `DELETE /realms/foundry/clients-registrations/openid-connect/{client_id}`

**Registration access token rotation**: Enabled by default in Keycloak. Each use of the token generates a new one in the response. The old token is invalidated. This means:
- The token can only be used once per operation
- The app must store the new token after each management request
- If the token is lost, the admin must regenerate it via Keycloak Admin Console

### Runtime Flow (Identical for Both Modes)

Once a client has credentials, the runtime flow is the same regardless of how it was registered:

```
Every API request:
┌──────────────┐  POST /realms/foundry/protocol/openid-connect/token          ┌──────────┐
│  App         │──────────────────────────────────────────────────────────────►│ Keycloak │
│              │  Content-Type: application/x-www-form-urlencoded              │          │
│              │  grant_type=client_credentials                                │          │
│              │  client_id=app-cool-showcase-viewer                            │          │
│              │  client_secret=<secret>                                       │          │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  { "access_token": "<jwt>", "expires_in": 300, ... }         └──────────┘
│              │
│              │  GET /api/v1/showcases                                        ┌──────────┐
│              │──────────────────────────────────────────────────────────────►│ Foundry  │
│              │  Authorization: Bearer <jwt>                                  │ API      │
│              │◄──────────────────────────────────────────────────────────────│          │
│              │  { "data": [...] }                                            └──────────┘
└──────────────┘
```

**JWT claims produced by Keycloak for service accounts:**
- `azp`: the client ID (e.g., `app-cool-showcase-viewer`) — this is how `PermissionExpansionMiddleware` detects service accounts
- `scope`: space-separated OAuth2 scopes assigned to the client (e.g., `showcases.read inquiries.write`)
- `aud`: must include `foundry-api` — enforced by the `foundry-api-audience` realm default client scope
- `sub`: Keycloak's internal service account user ID (auto-created for each confidential client)
- `iss`: the Keycloak authority URL (e.g., `http://localhost:8080/realms/foundry`)

**The prefix convention:**

Foundry uses two prefixes to distinguish client trust levels:

| Prefix | Who Creates | Trust Level | `X-Tenant-Id` Override | Registration Path |
|--------|-------------|-------------|----------------------|-------------------|
| `sa-` | Platform operator | Full trust | Allowed | Direct Keycloak DCR with Initial Access Token |
| `app-` | Third-party developer | Restricted | **Not allowed** | Foundry proxy endpoint (rate-limited, prefix-enforced) |

`PermissionExpansionMiddleware` identifies service accounts by checking if the JWT's `azp` claim starts with `sa-` OR `app-`. Both use scope-based permission expansion. Everything else uses role-based expansion.

**Prefix enforcement:**
- **`sa-*`**: Only registerable via Initial Access Token (operator-controlled). Developers cannot obtain these tokens without admin intervention.
- **`app-*`**: Enforced by the Foundry DCR proxy endpoint (`POST /api/v1/identity/apps/register`). The proxy validates the `app-` prefix before forwarding to Keycloak. Direct Keycloak DCR with a bearer token is disabled by Keycloak policy.
- **No prefix / wrong prefix**: `PermissionExpansionMiddleware` treats it as a regular user. Service accounts have no realm roles → role-based expansion yields zero permissions → 403 on every request. Fail-safe.

**This is intentionally fail-safe**: a missing or incorrect prefix results in LESS access (zero permissions), never more.

### Lazy Metadata Sync

`ServiceAccountTrackingMiddleware` runs after authorization on every successful request. When it encounters an `sa-*` or `app-*` client that has no `ServiceAccountMetadata` record in the database (e.g., a newly DCR-registered client making its first API call):

1. Creates a new `ServiceAccountMetadata` entity with:
   - `TenantId`: `TenantId.Platform` (sentinel GUID `00000000-0000-0000-0000-000000000001`)
   - `KeycloakClientId`: from `azp` claim (e.g., `app-cool-showcase-viewer`)
   - `Name`: from `azp` claim
   - `Description`: null
   - `Status`: `Active`
   - `Scopes`: empty (scopes come from JWT at runtime, not stored locally)
   - `CreatedByUserId`: `Guid.Empty` (system-created)
2. Sets `LastUsedAt` to current time
3. Saves to database

This runs fire-and-forget. Errors are logged but never block the API response. The metadata is for admin visibility and audit — it does not affect authentication or authorization.

**When to use this path:**
- Your personal site BFF (`sa-personal-site`) — Mode A
- A mobile app backend you build — Mode A
- Internal microservices or admin tools — Mode A
- Third-party developer apps that serve their own users (`app-cool-viewer`) — Mode B
- Any server-side app where the caller can securely store a `client_secret`

**When NOT to use this path:**
- Individual developers making API calls for themselves (use API keys — Path 3)
- Browser-side JavaScript with no backend (cannot hold secrets — must use BFF pattern or user session)
- End users interacting with apps (use OIDC sessions — Path 2)

> **Note: Mobile app OIDC flow** — Native mobile apps that need user login use authorization_code + PKCE as a public Keycloak client (separate from the mobile backend's `sa-*` service account). This flow is standard OIDC and will be documented in a future mobile integration guide.

---

## 4. Auth Path 2 — User Sessions (OIDC)

This path is for **humans using apps interactively** — browsing your site, managing their account, logging into a dashboard. Authentication is fully delegated to Keycloak via OpenID Connect.

### Flow

```
┌─────────┐     ┌──────────────┐     ┌──────────┐     ┌─────────┐
│ Browser │────►│  Your App    │────►│ Keycloak │────►│ Foundry │
│         │     │  (BFF/SPA)   │     │          │     │ API     │
└─────────┘     └──────────────┘     └──────────┘     └─────────┘

1. User clicks "Log in" in the app
2. App redirects to Keycloak login page:
   GET /realms/foundry/protocol/openid-connect/auth
     ?response_type=code
     &client_id=foundry-spa
     &redirect_uri=https://app.example.com/callback
     &scope=openid profile email
     &code_challenge=<S256 hash>
     &code_challenge_method=S256
3. User authenticates (email/password, social login, SSO federation)
4. Keycloak redirects back with authorization code:
   GET https://app.example.com/callback?code=<authorization_code>
5. App exchanges code for tokens:
   POST /realms/foundry/protocol/openid-connect/token
     grant_type=authorization_code
     &code=<authorization_code>
     &redirect_uri=https://app.example.com/callback
     &code_verifier=<PKCE verifier>
     &client_id=foundry-spa
6. Response: { access_token: "<jwt>", refresh_token: "<token>", id_token: "<jwt>" }
7. App sends user's JWT to Foundry API:
   GET /api/v1/organizations
   Authorization: Bearer <access_token>
```

### JWT Claims Used by Foundry

| Claim | Source | Used By | Purpose |
|-------|--------|---------|---------|
| `sub` | Keycloak user ID (UUID) | ClaimTypes.NameIdentifier, audit logs | Unique user identity |
| `organization` | Keycloak org membership | TenantResolutionMiddleware | Tenant context. Format: `{"orgId": {"name": "orgName"}}` (Keycloak 26+) or simple GUID |
| `realm_access` | Keycloak realm config | PermissionExpansionMiddleware | JSON object with `roles` array: `["admin"]`, `["manager"]`, `["user"]` |
| `azp` | Keycloak client config | PermissionExpansionMiddleware | Authorized party. For user sessions this is `foundry-spa` (not `sa-*`), so role-based expansion is used |
| `aud` | `foundry-api-audience` scope | JWT validation middleware | Must include `foundry-api`. Enforced by realm default client scope |
| `tenant_region` | Keycloak user attribute | TenantResolutionMiddleware | Optional. Region affinity for the user's tenant |

### Permission Expansion for Users

Users are identified by having an `azp` claim that does NOT start with `sa-` and NOT having `auth_method=api_key`. Their permissions come from Keycloak realm roles, expanded via `RolePermissionMapping`:

```
JWT realm_access.roles: ["manager"]
  ↓ RolePermissionMapping.GetPermissions()
  ↓
Permission claims added to ClaimsPrincipal:
  UsersRead, BillingRead, OrganizationsRead, OrganizationsManageMembers,
  ApiKeysRead, ApiKeysCreate, ApiKeysUpdate, ApiKeysDelete,
  SsoRead, ConfigurationManage, ShowcasesRead, ShowcasesManage, InquiriesRead
```

**Three role tiers** (defined in `RolePermissionMapping.cs`, case-insensitive matching):

| Role | Permission Count | Access Level |
|------|-----------------|--------------|
| **admin** | 47 | Everything: user/role/billing/subscription/org/API key/notification/webhook/SSO/SCIM/storage/showcase/service account/inquiry management, admin access, system settings |
| **manager** | 13 | Team management: users read, billing read, org read + manage members, API keys CRUD, SSO read, config manage, showcases read + manage, inquiries read |
| **user** | 9 | Basic access: org read, messaging, notification read, email preferences, announcement read, storage read + write, showcases read, inquiries write |

Roles are cumulative — a user with both `admin` and `manager` roles gets the union of all permissions (deduplicated).

### Keycloak Client Configuration

The `foundry-spa` client in `realm-export.json` is pre-configured for this flow:
- Public client (no client_secret — cannot hold secrets in browser)
- Standard flow enabled (authorization_code)
- Direct access grants disabled (no resource owner password grant)
- PKCE required
- Redirect URIs configured per deployment

### Token Lifecycle

| Token | Lifetime | Storage | Renewal |
|-------|----------|---------|---------|
| Access token | 5 minutes (configurable in Keycloak) | App memory | Automatic via refresh token |
| Refresh token | 30 minutes (configurable) | App memory or secure cookie | `POST /token` with `grant_type=refresh_token` |
| ID token | Same as access token | Not sent to Foundry API | Used by app for user profile display |

Foundry does not handle token refresh — the app (BFF or SPA) is responsible for refreshing tokens via Keycloak before they expire.

### Token Revocation

- **Logout**: App calls `POST /realms/foundry/protocol/openid-connect/logout` with the refresh token. Keycloak invalidates all tokens for that session.
- **Admin revocation**: Admin can revoke all sessions for a user via Keycloak Admin Console.
- **Foundry's role**: Foundry's `KeycloakTokenService` provides `RevokeTokenAsync(refreshToken)` as a convenience wrapper. Foundry itself does not maintain a token blacklist — it relies on Keycloak's short-lived access tokens and JWKS rotation.

### When to Use This Path

- Any interactive user session in a browser
- User managing their account, viewing dashboards, sending messages
- Admin performing tenant management via a web UI
- Any case where a human is directly interacting with an application

### When NOT to Use This Path

- Server-to-server calls (use DCR service accounts — Path 1)
- Programmatic/script access (use API keys — Path 3)
- Pre-login actions like submitting an inquiry on behalf of a visitor (use the BFF's service account — Path 1)

---

## 5. Auth Path 3 — User API Keys

This path is for **developers who want personal programmatic access** — scripts, CLI tools, CI/CD automation, quick prototyping. API keys are tied to a specific user and tenant, acting as that user with a scoped subset of their permissions.

### Existing Implementation

The API key system is fully implemented in the Identity module:

| Component | File | Purpose |
|-----------|------|---------|
| `IApiKeyService` | `Identity.Application/Interfaces/IApiKeyService.cs` | Interface: Create, Validate, List, Revoke |
| `RedisApiKeyService` | `Identity.Infrastructure/Services/RedisApiKeyService.cs` | Valkey-backed implementation with SHA256 hashing |
| `ApiKeyAuthenticationMiddleware` | `Identity.Infrastructure/Authorization/ApiKeyAuthenticationMiddleware.cs` | Pipeline middleware: X-Api-Key → ClaimsPrincipal |
| `ApiKeysController` | `Identity.Api/Controllers/ApiKeysController.cs` | REST API: `POST/GET/DELETE /api/v1/identity/auth/keys` |

### Lifecycle

```
Step 1: Developer registers as a Foundry user (Keycloak)
  → Standard OIDC registration, gets a user account

Step 2: Developer logs in via OIDC session (Path 2)
  → Needs an active session to create API keys

Step 3: Developer creates an API key
  POST /api/v1/identity/auth/keys
  Authorization: Bearer <user's JWT>
  {
    "name": "My Showcase Aggregator",
    "scopes": ["showcases.read", "inquiries.read"],
    "expiresAt": "2027-01-01T00:00:00Z"       // optional
  }

  Response (201 Created):
  {
    "keyId": "abc123def456",
    "apiKey": "sk_live_a1b2c3d4e5f6...",       // shown ONCE, never again
    "prefix": "sk_live_a1b2c3d4",              // first 16 chars, for identification
    "name": "My Showcase Aggregator",
    "scopes": ["showcases.read", "inquiries.read"],
    "expiresAt": "2027-01-01T00:00:00Z"
  }

Step 4: Developer stores the key securely
  → The full key (sk_live_...) is only returned at creation
  → Foundry stores only the SHA256 hash — the raw key cannot be recovered

Step 5: Developer uses the key in their app/script
  GET /api/v1/showcases
  X-Api-Key: sk_live_a1b2c3d4e5f6...
```

### Key Format and Storage

- **Format**: `sk_live_<32 random bytes, base64url encoded>` — total length approximately 52 characters
- **Prefix**: first 16 characters (`sk_live_` + first 8 chars of secret) — used for human identification in lists and logs
- **Storage**: Dual-write to PostgreSQL (durable) and Valkey (cache).
  - **PostgreSQL** (Identity schema): `ApiKeys` table stores key hash, metadata, user ID, tenant ID, scopes, timestamps. This is the source of truth.
  - **Valkey** (cache): Two entries per key for fast lookups:
    - `apikey:<hash>` → full metadata JSON (for validation lookups by key)
    - `apikey:id:<keyId>` → same metadata JSON (for management lookups by ID)
    - `apikeys:user:<userId>` → Redis set of keyIds (for listing all keys per user)
  - **Read path**: Valkey first → PostgreSQL fallback on cache miss → repopulate Valkey
  - **Write path**: PostgreSQL first (durable), then Valkey (cache). If Valkey write fails, key still exists in PostgreSQL.
- **TTL**: if the key has an expiration date, Valkey entries get a matching TTL. Keys without expiry live in Valkey indefinitely.
- **Durability guarantee**: API keys survive Valkey restarts. PostgreSQL is the durable store. Valkey is a performance optimization.

> **Implementation note**: The current implementation uses Valkey-only storage (`RedisApiKeyService`). PostgreSQL dual-write is a required enhancement tracked as `foundry-u49d`.

### Validation Flow

When a request arrives with an `X-Api-Key` header:

```
ApiKeyAuthenticationMiddleware:
  1. Extract key from X-Api-Key header
  2. Check format: must start with "sk_live_"
     → If not: return 401 { "detail": "Invalid API key format" }
  3. Compute SHA256 hash of the key
  4. Look up hash in Valkey: GET apikey:<hash>
     → If not found: return 401 { "detail": "API key not found" }
  5. Deserialize ApiKeyData from stored JSON
  6. Check expiration: if ExpiresAt < now
     → return 401 { "detail": "API key expired" }
  7. Fire-and-forget: update LastUsedAt timestamp in Valkey
  8. Create ClaimsPrincipal with claims:
     - ClaimTypes.NameIdentifier = UserId (the developer who created the key)
     - "sub" = UserId
     - "api_key_id" = KeyId
     - "auth_method" = "api_key"
     - "organization" = TenantId (the tenant the key is scoped to)
     - "scope" = each scope as a separate claim
  9. Set TenantContext: tenantSetter.SetTenant(TenantId, "api-key-{keyId}")
  10. Call next(context) — request proceeds as authenticated
```

### Scope Enforcement

At key creation time, every requested scope must map to a `PermissionType` that the user's current role grants. This prevents privilege escalation.

**Validation algorithm** (in `ApiKeysController.CreateApiKey`):
1. Get the user's current roles from their JWT `realm_access.roles` claim
2. Expand roles to permissions via `RolePermissionMapping.GetPermissions()`
3. For each requested scope, call `MapScopeToPermission()` to get the `PermissionType`
4. If any `PermissionType` is NOT in the user's expanded permissions, reject with 403

```
User role: manager (13 permissions, including ShowcasesRead, InquiriesRead)
  ↓
User creates key with scopes: ["showcases.read", "inquiries.read"]
  ↓ Each scope maps to a PermissionType the manager role includes ✓
  ↓
Key created successfully.

User creates key with scopes: ["billing.manage"]
  ↓ "billing.manage" maps to BillingManage
  ↓ Manager role does NOT include BillingManage ✗
  ↓
403: { "title": "Forbidden", "detail": "Scope 'billing.manage' exceeds your current permissions" }
```

**Role downgrade handling**: If a user's role is later downgraded (e.g., admin → user), existing API keys may have scopes that exceed the user's new permissions. Two enforcement mechanisms:

1. **Lazy enforcement (recommended)**: On key validation in `ApiKeyAuthenticationMiddleware`, check if the key's scopes still map to permissions the user currently holds. If not, reject with 403 and include the key ID in the error so the user knows which key to update.
2. **Admin audit**: Scheduled job (Hangfire) that periodically identifies keys whose scopes exceed their owner's current role. Flags them for admin review.

> **Implementation status**: Scope subset validation is NOT yet implemented. This is a P0 production blocker tracked as `foundry-hsp5`.

### Key Limits

Each user can create a maximum of **10 API keys** per tenant. This limit is configurable via `appsettings.json`:

```json
{
  "ApiKeys": {
    "MaxKeysPerUser": 10
  }
}
```

Exceeding the limit returns:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Maximum API key limit (10) reached. Revoke unused keys before creating new ones."
}
```

> **Implementation status**: Key limit is not yet implemented. Tracked as `foundry-s5hr`.

### Key Management

| Operation | Endpoint | Auth Required | Notes |
|-----------|----------|---------------|-------|
| Create | `POST /api/v1/identity/auth/keys` | User session (JWT) + `ApiKeyManage` permission | Returns full key once |
| List | `GET /api/v1/identity/auth/keys` | User session (JWT) + `ApiKeyManage` permission | Returns metadata only (prefix, name, scopes, dates) — never the raw key |
| Revoke | `DELETE /api/v1/identity/auth/keys/{keyId}` | User session (JWT) + `ApiKeyManage` permission | Immediate. Deletes from both PostgreSQL and Valkey. Next request with that key gets 401. |

**Revocation atomicity**: Revocation must delete the key from PostgreSQL first (durable), then Valkey (cache). The `UpdateLastUsedAsync` fire-and-forget task must check for key existence before writing to avoid a race condition where a concurrent `LastUsedAt` update re-creates a just-revoked key in Valkey. Implementation: use Valkey `SET ... NX` (set-if-not-exists) instead of `When.Always` in `UpdateLastUsedAsync`, or check a revoked flag.

> **Implementation status**: Race condition fix tracked as `foundry-5osr`.

**Admin capabilities** (future): Admins should be able to list and revoke any key in their tenant. This requires an additional endpoint that bypasses the user ownership check.

### Liability Model

- The key acts as the developer. All actions taken with the key are attributed to the developer's user ID.
- The developer is responsible for storing the key securely. Foundry shows it exactly once at creation.
- If the key is leaked, the blast radius is bounded:
  - Limited to the key's scoped permissions (a subset of the user's role)
  - Limited to one tenant (the tenant the key was created in)
  - Rate-limited independently (the attacker can't bypass limits)
- The developer (or an admin) can revoke the key immediately.
- Keys using the `sk_live_` prefix are detected by GitHub secret scanning, GitGuardian, and other tools. If detected in a public repo, the developer is notified.

### When to Use This Path

- Personal scripting against the API ("I want to pull my showcase data into a spreadsheet")
- CI/CD automation for your own tenant
- Quick prototyping before registering a full app via DCR
- CLI tooling for admin tasks
- Any case where *you personally* are the caller, not an app serving other users

### When NOT to Use This Path

- Building an app that other people will use (register a DCR service account — Path 1 Mode B)
- Your own BFF or mobile backend (use operator-provisioned DCR — Path 1 Mode A)
- Interactive browser sessions (use OIDC — Path 2)

**The rule of thumb**: If the thing calling the API is *you* → API key. If it's *an app you built* → service account.

---

## 6. The Open Ecosystem

Foundry is designed to be forked, extended, and consumed by external developers. The auth architecture supports this at three levels:

### Developer Journey (Your Instance)

```
Path A: "I want to quickly script against the API"
  1. Register as a user on your Foundry instance (Keycloak open registration)
  2. Log in, navigate to API key management
  3. Create an API key with desired scopes (validated against your role)
  4. Use X-Api-Key header in requests — done

  Time to first API call: minutes.

Path B: "I want to build an app that serves users"
  1. Register as a user on your Foundry instance (Keycloak open registration)
  2. Receive create-client role (auto-assigned or granted on request)
  3. Authenticate, get your JWT
  4. Call Foundry DCR proxy (`POST /api/v1/identity/apps/register`) with your JWT
  5. Get client_id (`app-*`) + client_secret for your app
  6. Build your app using client_credentials flow
  7. If your app needs user login:
     → Register a second Keycloak client for authorization_code + PKCE
     → Keycloak's Consent Required Policy shows users what they're authorizing
     → Your app exchanges authorization codes for user JWTs
     → Forward user JWTs to Foundry API for user-context requests

  Time to first API call: minutes (dev), hours with admin approval (prod).

Path C: "I operate my own Foundry instance"
  1. Fork the Foundry repo
  2. Deploy with your own Keycloak + Postgres + Valkey + RabbitMQ
  3. Configure your realm, DCR policies, rate limits, user registration
  4. Your users follow Path A or B against YOUR instance
  5. Add modules, change permissions, modify rate limits — your instance, your rules

  Same code, independent data and access control.
```

### What Third-Party Developers CAN Do

- Read showcases, submit inquiries, access any endpoint their scopes allow
- Build web apps, mobile apps, CLI tools, bots, integrations in any language
- Register multiple apps (up to the Max Clients Policy limit)
- Manage their own clients via `registration_access_token` (rotate secrets, update config, delete)
- Create multiple API keys with different scope combinations for different use cases

### What Third-Party Developers CANNOT Do

- Access endpoints beyond their scoped permissions
- Impersonate other users or tenants
- Exceed rate limits (per-key for API keys, per-client for service accounts)
- Register DCR service accounts without the `create-client` role
- Assign scopes to their own clients beyond what Client Scope Policy allows
- Bypass the `sa-` prefix convention (failing to use it results in zero permissions, not elevated access)

### The Ecosystem Topology

```
┌─────────────────────────────────────────────────────────────────┐
│  Your Foundry Instance                                           │
│                                                                  │
│  Operator apps (Mode A — Initial Access Token):                  │
│  ┌─────────────────┐                                             │
│  │ sa-personal-site│  Your BFF. Full scopes assigned by admin.  │
│  │ sa-mobile-app   │  Your mobile backend. Admin-scoped.        │
│  │ sa-admin-tool   │  Internal tooling. Admin-scoped.           │
│  └─────────────────┘                                             │
│                                                                  │
│  Developer apps (Mode B — Foundry DCR proxy, app-* prefix):      │
│  ┌──────────────────────┐                                        │
│  │ app-cool-viewer      │  Community dev. Policy-restricted.     │
│  │ app-showcase-bot     │  Another dev. showcases.read only.     │
│  │ app-inquiry-cli      │  CLI tool. inquiries.write only.       │
│  └──────────────────────┘                                        │
│                                                                  │
│  Developer API keys (personal access):                           │
│  ┌──────────────────────────────────────────────────────┐       │
│  │ sk_live_... (dev1, [showcases.read])                 │       │
│  │ sk_live_... (dev2, [inquiries.write, inquiries.read])│       │
│  │ sk_live_... (admin, [all scopes])                    │       │
│  └──────────────────────────────────────────────────────┘       │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Forked Instances

```
┌──────────────────────────────────┐     ┌──────────────────────────────────┐
│  Your Instance                    │     │  Their Instance (fork)            │
│  ┌────────┐ ┌─────────┐         │     │  ┌────────┐ ┌─────────┐         │
│  │Keycloak│ │Foundry  │         │     │  │Keycloak│ │Foundry  │         │
│  │(yours) │ │API      │         │     │  │(theirs)│ │API      │         │
│  └────────┘ └─────────┘         │     │  └────────┘ └─────────┘         │
│  Own users, own keys, own apps   │     │  Own users, own keys, own apps   │
│  Own DCR policies, own rate limits│     │  Own policies, own rate limits   │
│  Own realm, own scopes           │     │  May add custom modules + scopes │
└──────────────────────────────────┘     └──────────────────────────────────┘
        Completely independent. Same code, separate worlds.
```

### User Registration

User registration is handled entirely by Keycloak. Foundry does not implement custom registration logic — no password handling, no email verification, no account creation endpoints. Keycloak provides:

- Self-registration (configurable: open or invite-only)
- Email verification
- Password policies (length, complexity, history)
- Social login (Google, GitHub, etc.) via identity provider federation
- Multi-factor authentication (TOTP, WebAuthn)
- Account linking (merge social + password accounts)
- Brute-force detection and account lockout

All of these are configured in the Keycloak Admin Console, not in Foundry code. Foundry trusts the JWT that Keycloak issues — if the user authenticated through Keycloak, Foundry accepts them.

> **Future work: Lightweight user creation** — For flows like inquiry submission where a visitor provides their email without registering, Foundry may create a lightweight Keycloak user via the Admin API (email only, no password). This user can later set a password via Keycloak's "forgot password" flow, at which point all their inquiries are already linked. This flow is not yet designed or implemented.

> **Future work: Mobile OIDC** — Native mobile apps that need user login use `authorization_code + PKCE` as a public Keycloak client (separate from the mobile backend's `sa-*` service account). The mobile backend handles server-to-server calls; the mobile app handles user authentication. A dedicated mobile integration guide will be created after the core auth architecture is implemented.

**Why this works for open source:**
- The code is freely available and forkable. No hardcoded secrets, no phone-home, no license server.
- Each operator controls their own access policies, rate limits, and user registration.
- The auth architecture is the same everywhere — developers who learn one Foundry instance can build against any other.
- A fork operator can add modules, change permission mappings, modify rate limits, add new scopes — it's their instance.
- API keys use the universal `sk_live_` prefix for secret scanner compatibility across all instances.

---

## 7. Rate Limiting

Rate limiting operates in three layers. Each catches a different abuse pattern.

### Layer 1: Global Rate Limiter (ASP.NET Core Built-in)

Applied first in the middleware pipeline, before authentication. Configured in `src/Foundry.Api/Extensions/RateLimitDefaults.cs`:

| Policy | Limit | Window | Applied To | Purpose |
|--------|-------|--------|------------|---------|
| **Global** | 1,000 requests | 1 hour | Per authenticated identity | General abuse prevention |
| **Auth** | 3 requests | 10 minutes | Login/token endpoints | Brute-force login protection |
| **Upload** | 10 requests | 1 hour | File upload endpoints | Storage abuse prevention |
| **SCIM** | 30 requests | 1 minute | SCIM provisioning endpoints | Directory sync rate control |

Identity resolution for the global rate limiter:
- API key requests → `api_key_id` claim
- Service account requests → `azp` claim (client ID, e.g., `sa-personal-site`)
- User session requests → `sub` claim (Keycloak user ID)

A user with 3 API keys gets 3 independent rate limit quotas (1,000 req/hr each). A service account and a user session for the same person are also independent quotas.

### Layer 2: Per-API-Key Rate Limiter

After API key authentication resolves the identity, a flat per-key limit applies. Same limit for all keys: 1,000 req/hr, enforced via Valkey sliding window counter keyed by `api_key_id`.

This layer is specific to API keys. Service accounts and user sessions rely on the global rate limiter (Layer 1).

### Layer 3: Module-Specific Rate Limits

Individual modules enforce their own limits on top of global limits. These apply regardless of auth path.

Currently implemented:

| Module | Limit | Window | Key | Storage | Implementation |
|--------|-------|--------|-----|---------|----------------|
| **Inquiries** | 5 submissions | 15 minutes | Per caller identity | Valkey atomic counter with TTL | `ValkeyRateLimitService.cs` |

The inquiry rate limiter uses a simple algorithm:
1. `INCR` counter for key in Valkey
2. If counter == 1, set TTL to 15 minutes
3. Allow if counter ≤ 5

Module rate limits are enforced within the module's command handlers, not in the middleware pipeline. This means they apply after authentication and authorization have already passed.

### Layer 4: DCR Registration Rate Limiter

The Foundry DCR proxy endpoint (`POST /api/v1/identity/apps/register`) applies its own rate limit:

| Limit | Window | Key | Purpose |
|-------|--------|-----|---------|
| 5 registrations | 1 hour | Per authenticated user ID | Prevents abuse of client registration |

This limit applies only to developer self-service registration (Mode B, `app-*` prefix). Operator-provisioned registration (Mode A, `sa-*` prefix) goes directly to Keycloak with an Initial Access Token and is inherently limited by token consumption (one token = one client).

> **Implementation status**: DCR proxy with rate limiting is not yet implemented. Tracked as `foundry-i5y6`.

### How the Layers Stack

```
Request arrives
  │
  ▼
Layer 1: Global rate limiter
  1,000/hr per identity (pre-auth for volumetric, post-auth for identity-based)
  → 429 Too Many Requests if exceeded
  │ passes
  ▼
API Key auth (if X-Api-Key present)
  │
  ▼
Layer 2: Per-key rate limiter (API key requests only)
  1,000/hr per api_key_id
  → 429 Too Many Requests if exceeded
  │ passes
  ▼
JWT auth → Tenant resolution → Permission expansion → Authorization
  │ passes
  ▼
Controller action
  │
  ▼
Layer 3: Module-specific rate limiter (within command handler)
  e.g., Inquiries: 5 submissions/15min per caller
  → 429 or module-specific error if exceeded
  │ passes
  ▼
Action executes successfully
```

---

## 8. Permission Model

Two permission assignment systems exist, unified by `PermissionExpansionMiddleware` into the same `permission` claims on `ClaimsPrincipal`. All downstream authorization is identical regardless of auth path.

### Role-Based Permissions (User Sessions)

Keycloak assigns roles to users at the realm level. `PermissionExpansionMiddleware` reads the `realm_access.roles` claim and expands each role to a set of permissions via `RolePermissionMapping.GetPermissions()`.

**Role-to-permission mapping** (source of truth: `RolePermissionMapping.cs`):

| Role | Permissions (47/13/9) |
|------|----------------------|
| **admin** | UsersRead, UsersCreate, UsersUpdate, UsersDelete, RolesRead, RolesCreate, RolesUpdate, RolesDelete, BillingRead, BillingManage, InvoicesRead, InvoicesWrite, PaymentsRead, PaymentsWrite, SubscriptionsRead, SubscriptionsWrite, OrganizationsRead, OrganizationsCreate, OrganizationsUpdate, OrganizationsManageMembers, ApiKeysRead, ApiKeysCreate, ApiKeysUpdate, ApiKeysDelete, NotificationsRead, NotificationsWrite, WebhooksManage, SsoRead, SsoManage, ScimManage, AdminAccess, SystemSettings, ConfigurationRead, ConfigurationManage, NotificationRead, EmailPreferenceManage, MessagingAccess, AnnouncementRead, AnnouncementManage, ChangelogManage, StorageRead, StorageWrite, ApiKeyManage, ShowcasesRead, ShowcasesManage, ScopeRead, ServiceAccountsRead, ServiceAccountsWrite, ServiceAccountsManage, PushRead, PushConfigWrite, InquiriesRead, InquiriesWrite |
| **manager** | UsersRead, BillingRead, OrganizationsRead, OrganizationsManageMembers, ApiKeysRead, ApiKeysCreate, ApiKeysUpdate, ApiKeysDelete, SsoRead, ConfigurationManage, ShowcasesRead, ShowcasesManage, InquiriesRead |
| **user** | OrganizationsRead, MessagingAccess, NotificationRead, EmailPreferenceManage, AnnouncementRead, StorageRead, StorageWrite, ShowcasesRead, InquiriesWrite |

Roles are cumulative. A user with both `admin` and `manager` gets the deduplicated union.

### Scope-Based Permissions (Service Accounts + API Keys)

Service accounts and API keys carry `scope` claims (space-separated OAuth2 scopes). `PermissionExpansionMiddleware` maps each scope to a single `PermissionType` via `MapScopeToPermission()`.

**Complete scope-to-permission mapping** (source of truth: `PermissionExpansionMiddleware.cs`):

| Scope | PermissionType | Module |
|-------|---------------|--------|
| `billing.read` | BillingRead | Billing |
| `billing.manage` | BillingManage | Billing |
| `invoices.read` | InvoicesRead | Billing |
| `invoices.write` | InvoicesWrite | Billing |
| `payments.read` | PaymentsRead | Billing |
| `payments.write` | PaymentsWrite | Billing |
| `subscriptions.read` | SubscriptionsRead | Billing |
| `subscriptions.write` | SubscriptionsWrite | Billing |
| `users.read` | UsersRead | Identity |
| `users.write` | UsersUpdate | Identity |
| `users.manage` | UsersDelete | Identity |
| `roles.read` | RolesRead | Identity |
| `roles.write` | RolesUpdate | Identity |
| `roles.manage` | RolesDelete | Identity |
| `organizations.read` | OrganizationsRead | Identity |
| `organizations.write` | OrganizationsUpdate | Identity |
| `organizations.manage` | OrganizationsManageMembers | Identity |
| `apikeys.read` | ApiKeysRead | Identity |
| `apikeys.write` | ApiKeysUpdate | Identity |
| `apikeys.manage` | ApiKeyManage | Identity |
| `sso.read` | SsoRead | Identity |
| `sso.manage` | SsoManage | Identity |
| `scim.manage` | ScimManage | Identity |
| `storage.read` | StorageRead | Storage |
| `storage.write` | StorageWrite | Storage |
| `messaging.access` | MessagingAccess | Messaging |
| `announcements.read` | AnnouncementRead | Announcements |
| `announcements.manage` | AnnouncementManage | Announcements |
| `changelog.manage` | ChangelogManage | Announcements |
| `notifications.read` | NotificationsRead | Notifications |
| `notifications.write` | NotificationsWrite | Notifications |
| `configuration.read` | ConfigurationRead | Identity |
| `configuration.manage` | ConfigurationManage | Identity |
| `showcases.read` | ShowcasesRead | Showcases |
| `showcases.manage` | ShowcasesManage | Showcases |
| `inquiries.read` | InquiriesRead | Inquiries |
| `inquiries.write` | InquiriesWrite | Inquiries |
| `serviceaccounts.read` | ServiceAccountsRead | Identity |
| `serviceaccounts.write` | ServiceAccountsWrite | Identity |
| `serviceaccounts.manage` | ServiceAccountsManage | Identity |
| `webhooks.manage` | WebhooksManage | Platform |

Unknown scopes in a JWT are silently ignored by the middleware. They do not cause errors and do not grant permissions.

### How Scopes Get Assigned

| Auth Path | Scope Source | Who Controls | Enforcement |
|-----------|-------------|--------------|-------------|
| Operator-provisioned service account | Keycloak Admin Console or Foundry API (`PUT /service-accounts/{id}/scopes`) | Platform operator | Manual. Admin assigns any scope. |
| Developer-registered service account | Keycloak Client Scope Policy | Policy whitelist. Admin configures which scopes are available to DCR clients. | Automatic. Keycloak rejects scopes not in the whitelist. |
| API key | Selected at creation via `POST /api/v1/identity/auth/keys` | Developer, with validation. | Required: scopes must be a subset of the user's current role permissions. Prevents privilege escalation. |

### The Subset Constraint for API Keys

```
User role: manager
  → Permissions include: ShowcasesRead, ShowcasesManage, InquiriesRead, ...
  → Scopes that map to these: showcases.read, showcases.manage, inquiries.read, ...

API key creation request: scopes = ["showcases.read", "inquiries.read"]
  → showcases.read maps to ShowcasesRead → manager has ShowcasesRead ✓
  → inquiries.read maps to InquiriesRead → manager has InquiriesRead ✓
  → Key created successfully.

API key creation request: scopes = ["billing.manage"]
  → billing.manage maps to BillingManage → manager does NOT have BillingManage ✗
  → Key creation rejected: 403 "Scope exceeds your permissions"
```

### Authorization Enforcement

All three auth paths converge at the same point. Controllers use `[HasPermission(PermissionType.X)]` attribute:

```csharp
[HasPermission(PermissionType.ShowcasesRead)]
public async Task<IActionResult> GetShowcases()
```

`PermissionAuthorizationHandler` checks if the `ClaimsPrincipal` has a `permission` claim matching the required `PermissionType`:
- Claim exists → 200 (action executes)
- Claim missing → 403 Forbidden

No auth path gets special treatment. A service account with `showcases.read` scope, a user with `manager` role, and an API key with `showcases.read` scope all pass the same check.

### Adding New Permissions

When adding a new module or new endpoints to an existing module, three files must be updated together:

1. **`PermissionType.cs`** (`Shared.Kernel`) — add the new permission constant(s)
2. **`RolePermissionMapping.cs`** (`Identity.Infrastructure`) — assign the permission to the appropriate role tiers
3. **`PermissionExpansionMiddleware.cs`** (`Identity.Infrastructure`) — add the scope-to-permission mapping in `MapScopeToPermission()`

Plus Keycloak configuration:
4. **`realm-export.json`** — add the client scope definition (so it can be assigned to clients)
5. **`ApiScopes.cs`** (`Identity.Application`) — add the scope constant to `ValidScopes` (for API key creation validation)

All five must be updated in the same PR. An unmapped scope in a JWT is silently ignored. A scope in `ApiScopes.ValidScopes` that has no `MapScopeToPermission` entry would pass API key creation validation but grant zero permissions at runtime — a silent failure.

---

## 9. Security Guarantees

### No Anonymous Access

Every request must authenticate via one of three paths. An unauthenticated request receives 401 before reaching any controller.

**Known exception**: `ShowcasesController.GetAll` and `ShowcasesController.GetById` currently have `[AllowAnonymous]`. This contradicts the auth architecture and must be removed. Showcases should require authentication — public showcase access goes through the BFF's service account, not anonymous access. Tracked as `foundry-oopm`.

Even "public-facing" content like showcases requires authentication. The difference is who authenticates:
- A visitor browsing your site → the BFF's service account authenticates on their behalf
- A third-party app displaying showcases → the app's service account authenticates
- A developer pulling data → their API key authenticates

The visitor doesn't know or care. But Foundry always knows which app or person is calling.

### Every Request Traces to an Identity

| Auth Path | Identity | Identifier | Audit Trail |
|-----------|----------|------------|-------------|
| Service account (your app) | The app | `azp` = `sa-personal-site` | `ServiceAccountMetadata.LastUsedAt` in PostgreSQL |
| Service account (third-party) | The app + its developer | `azp` = `app-cool-viewer` | `ServiceAccountMetadata.LastUsedAt` + Keycloak audit log shows who registered it |
| User session | The human | `sub` = Keycloak user UUID | Keycloak audit log + Serilog structured logs (UserId in LogContext) |
| API key | The developer | `api_key_id` + `sub` = creating user UUID | `ApiKeyData.LastUsedAt` in Valkey + Serilog structured logs |
| SCIM token | The tenant's directory | `tenant_id` claim | `ScimSyncLog` table in PostgreSQL |

### Credential Leak Blast Radius

| Credential | If Leaked | Blast Radius | Recovery |
|------------|-----------|--------------|----------|
| Service account `client_secret` | Attacker gets the app's scopes | Limited to assigned scopes. Tenant-agnostic (uses `TenantId.Platform`) unless `X-Tenant-Id` is used. | Rotate via `registration_access_token` (single-use, returns new secret). Or revoke client entirely via Keycloak Admin Console. |
| API key `sk_live_*` | Attacker acts as the developer | Limited to key's scopes (subset of user's role). Scoped to one tenant. Rate-limited per key. | Developer revokes via `DELETE /api/v1/identity/auth/keys/{keyId}`. Admin can also revoke. Immediate effect (Valkey deletion). |
| User JWT (access token) | Attacker has a user session | Limited to user's role permissions within their tenant. Short-lived (default: 5 minutes). | Token expires naturally. Revoke refresh token to prevent renewal. Admin can end all sessions in Keycloak. |
| User JWT (refresh token) | Attacker can get new access tokens | Can generate new access tokens until refresh token expires (default: 30 minutes) or is revoked. | Admin revokes via Keycloak Admin Console (Sessions → Revoke). User logs out (invalidates refresh token). |
| Initial Access Token | Attacker can register one client | Client starts with zero functional scopes. `Full Scope Allowed` disabled by default. Needs admin to assign scopes to be useful. | Token is single-use — if already consumed, it's gone. Time-limited — if expired, it's useless. Admin deletes orphaned client. |
| `registration_access_token` | Attacker can modify one client | Can update or delete that specific client only. Token rotates on each use — attacker's copy is invalidated after one use. | Admin regenerates token via Keycloak Admin Console (`Clients → {client} → Registration Access Token`). |
| SCIM Bearer token | Attacker can provision users in one tenant | Limited to SCIM operations (user/group sync) within the specific tenant whose ScimConfiguration matched. | Admin regenerates token. Old token hash no longer matches. |

### Fail-Safe Defaults

Every security boundary is designed to fail toward LESS access, never more:

- **Missing `sa-` prefix on client ID**: `PermissionExpansionMiddleware` falls into role-based expansion. Service accounts have no realm roles → zero permissions → 403 on every request.
- **DCR-registered clients**: `Full Scope Allowed` is disabled by default in Keycloak → clients only get explicitly assigned scopes → zero functional scopes until admin intervenes.
- **API keys with no scopes**: No `scope` claims → `ExpandServiceAccountScopes` maps nothing → zero permission claims → 403 on every request.
- **API keys with scopes exceeding user's role**: (With the required subset enforcement) creation is rejected → key is never issued.
- **Unknown scope in JWT**: `MapScopeToPermission` returns `null` → scope is silently ignored → no permission granted.
- **Expired API key**: Valkey TTL deletes the entry → validation lookup returns null → 401.
- **Rate limiting identity**: per `api_key_id`, `azp`, or `sub` — sharing credentials doesn't bypass limits because the identity stays the same.

### Secret Scanning Compatibility

API keys use the `sk_live_` prefix, which is recognized by:
- GitHub secret scanning (automatic push protection)
- GitGuardian
- TruffleHog
- Other secret detection tools that match the Stripe-style `sk_live_` pattern

If a key is detected in a public repository, the developer is notified by the scanning tool. The key is tied to their account — liability is unambiguous.

### Assumptions (Explicitly Documented)

These are foundational assumptions that the entire auth architecture depends on:

1. **HTTPS is mandatory in production.** All token and key transmission must be over TLS. Without HTTPS, JWTs and API keys are visible in transit.
2. **Valkey is trusted infrastructure.** API key hashes and cached data are stored unencrypted. Valkey must not be exposed to untrusted networks.
3. **Keycloak is the trust root.** JWT signatures are verified against Keycloak's public keys. If Keycloak is compromised, all authentication is compromised.
4. **Clock synchronization.** JWT expiry checking depends on server clocks being synchronized (NTP). Clock skew > 5 minutes will cause valid tokens to be rejected or expired tokens to be accepted.
5. **The `sa-` and `app-` prefixes are a social contract for operator vs developer clients.** Enforcement is via Foundry's DCR proxy (for `app-*`) and Initial Access Token scarcity (for `sa-*`), not cryptographic proof.

---

## 9.5. Error Response Specification

All authentication and authorization errors follow [RFC 7807 Problem Details](https://tools.ietf.org/html/rfc7807) format. Every error response includes `Content-Type: application/problem+json`.

### Authentication Errors (401 Unauthorized)

| Scenario | Auth Path | Response Body |
|----------|-----------|---------------|
| No auth header, no API key | Any | `{ "type": "https://tools.ietf.org/html/rfc7235#section-3.1", "title": "Unauthorized", "status": 401, "detail": "No authentication credentials provided" }` |
| Invalid JWT signature | User session / Service account | `{ "title": "Unauthorized", "status": 401, "detail": "Bearer token validation failed" }` (ASP.NET Core default) |
| Expired JWT | User session / Service account | `{ "title": "Unauthorized", "status": 401, "detail": "The token expired at '...'" }` (ASP.NET Core default) |
| Wrong audience (`aud` missing `foundry-api`) | User session / Service account | `{ "title": "Unauthorized", "status": 401, "detail": "The audience '...' is invalid" }` |
| Invalid API key format | API key | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1", "title": "Unauthorized", "status": 401, "detail": "Invalid API key format" }` |
| API key not found (hash miss) | API key | `{ "title": "Unauthorized", "status": 401, "detail": "API key not found" }` |
| API key expired | API key | `{ "title": "Unauthorized", "status": 401, "detail": "API key expired" }` |
| Invalid SCIM token | SCIM | `{ "status": 401, "scimType": "invalidCredentials", "detail": "Invalid or expired SCIM token" }` (SCIM JSON format) |

### Authorization Errors (403 Forbidden)

| Scenario | Response Body |
|----------|---------------|
| Missing required permission | `{ "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3", "title": "Forbidden", "status": 403, "detail": "Insufficient permissions. Required: ShowcasesManage" }` |
| API key scope exceeds user's permissions (lazy check) | `{ "title": "Forbidden", "status": 403, "detail": "API key 'abc123' has scope 'billing.manage' which exceeds the owner's current permissions. Revoke or update the key." }` |
| API key creation — scope exceeds permissions | `{ "title": "Forbidden", "status": 403, "detail": "Scope 'billing.manage' exceeds your current permissions" }` |
| API key creation — max keys reached | `{ "title": "Forbidden", "status": 403, "detail": "Maximum API key limit (10) reached. Revoke unused keys before creating new ones." }` |

### Rate Limiting Errors (429 Too Many Requests)

| Scenario | Response Headers | Response Body |
|----------|-----------------|---------------|
| Global rate limit exceeded | `Retry-After: <seconds>` | `{ "title": "Too Many Requests", "status": 429, "detail": "Rate limit exceeded. Try again in <N> seconds." }` |
| Per-key rate limit exceeded | `Retry-After: <seconds>` | Same format |
| Module-specific limit (e.g., inquiries) | Varies by module | Module-specific error response |
| DCR registration limit exceeded | `Retry-After: <seconds>` | `{ "title": "Too Many Requests", "status": 429, "detail": "App registration rate limit exceeded. Maximum 5 registrations per hour." }` |

### Validation Errors (400 Bad Request)

| Scenario | Response Body |
|----------|---------------|
| API key name missing | `{ "title": "Invalid request", "status": 400, "detail": "API key name is required" }` |
| Invalid scopes in API key creation | `{ "title": "Invalid scopes", "status": 400, "detail": "The following scopes are not valid: foo.bar, baz.qux" }` |
| Tenant context missing | `{ "title": "Tenant required", "status": 400, "detail": "You must be associated with an organization to create API keys" }` |
| DCR registration — missing `app-` prefix | `{ "title": "Invalid client ID", "status": 400, "detail": "Developer app client IDs must start with 'app-'" }` |

---

## 10. For Platform Operators (Fork Guide)

When you fork Foundry and deploy your own instance, here's what you need to configure.

### Keycloak Configuration

| Configuration | Where | What to Change | Default |
|---------------|-------|----------------|---------|
| Realm name | `docker/keycloak/realm-export.json` | Rename from `foundry` if desired. Update all references in realm export and `appsettings.json`. | `foundry` |
| Audience mapper | `foundry-api-audience` client scope in realm export | Change `included.client.audience` if you rename the API client. Must match `Authentication:Audience` in `appsettings.json`. | `foundry-api` |
| DCR policies | `docker/keycloak/configure-dcr.sh` | Adjust trusted hosts (your domains), max clients limit, add Client Disabled Policy for prod. | Trusted hosts: localhost/127.0.0.1. Max clients: 100. |
| Initial Access Tokens | Keycloak Admin Console (`Clients → Initial Access Token`) | Create tokens for your own apps. Set expiry (recommended: 24h) and max client count per token (recommended: 1). | None created by default. |
| `create-client` role | Keycloak Admin Console (`Realm Roles`) | Decide: auto-assign to all registered users via default roles (open ecosystem) or grant on request (controlled ecosystem). | Not auto-assigned. |
| Client Scope Policy | Keycloak Admin Console (`Realm Settings → Client Registration → Client Registration Policies`) | Whitelist which scopes developer-registered apps can request. Block admin/billing scopes for untrusted developers. | No restrictions (all scopes available). |
| Client Disabled Policy | Keycloak Admin Console (same location) | Enable for production to require admin approval of new DCR clients. | Disabled (clients are active immediately). |
| User registration | Keycloak Admin Console (`Realm Settings → Login`) | Enable/disable self-registration. Configure required fields (email verification, CAPTCHA). | Enabled (dev mode). |
| Token lifetimes | Keycloak Admin Console (`Realm Settings → Tokens`) | Access token lifespan, refresh token lifespan, SSO session max. | Access: 5min, Refresh: 30min. |
| Login theme | Keycloak Admin Console (`Realm Settings → Themes`) | Brand the login page for your instance. | Default Keycloak theme. |

### Foundry Application Configuration

| Configuration | File / Location | What to Change | Default |
|---------------|-----------------|----------------|---------|
| JWT audience validation | `appsettings.json` → `Authentication:Audience` | Must match the audience mapper value in Keycloak. | `foundry-api` |
| Keycloak authority URL | `appsettings.json` → `Authentication:Authority` | Your Keycloak base URL + realm. | `http://localhost:8080/realms/foundry` |
| Global rate limit | `RateLimitDefaults.cs` → `GlobalPermitLimit` | Adjust for expected traffic. | 1,000 req/hr |
| Auth rate limit | `RateLimitDefaults.cs` → `AuthPermitLimit` | Brute-force protection for login. | 3 req/10min |
| Upload rate limit | `RateLimitDefaults.cs` → `UploadPermitLimit` | Storage abuse prevention. | 10 req/hr |
| SCIM rate limit | `RateLimitDefaults.cs` → `ScimPermitLimit` | Directory sync throttle. | 30 req/min |
| Inquiry rate limit | `ValkeyRateLimitService.cs` → `MaxRequests` / `_window` | Module-specific submission throttle. | 5 req/15min |
| Role permissions | `RolePermissionMapping.cs` | Add/remove permissions per role tier. This is the single source of truth for user authorization. | 3 tiers: admin (47), manager (13), user (9) |
| Scope mappings | `PermissionExpansionMiddleware.cs` → `MapScopeToPermission()` | Add mappings when creating new modules or scopes. Must stay in sync with Keycloak client scopes and `ApiScopes.ValidScopes`. | 40 mappings across all modules |
| API key format | `RedisApiKeyService.cs` | `sk_live_` prefix, SHA256 hashing, 32-byte random secret. Change prefix if desired (impacts secret scanner compatibility). | `sk_live_` |
| Valkey connection | `appsettings.json` → `ConnectionStrings:Redis` | Your Valkey/Redis instance for API key storage and rate limiting. | `localhost:6379` |

### Adding a New Module's Permissions

Five files must be updated together (same PR):

1. **`src/Shared/Foundry.Shared.Kernel/Identity/Authorization/PermissionType.cs`** — add new permission constants (e.g., `public const string MyModuleRead = "MyModuleRead";`)
2. **`src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/RolePermissionMapping.cs`** — assign new permissions to role tiers (admin gets all, manager/user get subsets as appropriate)
3. **`src/Modules/Identity/Foundry.Identity.Infrastructure/Authorization/PermissionExpansionMiddleware.cs`** — add scope-to-permission mapping in `MapScopeToPermission()` (e.g., `"mymodule.read" => PermissionType.MyModuleRead`)
4. **`docker/keycloak/realm-export.json`** — add client scope definition (so it can be assigned to clients in Keycloak)
5. **`src/Modules/Identity/Foundry.Identity.Application/Constants/ApiScopes.cs`** — add scope to `ValidScopes` (for API key creation validation)

### Pre-Configured vs DCR Clients

| Approach | When to Use | How |
|----------|-------------|-----|
| Pre-configured in realm export | Your primary apps that exist on every deployment | Define client in `realm-export.json` with scopes pre-assigned as `defaultClientScopes` |
| DCR with Initial Access Token | Additional operator apps deployed ad-hoc | Create token in Keycloak Admin Console, register via DCR endpoint |
| DCR with Bearer Token | Third-party developer apps | Configure `create-client` role and Client Scope Policy, developers self-register |

### Production Hardening Checklist

```
Keycloak:
[ ] Remove trusted hosts policy for localhost (no anonymous DCR)
[ ] Require Initial Access Tokens for operator app registration
[ ] Enable Client Disabled Policy (admin approval for developer-registered apps)
[ ] Enable email verification for user registration
[ ] Set access token lifespan to 5 minutes
[ ] Set refresh token lifespan to 30 minutes
[ ] Configure Client Scope Policy to whitelist allowed DCR scopes
[ ] Set Max Clients Policy to appropriate limit
[ ] Enable brute-force detection (Realm Settings → Security Defenses)
[ ] Configure password policy (Realm Settings → Authentication → Password Policy)
[ ] Review and restrict CORS origins on the realm

Foundry:
[ ] Review rate limits for expected traffic volume
[ ] Configure Valkey persistence (RDB or AOF) for API key durability
[ ] Set up monitoring for stale service accounts (LastUsedAt > 90 days)
[ ] Set up monitoring for stale API keys (LastUsedAt > 90 days)
[ ] Implement API key scope subset validation (current implementation gap)
[ ] Configure structured logging export (Serilog → your log aggregator)
[ ] Set up OpenTelemetry export for request metrics and traces
[ ] Review CORS policy in Program.cs for production origins
[ ] Ensure HTTPS is enforced (reverse proxy or Kestrel TLS)

Infrastructure:
[ ] Valkey: configure maxmemory and eviction policy (allkeys-lru recommended)
[ ] PostgreSQL: enable SSL connections
[ ] RabbitMQ: configure TLS and credential rotation
[ ] Keycloak: deploy behind reverse proxy with TLS
[ ] All services: use non-default credentials (override docker/.env values)
```

---

## References

- [RFC 7591 — OAuth 2.0 Dynamic Client Registration](https://datatracker.ietf.org/doc/html/rfc7591)
- [Keycloak Client Registration Service](https://www.keycloak.org/securing-apps/client-registration)
- [Keycloak Client Scopes](https://www.keycloak.org/docs/latest/server_admin/#_client_scopes)
- [Keycloak Admin REST API](https://www.keycloak.org/docs-api/latest/rest-api/index.html)
- [OWASP API Security Top 10](https://owasp.org/API-Security/)
- [RFC 6749 — OAuth 2.0 Authorization Framework](https://datatracker.ietf.org/doc/html/rfc6749)
- [RFC 7636 — PKCE](https://datatracker.ietf.org/doc/html/rfc7636)

## Related Documents

- **DCR Implementation Plan**: `docs/superpowers/plans/2026-03-16-dynamic-client-registration.md`
- **DCR Design Spec**: `docs/superpowers/specs/2026-03-16-dynamic-client-registration-design.md`
- **Developer Guide**: `docs/DEVELOPER_GUIDE.md`
- **Module Creation Guide**: `docs/claude/module-creation.md`
