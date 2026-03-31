# Reverse Proxy Deployment

Wallow supports deployment behind a reverse proxy using path-based routing. All three apps share a single domain — no subdomains required. TLS terminates at the proxy; each app runs plain HTTP internally.

---

## Table of Contents

1. [Path-Based Routing Overview](#1-path-based-routing-overview)
2. [Required Configuration Per App](#2-required-configuration-per-app)
3. [TLS Termination](#3-tls-termination)
4. [Forwarded Headers](#4-forwarded-headers)
5. [Health Check Endpoints](#5-health-check-endpoints)
6. [WebSocket Support for Blazor SignalR](#6-websocket-support-for-blazor-signalr)
7. [Proxy Configuration Examples](#7-proxy-configuration-examples)

---

## 1. Path-Based Routing Overview

Route incoming requests to each app based on path prefix:

| Public path | Internal target | App `PathBase` |
|-------------|-----------------|----------------|
| `/api/*` | `http://localhost:5001` | `/api` |
| `/auth/*` | `http://localhost:5002` | `/auth` |
| `/*` | `http://localhost:5003` | *(none)* |

The `PathBase` setting tells ASP.NET Core to strip the prefix before matching routes. Without it, `/api/v1/identity/users` would fail to match because the router only sees the path after `PathBase` is removed.

**Routing precedence:** The `/api` and `/auth` prefixes must be evaluated before the catch-all `/*` rule. Proxy configurations below show how to express this correctly for each tool.

---

## 2. Required Configuration Per App

Set these environment variables (or `appsettings.json` entries) for each app when running behind a proxy.

### Wallow.Api

```bash
# Strip /api prefix before ASP.NET Core route matching
PathBase=/api

# The public-facing base URL including the path prefix
# Used by Auth to construct redirect URLs
ServiceUrls__ApiUrl=https://example.com/api

# CORS must allow the Web app's public origin
Cors__AllowedOrigins__0=https://example.com
```

### Wallow.Auth

```bash
# Strip /auth prefix before Blazor route matching
PathBase=/auth

# Public URL of the API (used for OIDC and redirect construction)
ApiBaseUrl=https://example.com/api
```

### Wallow.Web

```bash
# Web is served at the root — no PathBase needed
# PathBase=   (leave empty or omit)

# OIDC authority must point to the public API URL
Oidc__Authority=https://example.com/api

# Pre-registered client redirect URIs must match actual public URLs
# Set these in appsettings.json or as environment variables:
PreRegisteredClients__Clients__0__RedirectUris__0=https://example.com/signin-oidc
PreRegisteredClients__Clients__0__PostLogoutRedirectUris__0=https://example.com/signout-callback-oidc
```

### Full configuration reference

| Setting | Wallow.Api | Wallow.Auth | Wallow.Web |
|---------|-----------|------------|-----------|
| `PathBase` | `/api` | `/auth` | *(unset)* |
| `ServiceUrls__ApiUrl` | `https://example.com/api` | — | — |
| `ApiBaseUrl` | — | `https://example.com/api` | — |
| `Oidc__Authority` | — | — | `https://example.com/api` |

> **Local development:** Leave `PathBase` empty or unset in all apps. No proxy configuration is needed for local development. See the [Developer Guide](../getting-started/developer-guide.md) for local setup.

---

## 3. TLS Termination

The proxy accepts HTTPS from clients and forwards plain HTTP to each app internally. The apps do not need certificates.

Because the proxy terminates TLS, ASP.NET Core apps see incoming requests as `http://` even though clients connected over `https://`. Forwarded-headers middleware (see next section) restores the original scheme so that redirect URIs, OIDC issuer URLs, and cookie `Secure` flags all work correctly.

Do not configure Kestrel for HTTPS on the app ports. Each app should bind to HTTP only:

```bash
# In each app's environment or appsettings.json
ASPNETCORE_URLS=http://+:5001   # API
ASPNETCORE_URLS=http://+:5002   # Auth
ASPNETCORE_URLS=http://+:5003   # Web
```

---

## 4. Forwarded Headers

All three Wallow apps read `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` headers to reconstruct the original client request. Enable this with a single environment variable on each app:

```bash
ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
```

Set this variable on all three containers. Without it, the apps generate OIDC discovery documents and redirect URIs with `http://` instead of `https://`, causing authentication failures.

> This variable activates ASP.NET Core's built-in `ForwardedHeadersMiddleware` automatically — no code change is required.

---

## 5. Health Check Endpoints

Each app exposes a health check at `/healthz` (relative to its `PathBase`):

| App | Internal URL | Public URL (behind proxy) |
|-----|-------------|--------------------------|
| Wallow.Api | `http://localhost:5001/healthz` | `https://example.com/api/healthz` |
| Wallow.Auth | `http://localhost:5002/healthz` | `https://example.com/auth/healthz` |
| Wallow.Web | `http://localhost:5003/healthz` | `https://example.com/healthz` |

Configure your proxy or container orchestrator to poll the internal URL. A `200 OK` response means the app is ready to serve traffic.

---

## 6. WebSocket Support for Blazor SignalR

Both Wallow.Auth and Wallow.Web are Blazor Server apps. They require a persistent WebSocket connection to the server for interactivity. The proxy must upgrade HTTP connections to WebSocket on the `/_blazor` path for each Blazor app.

**Paths that require WebSocket upgrade:**

| App | Path |
|-----|------|
| Wallow.Auth | `/auth/_blazor` |
| Wallow.Web | `/_blazor` |

If WebSocket upgrades are blocked, Blazor falls back to long-polling, which is significantly slower and may fail entirely depending on proxy timeout settings. Always verify that WebSocket connections succeed when configuring a new proxy.

---

## 7. Proxy Configuration Examples

### Pangolin

[Pangolin](https://pangolin.fossorial.io) is the recommended proxy for self-hosted Wallow deployments. Configure three routes under a single site:

```yaml
# Pangolin route configuration (pangolin.yml or dashboard equivalent)

sites:
  - domain: example.com
    routes:
      # API — must come before the catch-all
      - path: /api
        target: http://localhost:5001
        strip_prefix: false   # PathBase middleware in the app handles stripping

      # Auth
      - path: /auth
        target: http://localhost:5002
        strip_prefix: false

      # Web (catch-all)
      - path: /
        target: http://localhost:5003
```

> **`strip_prefix: false`** is important. Wallow's `PathBase` middleware strips the prefix inside the app. If the proxy also strips it, the prefix is removed twice and static assets (CSS, JS, `/_blazor`) will return 404.

### nginx

```nginx
server {
    listen 443 ssl;
    server_name example.com;

    # TLS certificates managed by nginx or certbot
    ssl_certificate     /etc/letsencrypt/live/example.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/example.com/privkey.pem;

    # API
    location /api {
        proxy_pass         http://localhost:5001;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host  $host;
    }

    # Auth
    location /auth {
        proxy_pass         http://localhost:5002;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host  $host;

        # WebSocket support for Blazor SignalR
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";
    }

    # Web (catch-all) — must be last
    location / {
        proxy_pass         http://localhost:5003;
        proxy_http_version 1.1;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
        proxy_set_header   X-Forwarded-Host  $host;

        # WebSocket support for Blazor SignalR
        proxy_set_header   Upgrade    $http_upgrade;
        proxy_set_header   Connection "upgrade";
    }
}

# Redirect HTTP to HTTPS
server {
    listen 80;
    server_name example.com;
    return 301 https://$host$request_uri;
}
```

### Caddy

```caddy
example.com {
    # API
    handle /api* {
        reverse_proxy localhost:5001 {
            header_up X-Forwarded-Proto {scheme}
            header_up X-Forwarded-Host  {host}
        }
    }

    # Auth (WebSocket passthrough is automatic in Caddy)
    handle /auth* {
        reverse_proxy localhost:5002 {
            header_up X-Forwarded-Proto {scheme}
            header_up X-Forwarded-Host  {host}
        }
    }

    # Web (catch-all, WebSocket passthrough is automatic in Caddy)
    handle {
        reverse_proxy localhost:5003 {
            header_up X-Forwarded-Proto {scheme}
            header_up X-Forwarded-Host  {host}
        }
    }

    # Caddy handles TLS automatically via Let's Encrypt
}
```

> Caddy handles WebSocket upgrades and TLS certificate provisioning automatically. No additional directives are needed for either.

---

## Common Mistakes

| Mistake | Symptom | Fix |
|---------|---------|-----|
| Proxy strips the path prefix | Static assets return 404; Blazor fails to load | Set `strip_prefix: false` (Pangolin) or use `proxy_pass` with no trailing slash (nginx) |
| `PathBase` not set on API or Auth | Routes under `/api` or `/auth` return 404 | Set `PathBase=/api` on the API, `PathBase=/auth` on Auth |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` missing | OIDC redirects use `http://`; login fails | Set on all three apps |
| WebSocket not upgraded on `/_blazor` | Blazor UI freezes or falls back to long-polling | Add `Upgrade`/`Connection` headers in nginx; Caddy/Pangolin handle this automatically |
| Redirect URIs not updated | OIDC login returns `redirect_uri mismatch` | Update `PreRegisteredClients` config to use the public `https://example.com/...` URLs |
