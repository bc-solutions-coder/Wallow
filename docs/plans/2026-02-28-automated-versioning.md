# Automated Versioning Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Unified semver versioning across assemblies, Docker images, and git tags, driven by conventional commits and GitVersion.

**Architecture:** GitVersion reads git history + config to compute versions. CI passes the version to MSBuild and Docker. Dev gets pre-release suffixes (`0.2.0-dev.3`), main gets clean releases (`0.2.0`). Merging to main pushes a git tag which triggers the existing prod deploy workflow.

**Tech Stack:** GitVersion, GitHub Actions (`gittools/actions`), MSBuild `/p:Version`, Docker metadata action

---

### Task 1: Create GitVersion Configuration

**Files:**
- Create: `GitVersion.yml`

**Step 1: Create the GitVersion config file**

```yaml
mode: ContinuousDeployment
tag-prefix: v
major-version-bump-message: '(feat|fix|chore|refactor|docs|test|ci|style|perf|build)(\(.+\))?!:'
minor-version-bump-message: 'feat(\(.+\))?:'
patch-version-bump-message: '(fix|chore|refactor|docs|test|ci|style|perf|build)(\(.+\))?:'
commit-message-incrementing: Enabled
branches:
  main:
    regex: ^main$
    mode: ContinuousDelivery
    tag: ''
    increment: Patch
    prevent-increment-of-merged-branch-version: true
    track-merge-target: false
    source-branches:
      - develop
  develop:
    regex: ^dev$
    mode: ContinuousDeployment
    tag: dev
    increment: Minor
    source-branches: []
    tracks-release-branches: true
  feature:
    regex: ^(feature|feat|fix|chore|refactor|docs|test|ci|style|perf|build)[/-]
    mode: ContinuousDeployment
    tag: useBranchName
    increment: Inherit
    source-branches:
      - develop
      - main
ignore:
  sha: []
merge-message-formats: {}
```

**Step 2: Commit**

```bash
git add GitVersion.yml
git commit -m "ci: add GitVersion configuration for automated versioning"
```

---

### Task 2: Update Directory.Build.props

**Files:**
- Modify: `Directory.Build.props` (add Version property in the Assembly information PropertyGroup)

**Step 1: Add Version property**

Add this inside the existing "Assembly information" `<PropertyGroup>`:

```xml
    <!-- Version is overridden by CI via /p:Version; local builds use 0.0.0-local -->
    <Version>0.0.0-local</Version>
```

This goes right after the `<Copyright>` line, inside the same PropertyGroup.

**Step 2: Commit**

```bash
git add Directory.Build.props
git commit -m "ci: add Version property placeholder to Directory.Build.props"
```

---

### Task 3: Update deploy-dev.yml

**Files:**
- Modify: `.github/workflows/deploy-dev.yml`

**Step 1: Replace the full workflow with GitVersion integration**

Key changes:
- Add `fetch-depth: 0` to checkout (GitVersion needs full history)
- Add `contents: write` permission (for pushing tags)
- Add GitVersion setup and execute steps
- Add .NET build step with `/p:Version` before Docker build
- Add semver Docker tag
- Add git tag push step

The full updated workflow:

```yaml
# Deploy to Dev - builds, versions, pushes Docker image, and deploys via SSH
#
# Required GitHub Secrets (configure in Settings > Environments > development):
#   DEPLOY_SSH_KEY  - Private SSH key for the deploy user
#   DEPLOY_HOST     - Hostname or IP of the dev server
#   DEPLOY_USER     - SSH username on the dev server

name: Deploy to Dev

on:
  push:
    branches: [dev]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      packages: write

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v3.1.11
        with:
          versionSpec: '6.x'

      - name: Determine version
        id: gitversion
        uses: gittools/actions/gitversion/execute@v3.1.11

      - name: Tag and push version
        run: |
          git tag "v${{ steps.gitversion.outputs.fullSemVer }}"
          git push origin "v${{ steps.gitversion.outputs.fullSemVer }}"

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=raw,value=dev
            type=sha,prefix=dev-
            type=raw,value=${{ steps.gitversion.outputs.fullSemVer }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          build-args: |
            VERSION=${{ steps.gitversion.outputs.fullSemVer }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    environment: development

    steps:
      - name: Deploy to Dev Server
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.DEPLOY_HOST }}
          username: ${{ secrets.DEPLOY_USER }}
          key: ${{ secrets.DEPLOY_SSH_KEY }}
          script: bash /opt/foundry/scripts/deploy.sh dev dev-${{ github.sha }}
```

**Step 2: Commit**

```bash
git add .github/workflows/deploy-dev.yml
git commit -m "ci: add GitVersion to dev deployment workflow"
```

---

### Task 4: Update deploy-staging.yml

**Files:**
- Modify: `.github/workflows/deploy-staging.yml`

**Step 1: Replace the full workflow with GitVersion integration**

Key changes:
- Add `fetch-depth: 0` to checkout
- Add `contents: write` permission (for pushing tags that trigger prod deploy)
- Add GitVersion steps
- Add semver Docker tag
- Push git tag (this triggers `deploy-prod.yml` automatically)

The full updated workflow:

```yaml
# Deploy to Staging - builds, versions, pushes Docker image, tags release, and deploys via SSH
#
# When this workflow pushes a git tag (e.g. v0.2.0), it automatically triggers
# the production deploy workflow (deploy-prod.yml).
#
# Required GitHub Secrets (configure in Settings > Environments > staging):
#   DEPLOY_SSH_KEY  - Private SSH key for the deploy user
#   DEPLOY_HOST     - Hostname or IP of the staging server
#   DEPLOY_USER     - SSH username on the staging server

name: Deploy to Staging

on:
  push:
    branches: [main]

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: write
      packages: write

    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v3.1.11
        with:
          versionSpec: '6.x'

      - name: Determine version
        id: gitversion
        uses: gittools/actions/gitversion/execute@v3.1.11

      - name: Tag and push version
        run: |
          git tag "v${{ steps.gitversion.outputs.fullSemVer }}"
          git push origin "v${{ steps.gitversion.outputs.fullSemVer }}"

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=raw,value=staging
            type=sha,prefix=staging-
            type=raw,value=${{ steps.gitversion.outputs.fullSemVer }}

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          build-args: |
            VERSION=${{ steps.gitversion.outputs.fullSemVer }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    environment: staging

    steps:
      - name: Deploy to Staging Server
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.DEPLOY_HOST }}
          username: ${{ secrets.DEPLOY_USER }}
          key: ${{ secrets.DEPLOY_SSH_KEY }}
          script: bash /opt/foundry/scripts/deploy.sh staging staging-${{ github.sha }}
```

**Step 2: Commit**

```bash
git add .github/workflows/deploy-staging.yml
git commit -m "ci: add GitVersion to staging deployment workflow"
```

---

### Task 5: Update deploy-prod.yml

**Files:**
- Modify: `.github/workflows/deploy-prod.yml`

**Step 1: Add version stamping to production workflow**

The prod workflow already triggers on tag push (`v*`). Add version extraction from the tag and pass it as a Docker build arg. The tag itself is already used for Docker metadata via `type=semver`.

Key changes:
- Extract version from tag ref name
- Add `VERSION` build arg to Docker build

```yaml
# Deploy to Production - builds, pushes Docker image, and deploys via SSH
#
# Triggered automatically when deploy-staging.yml pushes a version tag (e.g. v1.2.3),
# or manually by pushing a tag.
#
# Required GitHub Secrets (configure in Settings > Environments > production):
#   DEPLOY_SSH_KEY  - Private SSH key for the deploy user
#   DEPLOY_HOST     - Hostname or IP of the production server
#   DEPLOY_USER     - SSH username on the production server

name: Deploy to Production

on:
  push:
    tags: ['v*']

env:
  REGISTRY: ghcr.io
  IMAGE_NAME: ${{ github.repository }}

jobs:
  build-and-push:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - uses: actions/checkout@v4

      - name: Extract version from tag
        id: version
        run: echo "version=${GITHUB_REF_NAME#v}" >> "$GITHUB_OUTPUT"

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Log in to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}
          tags: |
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=raw,value=latest

      - name: Build and push
        uses: docker/build-push-action@v5
        with:
          context: .
          push: true
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          build-args: |
            VERSION=${{ steps.version.outputs.version }}
          cache-from: type=gha
          cache-to: type=gha,mode=max

  deploy:
    needs: build-and-push
    runs-on: ubuntu-latest
    environment: production

    steps:
      - name: Deploy to Production Server
        uses: appleboy/ssh-action@v1
        with:
          host: ${{ secrets.DEPLOY_HOST }}
          username: ${{ secrets.DEPLOY_USER }}
          key: ${{ secrets.DEPLOY_SSH_KEY }}
          script: bash /opt/foundry/scripts/deploy.sh prod ${{ github.ref_name }}
```

**Step 2: Commit**

```bash
git add .github/workflows/deploy-prod.yml
git commit -m "ci: add version stamping to production deployment workflow"
```

---

### Task 6: Update Dockerfile to Accept VERSION Build Arg

**Files:**
- Modify: `Dockerfile` (add ARG and pass to dotnet build/publish)

**Step 1: Find and read the Dockerfile**

Look for the Dockerfile at the repo root or in `docker/`. Read it to understand the current build structure.

**Step 2: Add VERSION build arg**

Add these lines to the Dockerfile build stage:

```dockerfile
ARG VERSION=0.0.0-local
```

And update the `dotnet publish` (or `dotnet build`) command to include:

```
-p:Version=${VERSION}
```

**Step 3: Commit**

```bash
git add Dockerfile
git commit -m "ci: accept VERSION build arg in Dockerfile for assembly versioning"
```

---

### Task 7: Create docs/VERSIONING_GUIDE.md

**Files:**
- Create: `docs/VERSIONING_GUIDE.md`

**Step 1: Write the versioning guide**

Full developer-facing documentation covering:
- Overview of the versioning strategy
- Conventional commit format with examples
- How versions flow through dev → staging → production
- How to trigger major/minor/patch bumps
- Pre-release version format
- How to bootstrap (initial `v0.1.0` tag)
- How GitVersion config works
- Troubleshooting (what if GitVersion computes wrong version, how to force a version)

Draw from the design doc at `docs/plans/2026-02-28-automated-versioning-design.md` but write it as a practical developer guide.

**Step 2: Commit**

```bash
git add docs/VERSIONING_GUIDE.md
git commit -m "docs: add versioning guide"
```

---

### Task 8: Update CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

**Step 1: Add versioning section**

Add after the "Architecture" section:

```markdown
## Versioning

Automated semver via [Conventional Commits](https://www.conventionalcommits.org/) + GitVersion. See `docs/VERSIONING_GUIDE.md`.

**Commit message format:** `<type>[optional scope][!]: <description>`

| Prefix | Bump | Example |
|--------|------|---------|
| `fix:` | Patch | `fix: resolve null ref in tenant resolver` |
| `feat:` | Minor | `feat: add billing invoice export` |
| `feat!:` | Major | `feat!: redesign authentication API` |
| `chore:`, `refactor:`, `docs:`, `test:`, `ci:` | Patch | `chore: update dependencies` |

- **Dev branch** produces pre-release versions: `0.2.0-dev.3`
- **Main branch** produces release versions: `0.2.0`
- Version stamps assemblies, Docker images, and git tags
```

**Step 2: Add link in Documentation section**

Add to the Documentation list:
```markdown
- **Versioning guide:** `docs/VERSIONING_GUIDE.md`
```

**Step 3: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add versioning conventions to CLAUDE.md"
```

---

### Task 9: Create .claude/rules/COMMITS.md

**Files:**
- Create: `.claude/rules/COMMITS.md`

**Step 1: Write the rule**

```markdown
## Commit Messages

All commits MUST use Conventional Commits format: `<type>[scope][!]: <description>`

**Types:** `feat`, `fix`, `chore`, `refactor`, `docs`, `test`, `ci`, `style`, `perf`, `build`

- `fix:` = patch bump, `feat:` = minor bump, `!` or `BREAKING CHANGE` = major bump
- Description must be lowercase, imperative, no period at end
- Keep the first line under 72 characters
- Use scope for module names when relevant: `feat(billing): add invoice export`
```

**Step 2: Commit**

```bash
git add .claude/rules/COMMITS.md
git commit -m "docs: add conventional commits rule for agents"
```

---

### Task 10: Bootstrap Tag

**Note:** This task is performed AFTER the PR merges to dev.

**Step 1: Tag the merge commit**

```bash
git checkout dev
git pull
git tag v0.1.0
git push origin v0.1.0
```

This bootstraps GitVersion. All future versions derive from this tag.

---

## Execution Notes

- Tasks 1-9 can be committed on the current branch and PR'd together
- Task 10 happens after the PR merges
- The Dockerfile task (6) requires reading the existing Dockerfile first to determine exact edit location
- No tests to write - this is CI/docs configuration only
