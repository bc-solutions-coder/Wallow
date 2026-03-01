# Automated Versioning Design

## Goal

Unified semver versioning across assemblies, Docker images, and git tags, driven by conventional commit messages and computed by GitVersion in CI.

## Version Flow

- **Feature branches** - no version tagging; CI builds and tests only
- **Merge to `dev`** - GitVersion computes a pre-release version (e.g. `0.2.0-dev.3`). Stamps assemblies, Docker image, git tag.
- **Merge to `main`** - GitVersion produces the clean release version (e.g. `0.2.0`). Stamps assemblies, Docker image, git tag, GitHub Release with changelog.

## Commit Message Conventions

All commits must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>[optional scope][!]: <description>

[optional body]

[optional footer(s)]
```

Types and their version impact:

| Type | Version Bump | Example |
|------|-------------|---------|
| `fix:` | Patch (0.1.0 -> 0.1.1) | `fix: resolve null reference in tenant resolver` |
| `feat:` | Minor (0.1.0 -> 0.2.0) | `feat: add billing invoice export` |
| `feat!:` or `BREAKING CHANGE` | Major (0.1.0 -> 1.0.0) | `feat!: mark API as stable` |
| `chore:`, `refactor:`, `docs:`, `test:`, `ci:`, `style:`, `perf:`, `build:` | Patch | `chore: update NuGet dependencies` |

## Pre-release Versions

Dev builds carry a `-dev.{height}` suffix where height is the number of commits since the last version-changing commit:

- `feat:` merge to dev -> `0.2.0-dev.1`
- Next commit on dev -> `0.2.0-dev.2`
- Merge dev to main -> `0.2.0` (suffix dropped)
- Next `fix:` on dev -> `0.2.1-dev.1`

## GitVersion Configuration

A `GitVersion.yml` at the repo root:

- Mainline mode with `dev` as development branch, `main` as release
- Tag prefix: `v` (e.g. `v0.1.0`)
- Dev branch label: `dev`
- Feature branches inherit from dev, no tags produced

## What Gets Stamped

On every merge to `dev` or `main`:

1. **Assembly version** - via `/p:Version={semver}` passed to `dotnet build`
2. **Docker image tags** - semver tag (e.g. `0.2.0-dev.3`) alongside rolling `dev` tag
3. **Git tag** - `v0.2.0-dev.3` or `v0.2.0` pushed to repo
4. **GitHub Release** (main only) - auto-generated changelog from conventional commits

## CI Changes

### deploy-dev.yml

Add steps before build:
1. Full git checkout (`fetch-depth: 0` for GitVersion)
2. GitVersion action (`gittools/actions/gitversion`) to compute version
3. Pass version to `dotnet build` via `/p:Version`
4. Add semver Docker tag alongside existing `dev` + `dev-<sha>` tags
5. Push git tag back to repo

### deploy-prod.yml / deploy-staging.yml

Same GitVersion step, producing clean versions on main.

### ci.yml

No changes - continues to build and test without versioning.

## Bootstrap

Once the implementing PR is green, tag the merge commit with `v0.1.0` to bootstrap the system. All subsequent versions derive from this tag.

## Implementation Deliverables

1. `GitVersion.yml` - configuration file at repo root
2. `docs/VERSIONING_GUIDE.md` - full developer-facing documentation
3. `CLAUDE.md` - add versioning conventions section
4. `.claude/rules/COMMITS.md` - rule enforcing conventional commit format
5. `.github/workflows/deploy-dev.yml` - add GitVersion + version stamping
6. `.github/workflows/deploy-prod.yml` - add GitVersion + version stamping + GitHub Release
7. `.github/workflows/deploy-staging.yml` - add GitVersion + version stamping
8. `Directory.Build.props` - add Version property placeholder (overridden by CI)
