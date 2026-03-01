# Versioning Guide

Foundry uses automated semantic versioning driven by [Conventional Commits](https://www.conventionalcommits.org/) and [GitVersion](https://gitversion.net/). Versions flow through assemblies, Docker images, and git tags with zero manual intervention.

## Commit Message Format

All commits must follow the Conventional Commits specification:

```
<type>[optional scope][!]: <description>

[optional body]

[optional footer(s)]
```

### Types and Version Impact

| Type | Version Bump | Example |
|------|-------------|---------|
| `fix` | Patch (0.0.X) | `fix: resolve null reference in tenant resolver` |
| `feat` | Minor (0.X.0) | `feat: add file upload to Storage module` |
| `feat!` | **Major (X.0.0)** | `feat!: redesign authentication flow` |
| `chore` | Patch | `chore: update NuGet packages` |
| `refactor` | Patch | `refactor: extract base entity class` |
| `docs` | Patch | `docs: add caching guide` |
| `test` | Patch | `test: add billing integration tests` |
| `ci` | Patch | `ci: add Docker build step` |
| `style` | Patch | `style: apply formatting rules` |
| `perf` | Patch | `perf: optimize tenant query with index` |
| `build` | Patch | `build: pin SDK version in global.json` |

A `BREAKING CHANGE` footer in any commit body also triggers a major bump:

```
refactor: change tenant ID from int to Guid

BREAKING CHANGE: TenantId is now a strongly-typed ID wrapping a Guid.
```

### Scope Examples

Scope is optional but useful for changelogs:

```
feat(billing): add Stripe webhook handler
fix(identity): correct token refresh logic
chore(deps): bump Wolverine to 3.x
```

## Version Flow

```
feature branch ──► dev branch ──► main branch ──► production
   (no version)    (pre-release)   (release)       (deploy)
```

- **Feature branches** — No versioning. CI builds and runs tests only.
- **Merge to dev** — Pre-release version, e.g. `0.2.0-dev.3`.
- **Merge to main** — Clean release version, e.g. `0.2.0`.
- **Tag push** — Triggers production deployment.

## Pre-release Versions

Dev builds carry a `-dev.{height}` suffix. Height is the number of commits since the last version-bumping merge.

### Example Sequence

```
1. feat: add payments     → merge to dev  → 0.2.0-dev.1
2. fix: typo in handler   → commit on dev → 0.2.0-dev.2
3. chore: update deps     → commit on dev → 0.2.0-dev.3
4. merge dev to main      →               → 0.2.0
5. fix: billing edge case → merge to dev  → 0.2.1-dev.1
6. feat: add invoices     → merge to dev  → 0.3.0-dev.1
```

## How to Trigger Version Bumps

**Patch** — Use any of: `fix`, `chore`, `refactor`, `docs`, `test`, `ci`, `style`, `perf`, `build`.

**Minor** — Use `feat`.

**Major** — Use `feat!` or include `BREAKING CHANGE` in the commit body.

> **Note:** The project starts at `0.x.y`. Moving to `1.0.0` is an intentional decision — push a commit with `feat!: release v1.0.0` or `BREAKING CHANGE` when ready.

## GitVersion Configuration

Configuration lives at `GitVersion.yml` in the repository root.

```yaml
mode: ContinuousDeployment
tag-prefix: 'v'
```

Branch configs:

| Branch | Mode | Tag | Increment | Source |
|--------|------|-----|-----------|--------|
| `main` | ContinuousDelivery | *(none)* | Patch | develop |
| `dev` / `develop` | ContinuousDeployment | `dev` | Minor | — |
| `feature/*`, `fix/*`, etc. | ContinuousDeployment | branch name | Inherit | develop, main |

## What Gets Stamped

| Artifact | How | Example |
|----------|-----|---------|
| Assembly version | CI passes `/p:Version={semver}` to `dotnet build` | `0.2.0` |
| Docker image tag | Semver tag + rolling branch tag | `0.2.0-dev.3`, `dev` |
| Git tags | Pushed by CI on main merges | `v0.2.0` |
| GitHub Releases | Created from main tags (future) | `v0.2.0` |

## Local Development

Local builds use `0.0.0-local` as the default version, set in `Directory.Build.props`:

```xml
<!-- CI overrides this via /p:Version -->
<Version>0.0.0-local</Version>
```

GitVersion only runs in CI. You do not need GitVersion installed locally.

## Bootstrapping

For new forks, tag the initial commit to give GitVersion a starting point:

```bash
git tag v0.1.0
git push origin v0.1.0
```

All subsequent versions are computed automatically from this baseline.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| GitVersion reports wrong version | Check `GitVersion.yml` branch configs match your branch naming. Run `dotnet gitversion` locally to debug. |
| CI gets `0.0.1` or unexpected version | Ensure `fetch-depth: 0` in your checkout action. GitVersion needs full git history. |
| Want to force a specific version | Push a manual tag: `git tag v1.0.0 && git push origin v1.0.0`. Next commits compute from that tag. |
| Pre-release height resets unexpectedly | Height resets after each merge to main. This is expected behavior. |
| Local build shows `0.0.0-local` | Normal. CI overrides this with the computed version via `/p:Version`. |
