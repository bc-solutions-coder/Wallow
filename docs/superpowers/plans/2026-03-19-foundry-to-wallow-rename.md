# Foundry → Wallow v0.1.0 Rename Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create a fresh `bc-solutions-coder/Wallow` repository as a clean v0.1.0 by renaming all "Foundry" references to "Wallow" from the existing Foundry codebase — no git history, fresh migrations, clean slate.

**Architecture:** Copy the Foundry codebase (excluding `.git`, build artifacts, and generated files) into a new directory. Rename all directories, files, and content references from `Foundry`/`foundry` to `Wallow`/`wallow`. Reset all EF Core migrations to fresh `InitialCreate` per module. Initialize a new git repo, push to GitHub as `bc-solutions-coder/Wallow`, and tag `v0.1.0`.

**Tech Stack:** .NET 10, PostgreSQL, RabbitMQ, Docker, Helm, Kustomize, GitHub Actions

**Source repo:** `bc-solutions-coder/Foundry` (stays untouched as backup)
**Target repo:** `bc-solutions-coder/Wallow` (new, clean v0.1.0)
**Domain:** `wallow.dev`

---

## Naming Convention Reference

All rename operations follow this mapping:

| Original | Replacement | Context |
|----------|-------------|---------|
| `Foundry` | `Wallow` | PascalCase — namespaces, class names, project names, assembly names |
| `foundry` | `wallow` | lowercase — schemas, docker, k8s, URLs, database names, env vars |
| `FOUNDRY` | `WALLOW` | UPPER — environment variable names (e.g. `FOUNDRY_DB_PASSWORD`) |

---

## Epic 1: Repository Bootstrap

> **Purpose:** Create the new Wallow directory from a clean copy of Foundry, with all build artifacts and git history excluded. This is the foundation everything else builds on.

### Feature 1.1: Copy and Clean

#### Task 1.1.1: Create clean copy of Foundry codebase

**Files:**
- Source: `/Users/traveler/Repos/Foundry/` (read-only)
- Target: `/Users/traveler/Repos/Wallow/` (new)

- [ ] **Step 1: Create the target directory**

```bash
mkdir -p /Users/traveler/Repos/Wallow
```

- [ ] **Step 2: Copy codebase excluding git and build artifacts**

```bash
rsync -av --progress /Users/traveler/Repos/Foundry/ /Users/traveler/Repos/Wallow/ \
  --exclude='.git' \
  --exclude='bin/' \
  --exclude='obj/' \
  --exclude='TestResults/' \
  --exclude='CoverageReport/' \
  --exclude='.qodana/' \
  --exclude='.qodana-cache/' \
  --exclude='.qodana-results/' \
  --exclude='node_modules/' \
  --exclude='.beads/' \
  --exclude='.claude/' \
  --exclude='.superpowers/' \
  --exclude='.run/'
```

- [ ] **Step 3: Verify the copy is complete**

```bash
# Count directories and files in both
echo "Source:" && find /Users/traveler/Repos/Foundry -not -path '*/.git/*' -not -path '*/bin/*' -not -path '*/obj/*' -not -path '*/TestResults/*' -not -path '*/.beads/*' -not -path '*/.claude/*' | wc -l
echo "Target:" && find /Users/traveler/Repos/Wallow | wc -l
```

- [ ] **Step 4: Initialize git repo in the new directory**

```bash
cd /Users/traveler/Repos/Wallow
git init
```

- [ ] **Step 5: Delete all existing migration files** (they'll be regenerated fresh in Epic 5)

```bash
cd /Users/traveler/Repos/Wallow
find . -path '*/Migrations/*' -name '*.cs' -delete
find . -path '*/Migrations/*' -name '*.Designer.cs' -delete
# Remove empty Migrations directories will be kept for structure
```

Expected: All `*.cs` files under any `Migrations/` directory are deleted.

---

## Epic 2: Directory and File Renames

> **Purpose:** Rename every directory and file that contains "Foundry" in its name to use "Wallow" instead. This must happen before content changes because file paths are referenced in `.csproj`, `.slnx`, and other config files.
>
> **CRITICAL:** Rename from deepest paths first (bottom-up) to avoid breaking parent paths before children are renamed.

### Feature 2.1: Rename Source Directories

#### Task 2.1.1: Rename module project directories (src/Modules)

**Directories to rename (32 directories — 8 modules × 4 layers):**

Each module has 4 project directories following the pattern `Foundry.{Module}.{Layer}/`.

- [ ] **Step 1: Rename all module project directories**

```bash
cd /Users/traveler/Repos/Wallow/src/Modules

for module in Announcements Billing Identity Inquiries Messaging Notifications Showcases Storage; do
  for layer in Api Application Domain Infrastructure; do
    if [ -d "$module/Foundry.$module.$layer" ]; then
      mv "$module/Foundry.$module.$layer" "$module/Wallow.$module.$layer"
      echo "Renamed: $module/Foundry.$module.$layer → $module/Wallow.$module.$layer"
    fi
  done
done
```

- [ ] **Step 2: Verify all module directories renamed**

```bash
# Should return 0 results
find /Users/traveler/Repos/Wallow/src/Modules -maxdepth 3 -type d -name 'Foundry.*'
# Should return 32 results
find /Users/traveler/Repos/Wallow/src/Modules -maxdepth 3 -type d -name 'Wallow.*' | wc -l
```

Expected: 0 Foundry directories remain, 32 Wallow directories exist.

#### Task 2.1.2: Rename shared project directories (src/Shared)

**Directories (8):**
- `Foundry.Shared.Api/` → `Wallow.Shared.Api/`
- `Foundry.Shared.Contracts/` → `Wallow.Shared.Contracts/`
- `Foundry.Shared.Infrastructure/` → `Wallow.Shared.Infrastructure/`
- `Foundry.Shared.Infrastructure.BackgroundJobs/` → `Wallow.Shared.Infrastructure.BackgroundJobs/`
- `Foundry.Shared.Infrastructure.Core/` → `Wallow.Shared.Infrastructure.Core/`
- `Foundry.Shared.Infrastructure.Plugins/` → `Wallow.Shared.Infrastructure.Plugins/`
- `Foundry.Shared.Infrastructure.Workflows/` → `Wallow.Shared.Infrastructure.Workflows/`
- `Foundry.Shared.Kernel/` → `Wallow.Shared.Kernel/`

- [ ] **Step 1: Rename shared directories**

```bash
cd /Users/traveler/Repos/Wallow/src/Shared
for dir in Foundry.Shared.*; do
  newdir="${dir/Foundry/Wallow}"
  mv "$dir" "$newdir"
  echo "Renamed: $dir → $newdir"
done
```

- [ ] **Step 2: Verify**

```bash
ls /Users/traveler/Repos/Wallow/src/Shared/
```

Expected: All directories start with `Wallow.Shared.`

#### Task 2.1.3: Rename API project directory

- [ ] **Step 1: Rename Foundry.Api directory**

```bash
mv /Users/traveler/Repos/Wallow/src/Foundry.Api /Users/traveler/Repos/Wallow/src/Wallow.Api
```

- [ ] **Step 2: Verify**

```bash
ls /Users/traveler/Repos/Wallow/src/
```

Expected: `Wallow.Api/` exists, no `Foundry.Api/`.

### Feature 2.2: Rename Test Directories

#### Task 2.2.1: Rename root-level test directories

**Directories (7):**
- `Foundry.Api.Tests/` → `Wallow.Api.Tests/`
- `Foundry.Architecture.Tests/` → `Wallow.Architecture.Tests/`
- `Foundry.Messaging.IntegrationTests/` → `Wallow.Messaging.IntegrationTests/`
- `Foundry.Shared.Infrastructure.Tests/` → `Wallow.Shared.Infrastructure.Tests/`
- `Foundry.Shared.Kernel.Tests/` → `Wallow.Shared.Kernel.Tests/`
- `Foundry.Tests.Common/` → `Wallow.Tests.Common/`
- `Benchmarks/Foundry.Benchmarks.csproj` (file rename only, directory is `Benchmarks/`)

- [ ] **Step 1: Rename root test directories**

```bash
cd /Users/traveler/Repos/Wallow/tests
for dir in Foundry.*; do
  if [ -d "$dir" ]; then
    newdir="${dir/Foundry/Wallow}"
    mv "$dir" "$newdir"
    echo "Renamed: $dir → $newdir"
  fi
done
```

#### Task 2.2.2: Rename module test directories

**Directories (10 — under tests/Modules/{Module}/):**

- [ ] **Step 1: Rename module test directories**

```bash
cd /Users/traveler/Repos/Wallow/tests/Modules

for module in Announcements Billing Identity Inquiries Messaging Notifications Showcases Storage; do
  if [ -d "$module" ]; then
    for dir in "$module"/Foundry.*; do
      if [ -d "$dir" ]; then
        newdir="${dir/Foundry/Wallow}"
        mv "$dir" "$newdir"
        echo "Renamed: $dir → $newdir"
      fi
    done
  fi
done
```

- [ ] **Step 2: Verify no Foundry test directories remain**

```bash
find /Users/traveler/Repos/Wallow/tests -type d -name 'Foundry.*'
```

Expected: 0 results.

### Feature 2.3: Rename Project Files (.csproj) and Solution File

#### Task 2.3.1: Rename all .csproj files

- [ ] **Step 1: Rename all .csproj files containing "Foundry"**

```bash
cd /Users/traveler/Repos/Wallow
find . -name 'Foundry.*.csproj' -type f | while read f; do
  dir=$(dirname "$f")
  oldname=$(basename "$f")
  newname="${oldname/Foundry/Wallow}"
  mv "$f" "$dir/$newname"
  echo "Renamed: $oldname → $newname"
done
```

- [ ] **Step 2: Verify count**

```bash
find /Users/traveler/Repos/Wallow -name '*.csproj' | wc -l
find /Users/traveler/Repos/Wallow -name 'Foundry.*.csproj' | wc -l
find /Users/traveler/Repos/Wallow -name 'Wallow.*.csproj' | wc -l
```

Expected: 0 Foundry csproj files, 57+ Wallow csproj files.

#### Task 2.3.2: Rename solution file

- [ ] **Step 1: Rename Foundry.slnx**

```bash
mv /Users/traveler/Repos/Wallow/Foundry.slnx /Users/traveler/Repos/Wallow/Wallow.slnx
```

#### Task 2.3.3: Rename DotSettings files

- [ ] **Step 1: Rename solution settings files**

```bash
cd /Users/traveler/Repos/Wallow
mv Foundry.sln.DotSettings Wallow.sln.DotSettings 2>/dev/null || true
mv Foundry.sln.DotSettings.user Wallow.sln.DotSettings.user 2>/dev/null || true
```

### Feature 2.4: Rename Deployment Directories

#### Task 2.4.1: Rename Helm chart directory

- [ ] **Step 1: Rename the Helm chart directory**

```bash
mv /Users/traveler/Repos/Wallow/deploy/helm/foundry /Users/traveler/Repos/Wallow/deploy/helm/wallow
```

### Feature 2.5: Rename Other Files

#### Task 2.5.1: Rename any remaining files with "Foundry" in name

- [ ] **Step 1: Find and rename any remaining files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*[Ff]oundry*' -not -path './.git/*' -type f | while read f; do
  dir=$(dirname "$f")
  oldname=$(basename "$f")
  newname=$(echo "$oldname" | sed 's/Foundry/Wallow/g; s/foundry/wallow/g')
  if [ "$oldname" != "$newname" ]; then
    mv "$f" "$dir/$newname"
    echo "Renamed: $f → $dir/$newname"
  fi
done
```

- [ ] **Step 2: Verify no Foundry-named files remain**

```bash
find /Users/traveler/Repos/Wallow -name '*Foundry*' -not -path './.git/*' | head -20
find /Users/traveler/Repos/Wallow -name '*foundry*' -not -path './.git/*' | head -20
```

Expected: 0 results for both.

### Feature 2.6: Verification Gate — Directory & File Renames

#### Task 2.6.1: Full filesystem verification

- [ ] **Step 1: Comprehensive check — no "Foundry" in any path**

```bash
cd /Users/traveler/Repos/Wallow
echo "=== Directories with Foundry ==="
find . -type d -iname '*foundry*' -not -path './.git/*' | head -20
echo "=== Files with Foundry ==="
find . -type f -iname '*foundry*' -not -path './.git/*' | head -20
echo "=== Expected: 0 results for both ==="
```

- [ ] **Step 2: Spot-check key paths exist**

```bash
cd /Users/traveler/Repos/Wallow
test -f Wallow.slnx && echo "✓ Wallow.slnx" || echo "✗ Wallow.slnx MISSING"
test -d src/Wallow.Api && echo "✓ src/Wallow.Api" || echo "✗ src/Wallow.Api MISSING"
test -d src/Shared/Wallow.Shared.Kernel && echo "✓ Shared.Kernel" || echo "✗ Shared.Kernel MISSING"
test -d src/Modules/Identity/Wallow.Identity.Domain && echo "✓ Identity.Domain" || echo "✗ Identity.Domain MISSING"
test -d tests/Wallow.Api.Tests && echo "✓ Api.Tests" || echo "✗ Api.Tests MISSING"
test -d tests/Wallow.Tests.Common && echo "✓ Tests.Common" || echo "✗ Tests.Common MISSING"
test -d deploy/helm/wallow && echo "✓ helm/wallow" || echo "✗ helm/wallow MISSING"
```

Expected: All checks pass with `✓`.

---

## Epic 3: Content Renaming — Code and Projects

> **Purpose:** Replace all occurrences of "Foundry" / "foundry" / "FOUNDRY" inside file contents. This is the largest epic by volume (~13,000+ replacements). Organized by file type to allow parallel execution.
>
> **CRITICAL ORDERING:** `.csproj` and `.slnx` files must be updated before attempting `dotnet restore` or `dotnet build`. C# files can be updated in any order.

### Feature 3.1: Solution and Project Files

#### Task 3.1.1: Update solution file content (Wallow.slnx)

**Files:**
- Modify: `Wallow.slnx`

- [ ] **Step 1: Replace all "Foundry" with "Wallow" in solution file**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g' Wallow.slnx
```

- [ ] **Step 2: Verify no Foundry references remain**

```bash
grep -c 'Foundry' Wallow.slnx
```

Expected: 0

#### Task 3.1.2: Update all .csproj file contents

**Files:** All 58 `.csproj` files

- [ ] **Step 1: Replace "Foundry" in all .csproj files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.csproj' -type f | xargs sed -i '' 's/Foundry/Wallow/g'
```

- [ ] **Step 2: Verify no Foundry references remain in any .csproj**

```bash
grep -rl 'Foundry' --include='*.csproj' /Users/traveler/Repos/Wallow | head -10
```

Expected: 0 results.

#### Task 3.1.3: Update Directory.Build.props files

**Files:**
- Modify: `Directory.Build.props` (root)
- Modify: `tests/Directory.Build.props`

- [ ] **Step 1: Replace in root Directory.Build.props**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g' Directory.Build.props
```

- [ ] **Step 2: Replace in tests Directory.Build.props**

```bash
sed -i '' 's/Foundry/Wallow/g' tests/Directory.Build.props
```

- [ ] **Step 3: Verify Company/Product/Copyright updated**

```bash
grep -E 'Company|Product|Copyright|InternalsVisibleTo' Directory.Build.props
```

Expected: All references show "Wallow".

#### Task 3.1.4: Update Directory.Build.targets

**Files:**
- Modify: `Directory.Build.targets`

- [ ] **Step 1: Replace if file contains Foundry references**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g' Directory.Build.targets
```

### Feature 3.2: C# Source Files — Namespaces and Usings

> **Note:** This is the highest-volume change (~7,000+ replacements across ~1,500 files). A single `sed` pass handles it.

#### Task 3.2.1: Replace "Foundry" in all C# source files

**Files:** All `*.cs` files under `src/` and `tests/`

- [ ] **Step 1: Replace in all C# files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.cs' -type f -not -path './.git/*' | xargs sed -i '' 's/Foundry/Wallow/g'
```

This handles:
- `namespace Foundry.` → `namespace Wallow.`
- `using Foundry.` → `using Wallow.`
- `"Foundry"` string literals → `"Wallow"`
- Assembly attributes → updated
- `FoundryApiFactory` → `WallowApiFactory` (class names)

- [ ] **Step 2: Handle lowercase "foundry" in C# files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.cs' -type f -not -path './.git/*' | xargs sed -i '' 's/foundry/wallow/g'
```

This handles:
- `Database=foundry` → `Database=wallow`
- `?? "foundry"` fallback passwords → `?? "wallow"`
- `foundry_test` database name → `wallow_test`

- [ ] **Step 3: Handle UPPERCASE "FOUNDRY" in C# files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.cs' -type f -not -path './.git/*' | xargs sed -i '' 's/FOUNDRY/WALLOW/g'
```

This handles:
- `FOUNDRY_DB_PASSWORD` → `WALLOW_DB_PASSWORD`

- [ ] **Step 4: Verify no Foundry references remain in C# files**

```bash
grep -rl 'Foundry' --include='*.cs' /Users/traveler/Repos/Wallow | wc -l
grep -rl 'foundry' --include='*.cs' /Users/traveler/Repos/Wallow | wc -l
grep -rl 'FOUNDRY' --include='*.cs' /Users/traveler/Repos/Wallow | wc -l
```

Expected: 0 for all three.

### Feature 3.3: Razor Pages and Other View Files

#### Task 3.3.1: Replace "Foundry" in all Razor (.cshtml) files

**Files:**
- Modify: `src/Wallow.Api/Pages/_ViewImports.cshtml`
- Modify: `src/Wallow.Api/Pages/Account/Login.cshtml`
- Modify: `src/Wallow.Api/Pages/Account/Logout.cshtml`
- Modify: `src/Wallow.Api/Pages/Account/Consent.cshtml`

> **CRITICAL:** These files contain `@using Foundry.Api`, `@namespace Foundry.Api.Pages`, and `@model Foundry.Api.Pages.Account.*` references. Missing these will cause build failures.

- [ ] **Step 1: Replace in all .cshtml files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.cshtml' -type f | xargs sed -i '' 's/Foundry/Wallow/g'
```

- [ ] **Step 2: Verify**

```bash
grep -rl 'Foundry' --include='*.cshtml' /Users/traveler/Repos/Wallow
```

Expected: 0 results.

### Feature 3.4: Build Verification Gate

#### Task 3.4.1: Verify the solution restores and builds

- [ ] **Step 1: Clean and restore**

```bash
cd /Users/traveler/Repos/Wallow
dotnet restore Wallow.slnx
```

Expected: Restore succeeds with 0 errors.

- [ ] **Step 2: Build the solution**

```bash
cd /Users/traveler/Repos/Wallow
dotnet build Wallow.slnx --no-restore
```

Expected: Build succeeds. If there are errors, they indicate missed renames — fix individually.

- [ ] **Step 3: If build fails, find remaining references**

```bash
# Only run if build fails
cd /Users/traveler/Repos/Wallow
grep -rn 'Foundry' --include='*.cs' --include='*.csproj' . | grep -v '.git' | head -30
```

Fix any remaining references and re-build.

---

## Epic 4: Content Renaming — Configuration and Infrastructure

> **Purpose:** Update all non-code files: JSON config, Docker, Kubernetes, CI/CD, documentation. These don't affect compilation but are critical for runtime, deployment, and developer experience.

### Feature 4.1: Application Configuration (JSON)

#### Task 4.1.1: Update appsettings files

**Files:**
- Modify: `src/Wallow.Api/appsettings.json`
- Modify: `src/Wallow.Api/appsettings.Development.json`
- Modify: `src/Wallow.Api/appsettings.Production.json`
- Modify: `src/Wallow.Api/appsettings.Staging.json`
- Modify: `src/Wallow.Api/appsettings.Testing.json`
- Modify: `appsettings.json` (root, if exists)
- Modify: `appsettings.Development.json` (root, if exists)

- [ ] **Step 1: Replace in all appsettings files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name 'appsettings*.json' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g; s/FOUNDRY/WALLOW/g'
```

- [ ] **Step 2: Verify**

```bash
grep -rl 'oundry' --include='appsettings*.json' /Users/traveler/Repos/Wallow
```

Expected: 0 results.

#### Task 4.1.2: Update launchSettings.json files

**Files:**
- Modify: `Properties/launchSettings.json`
- Modify: `src/Wallow.Api/Properties/launchSettings.json`

- [ ] **Step 1: Replace in launch settings**

```bash
cd /Users/traveler/Repos/Wallow
find . -name 'launchSettings.json' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

#### Task 4.1.3: Update stylecop.json

**Files:**
- Modify: `stylecop.json`

- [ ] **Step 1: Replace company name**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g' stylecop.json
```

#### Task 4.1.4: Update Grafana dashboard JSON files

**Files:** All JSON files under `docker/grafana/`

- [ ] **Step 1: Replace in all Grafana dashboard files**

```bash
cd /Users/traveler/Repos/Wallow
find docker/grafana -name '*.json' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

#### Task 4.1.5: Update test configuration files

**Files:**
- Modify: Any `foundry-realm.json` → rename to `wallow-realm.json` and update content

- [ ] **Step 1: Find and update test config files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*foundry*' -name '*.json' -type f | while read f; do
  dir=$(dirname "$f")
  oldname=$(basename "$f")
  newname="${oldname/foundry/wallow}"
  sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' "$f"
  mv "$f" "$dir/$newname"
  echo "Updated and renamed: $f → $dir/$newname"
done
```

### Feature 4.2: Docker Configuration

#### Task 4.2.1: Update Dockerfile

**Files:**
- Modify: `Dockerfile`

- [ ] **Step 1: Replace all Foundry references**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' Dockerfile
```

- [ ] **Step 2: Verify key lines**

```bash
grep -n 'allow' Dockerfile
```

Expected: References to `Wallow.slnx`, `Wallow.Api.csproj`, `Wallow.Api.dll`.

#### Task 4.2.2: Update docker-compose files

**Files:**
- Modify: `docker/docker-compose.yml`
- Modify: `docker/docker-compose.dev.yml`
- Modify: `docker/docker-compose.prod.yml`
- Modify: `docker/docker-compose.staging.yml`
- Modify: `docker/docker-compose.multi-region.yml`
- Modify: `docker/docker-compose.rabbitmq.yml`
- Modify: `deploy/docker-compose.base.yml`
- Modify: `deploy/docker-compose.dev.yml`
- Modify: `deploy/docker-compose.prod.yml`
- Modify: `deploy/docker-compose.staging.yml`

- [ ] **Step 1: Replace in all docker-compose files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name 'docker-compose*.yml' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g; s/FOUNDRY/WALLOW/g'
```

- [ ] **Step 2: Verify network and container names updated**

```bash
grep -n 'oundry' docker/docker-compose.yml
```

Expected: 0 results.

#### Task 4.2.3: Update docker .env and .env.example files

- [ ] **Step 1: Check and update docker .env and .env.example files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '.env' -o -name '.env.example' | while read f; do
  sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g; s/FOUNDRY/WALLOW/g' "$f"
  echo "Updated: $f"
done
```

This covers:
- `docker/.env` — `COMPOSE_PROJECT_NAME`, `POSTGRES_USER`, `POSTGRES_DB`
- `docker/.env.example` — same variables
- `deploy/.env.example` — `APP_IMAGE`, `COMPOSE_PROJECT_NAME`, `POSTGRES_USER`, `RABBITMQ_USER`

#### Task 4.2.4: Update deploy shell scripts

**Files:**
- Modify: `deploy/bootstrap.sh`
- Modify: `deploy/deploy.sh`

> These contain paths like `/opt/foundry/`, image names like `ghcr.io/bc-solutions-coder/foundry`, and log messages referencing Foundry.

- [ ] **Step 1: Replace in all shell scripts**

```bash
cd /Users/traveler/Repos/Wallow
find deploy -name '*.sh' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g; s/FOUNDRY/WALLOW/g'
```

- [ ] **Step 2: Verify**

```bash
grep -rn 'oundry' deploy/*.sh
```

Expected: 0 results.
```

### Feature 4.3: Kubernetes and Helm

#### Task 4.3.1: Update Helm chart

**Files:**
- Modify: `deploy/helm/wallow/Chart.yaml`
- Modify: `deploy/helm/wallow/values.yaml`
- Modify: `deploy/helm/wallow/values-*.yaml` (5 environment files)
- Modify: `deploy/helm/wallow/templates/_helpers.tpl`
- Modify: `deploy/helm/wallow/templates/*.yaml` (9 template files)
- Modify: `deploy/helm/wallow/templates/NOTES.txt`

- [ ] **Step 1: Replace in all Helm files**

```bash
cd /Users/traveler/Repos/Wallow
find deploy/helm -type f \( -name '*.yaml' -o -name '*.yml' -o -name '*.tpl' -o -name '*.txt' \) | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

- [ ] **Step 2: Verify Chart.yaml**

```bash
grep -n 'oundry' deploy/helm/wallow/Chart.yaml
```

Expected: 0 results. Chart name should be "wallow".

#### Task 4.3.2: Update Kustomize overlays

**Files:** All files under `deploy/kustomize/`

- [ ] **Step 1: Replace in all Kustomize files**

```bash
cd /Users/traveler/Repos/Wallow
find deploy/kustomize -type f \( -name '*.yaml' -o -name '*.yml' \) | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

#### Task 4.3.3: Update DNS config templates

**Files:**
- Modify: `deploy/dns/route53-config.template.yaml`
- Modify: `deploy/dns/cloudflare-config.template.yaml`

- [ ] **Step 1: Replace and update to wallow.dev domain**

```bash
cd /Users/traveler/Repos/Wallow
find deploy/dns -type f -name '*.yaml' | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry\.example\.com/wallow.dev/g; s/foundry/wallow/g'
```

### Feature 4.4: CI/CD Workflows

#### Task 4.4.1: Update GitHub Actions workflows

**Files:**
- Modify: `.github/workflows/ci.yml`
- Modify: `.github/workflows/publish.yml`
- Modify: `.github/workflows/release-please.yml`
- Modify: `.github/workflows/security.yml`

- [ ] **Step 1: Replace in all workflow files**

```bash
cd /Users/traveler/Repos/Wallow
find .github -type f -name '*.yml' | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

- [ ] **Step 2: Verify solution file reference in ci.yml**

```bash
grep 'slnx' .github/workflows/ci.yml
```

Expected: References `Wallow.slnx`.

- [ ] **Step 3: Verify container registry reference in publish.yml**

```bash
grep -i 'ghcr\|image' .github/workflows/publish.yml
```

Expected: References `wallow` not `foundry`.

#### Task 4.4.2: Update GitHub issue templates

**Files:** All files under `.github/ISSUE_TEMPLATE/`

- [ ] **Step 1: Replace in issue templates**

```bash
cd /Users/traveler/Repos/Wallow
find .github/ISSUE_TEMPLATE -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

### Feature 4.5: Monitoring and Alerting

#### Task 4.5.1: Update Grafana provisioning YAML

**Files:**
- Modify: `docker/grafana/provisioning/alerting/slo-alerts.yml`
- Modify: `docker/grafana/provisioning/alerting/alerting.yml`
- Modify: `docker/grafana/provisioning/dashboards/dashboards.yml`

- [ ] **Step 1: Replace in all provisioning files**

```bash
cd /Users/traveler/Repos/Wallow
find docker/grafana/provisioning -type f \( -name '*.yml' -o -name '*.yaml' \) | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

### Feature 4.6: Tool and IDE Configuration

#### Task 4.6.1: Update Serena project config

**Files:**
- Modify: `.serena/project.yml`

- [ ] **Step 1: Replace project name**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' .serena/project.yml
```

#### Task 4.6.2: Update DotSettings files

**Files:**
- Modify: `Wallow.sln.DotSettings` (already renamed in Epic 2)

- [ ] **Step 1: Replace in DotSettings**

```bash
cd /Users/traveler/Repos/Wallow
find . -name '*.DotSettings*' -type f | xargs sed -i '' 's/Foundry/Wallow/g'
```

#### Task 4.6.3: Update Qodana config

**Files:**
- Modify: `qodana.yaml`

- [ ] **Step 1: Replace if contains Foundry**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' qodana.yaml 2>/dev/null || true
```

#### Task 4.6.4: Update Alloy config (if exists)

- [ ] **Step 1: Replace in alloy config**

```bash
cd /Users/traveler/Repos/Wallow
find docker/alloy -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' 2>/dev/null || true
```

### Feature 4.7: Configuration Verification Gate

#### Task 4.7.1: Comprehensive "Foundry" sweep across all non-CS files

- [ ] **Step 1: Search all remaining files for any "Foundry" references**

```bash
cd /Users/traveler/Repos/Wallow
echo "=== Case-sensitive Foundry ==="
grep -rl 'Foundry' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.xml' --include='*.props' --include='*.targets' --include='*.txt' --include='*.tpl' --include='*.env' --include='*.cshtml' --include='*.sh' --include='*.alloy' . 2>/dev/null | grep -v '.git' | head -20

echo "=== Case-sensitive foundry ==="
grep -rl 'foundry' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.xml' --include='*.props' --include='*.targets' --include='*.txt' --include='*.tpl' --include='*.env' --include='*.cshtml' --include='*.sh' --include='*.alloy' . 2>/dev/null | grep -v '.git' | head -20
```

Expected: 0 results. If any remain, fix them individually.

---

## Epic 5: Documentation

> **Purpose:** Update all markdown documentation, CLAUDE.md files, README, and guides to reference "Wallow" instead of "Foundry". Also update the domain to `wallow.dev`.

### Feature 5.1: Core Documentation

#### Task 5.1.1: Update root CLAUDE.md

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Replace all references**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' CLAUDE.md
```

- [ ] **Step 2: Review and verify key sections are correct**

```bash
head -20 CLAUDE.md
```

Expected: First line reads "# CLAUDE.md" with "Wallow" in the description.

#### Task 5.1.2: Update all other CLAUDE.md files

**Files:**
- Modify: `tests/CLAUDE.md`
- Modify: `src/Wallow.Api/CLAUDE.md`
- Modify: `src/Shared/Wallow.Shared.Kernel/CLAUDE.md`
- Modify: `src/Shared/Wallow.Shared.Contracts/CLAUDE.md`
- Modify: `src/Modules/Identity/CLAUDE.md`
- Modify: `src/Modules/Storage/CLAUDE.md`
- Modify: `src/Modules/Billing/CLAUDE.md`

- [ ] **Step 1: Replace in all CLAUDE.md files**

```bash
cd /Users/traveler/Repos/Wallow
find . -name 'CLAUDE.md' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

#### Task 5.1.3: Update .claude/rules files

**Files:**
- Modify: `.claude/rules/CRITICAL.md`
- Modify: `.claude/rules/COMMITS.md`
- Modify: `.claude/rules/FAILSAFES.md`
- Modify: `.claude/rules/GENERAL.md`
- Modify: `.claude/rules/TEAMS.md`

- [ ] **Step 1: Replace in all rules files**

```bash
cd /Users/traveler/Repos/Wallow
find .claude -name '*.md' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' 2>/dev/null || true
```

### Feature 5.2: Developer and Deployment Documentation

#### Task 5.2.1: Update all docs/ markdown files

**Files:**
- Modify: All `*.md` files under `docs/`

- [ ] **Step 1: Replace in all documentation**

```bash
cd /Users/traveler/Repos/Wallow
find docs -name '*.md' -type f | xargs sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g'
```

#### Task 5.2.2: Update README.md and CONTRIBUTING.md

**Files:**
- Modify: `README.md`
- Modify: `CONTRIBUTING.md`
- Modify: `CHANGELOG.md`
- Modify: `AGENTS.md`

- [ ] **Step 1: Replace in root markdown files**

```bash
cd /Users/traveler/Repos/Wallow
for f in README.md CONTRIBUTING.md CHANGELOG.md AGENTS.md; do
  if [ -f "$f" ]; then
    sed -i '' 's/Foundry/Wallow/g; s/foundry/wallow/g' "$f"
  fi
done
```

#### Task 5.2.3: Reset CHANGELOG.md for v0.1.0

**Files:**
- Modify: `CHANGELOG.md`

- [ ] **Step 1: Replace CHANGELOG with clean v0.1.0 entry**

```bash
cat > /Users/traveler/Repos/Wallow/CHANGELOG.md << 'EOF'
# Changelog

## [0.1.0](https://github.com/bc-solutions-coder/Wallow/releases/tag/v0.1.0) (2026-03-19)

### Features

* Initial release of Wallow — a .NET 10 modular monolith platform
* Modules: Identity, Billing, Storage, Notifications, Messaging, Announcements, Inquiries, Showcases
* Multi-tenancy with per-tenant schema isolation
* Clean Architecture with DDD, CQRS, and RabbitMQ messaging
* Docker, Helm, and Kustomize deployment support
* GitHub Actions CI/CD with conventional commits and release-please
EOF
```

### Feature 5.3: Version Reset

#### Task 5.3.1: Reset version to 0.1.0

**Files:**
- Modify: `Directory.Build.props`
- Modify: `.release-please-manifest.json` (if exists)

- [ ] **Step 1: Update version in Directory.Build.props**

```bash
cd /Users/traveler/Repos/Wallow
sed -i '' 's/<Version>[0-9]*\.[0-9]*\.[0-9]*<\/Version>/<Version>0.1.0<\/Version>/' Directory.Build.props
```

- [ ] **Step 2: Update release-please manifest if exists**

```bash
cd /Users/traveler/Repos/Wallow
if [ -f .release-please-manifest.json ]; then
  sed -i '' 's/"[0-9]*\.[0-9]*\.[0-9]*"/"0.1.0"/' .release-please-manifest.json
fi
```

### Feature 5.4: Documentation Verification Gate

#### Task 5.4.1: Sweep all markdown for remaining Foundry references

- [ ] **Step 1: Search all markdown files**

```bash
cd /Users/traveler/Repos/Wallow
grep -rl 'Foundry\|foundry' --include='*.md' . | grep -v '.git' | head -20
```

Expected: 0 results.

---

## Epic 6: Fresh Database Migrations

> **Purpose:** Generate brand-new `InitialCreate` migrations for every module. Since this is v0.1.0 with no existing databases, we get a clean migration history.
>
> **Prerequisites:** Epic 3 must be complete (solution must build). Docker infrastructure must be running (PostgreSQL).

### Feature 6.1: Prepare for Migration Generation

#### Task 6.1.1: Ensure clean Migrations directories

- [ ] **Step 1: Verify all old migrations were deleted (from Epic 1)**

```bash
cd /Users/traveler/Repos/Wallow
find . -path '*/Migrations/*.cs' -type f | head -5
```

Expected: 0 results (all deleted in Task 1.1.1).

- [ ] **Step 2: Ensure infrastructure is running**

```bash
cd /Users/traveler/Repos/Wallow/docker
docker compose up -d postgres
```

Wait for PostgreSQL to be ready.

- [ ] **Step 3: Build the solution**

```bash
cd /Users/traveler/Repos/Wallow
dotnet build Wallow.slnx
```

Expected: Build succeeds.

### Feature 6.2: Generate Fresh Migrations

> **Note:** Each migration command follows the pattern:
> ```
> dotnet ef migrations add InitialCreate \
>   --project src/Modules/{Module}/Wallow.{Module}.Infrastructure \
>   --startup-project src/Wallow.Api \
>   --context {Module}DbContext
> ```

#### Task 6.2.1: Generate Shared (Audit) migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Shared/Wallow.Shared.Infrastructure.Core \
  --startup-project src/Wallow.Api \
  --context AuditDbContext
```

- [ ] **Step 2: Verify migration created**

```bash
find src/Shared/Wallow.Shared.Infrastructure.Core/Migrations -name '*.cs' | head -5
```

#### Task 6.2.2: Generate Identity migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Identity/Wallow.Identity.Infrastructure \
  --startup-project src/Wallow.Api \
  --context IdentityDbContext
```

#### Task 6.2.3: Generate Billing migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Billing/Wallow.Billing.Infrastructure \
  --startup-project src/Wallow.Api \
  --context BillingDbContext
```

#### Task 6.2.4: Generate Storage migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Storage/Wallow.Storage.Infrastructure \
  --startup-project src/Wallow.Api \
  --context StorageDbContext
```

#### Task 6.2.5: Generate Notifications migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Notifications/Wallow.Notifications.Infrastructure \
  --startup-project src/Wallow.Api \
  --context NotificationsDbContext
```

#### Task 6.2.6: Generate Messaging migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Messaging/Wallow.Messaging.Infrastructure \
  --startup-project src/Wallow.Api \
  --context MessagingDbContext
```

#### Task 6.2.7: Generate Announcements migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Announcements/Wallow.Announcements.Infrastructure \
  --startup-project src/Wallow.Api \
  --context AnnouncementsDbContext
```

#### Task 6.2.8: Generate Showcases migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Showcases/Wallow.Showcases.Infrastructure \
  --startup-project src/Wallow.Api \
  --context ShowcasesDbContext
```

#### Task 6.2.9: Generate Inquiries migration

- [ ] **Step 1: Generate migration**

```bash
cd /Users/traveler/Repos/Wallow
dotnet ef migrations add InitialCreate \
  --project src/Modules/Inquiries/Wallow.Inquiries.Infrastructure \
  --startup-project src/Wallow.Api \
  --context InquiriesDbContext
```

### Feature 6.3: Migration Verification Gate

#### Task 6.3.1: Verify all migrations generated and build passes

- [ ] **Step 1: Count migration files (expect 9 modules × 3 files each = 27 files)**

```bash
cd /Users/traveler/Repos/Wallow
find . -path '*/Migrations/*InitialCreate*' -name '*.cs' | wc -l
```

Expected: 9 (one per module/context).

- [ ] **Step 2: Rebuild to verify migrations compile**

```bash
cd /Users/traveler/Repos/Wallow
dotnet build Wallow.slnx
```

Expected: Build succeeds.

- [ ] **Step 3: Verify no "Foundry" in generated migration files**

```bash
grep -rl 'Foundry' --include='*.cs' /Users/traveler/Repos/Wallow/src/*/Migrations/ 2>/dev/null
grep -rl 'Foundry' --include='*.cs' /Users/traveler/Repos/Wallow/src/Modules/*/Wallow.*.Infrastructure/Migrations/ 2>/dev/null
grep -rl 'Foundry' --include='*.cs' /Users/traveler/Repos/Wallow/src/Modules/*/Wallow.*.Infrastructure/Persistence/Migrations/ 2>/dev/null
```

Expected: 0 results for all (migrations should use Wallow namespaces).

---

## Epic 7: Full Verification

> **Purpose:** Comprehensive testing to verify the entire rename was successful. This is the final quality gate before creating the repository.

### Feature 7.1: Build Verification

#### Task 7.1.1: Clean build from scratch

- [ ] **Step 1: Clean all build artifacts**

```bash
cd /Users/traveler/Repos/Wallow
dotnet clean Wallow.slnx
find . -type d -name 'bin' -exec rm -rf {} + 2>/dev/null || true
find . -type d -name 'obj' -exec rm -rf {} + 2>/dev/null || true
```

- [ ] **Step 2: Full restore and build**

```bash
cd /Users/traveler/Repos/Wallow
dotnet restore Wallow.slnx
dotnet build Wallow.slnx --configuration Release
```

Expected: 0 errors, 0 warnings (ideally).

#### Task 7.1.2: Code format check

- [ ] **Step 1: Run dotnet format verification**

```bash
cd /Users/traveler/Repos/Wallow
dotnet format Wallow.slnx --verify-no-changes 2>&1 | tail -20
```

Expected: No formatting issues (or only pre-existing ones).

### Feature 7.2: Test Verification

#### Task 7.2.1: Run all unit tests

- [ ] **Step 1: Run tests**

```bash
cd /Users/traveler/Repos/Wallow
dotnet test Wallow.slnx --configuration Release --no-build
```

Expected: All tests pass. If tests fail, investigate — likely a missed string literal rename.

- [ ] **Step 2: If tests fail, search for remaining Foundry references in test files**

```bash
grep -rn 'Foundry\|foundry' --include='*.cs' /Users/traveler/Repos/Wallow/tests/ | head -20
```

### Feature 7.3: Content Verification — Final Sweep

#### Task 7.3.1: Exhaustive search for any remaining "Foundry" references

- [ ] **Step 1: Search ALL files in the repository**

```bash
cd /Users/traveler/Repos/Wallow
echo "=== Files containing 'Foundry' (case-sensitive) ==="
grep -rl 'Foundry' . --include='*.cs' --include='*.csproj' --include='*.slnx' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.md' --include='*.props' --include='*.targets' --include='*.xml' --include='*.txt' --include='*.tpl' --include='*.DotSettings' --include='*.cshtml' --include='*.sh' --include='*.alloy' 2>/dev/null | grep -v '.git' | sort

echo ""
echo "=== Files containing 'foundry' (lowercase) ==="
grep -rl 'foundry' . --include='*.cs' --include='*.csproj' --include='*.slnx' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.md' --include='*.props' --include='*.targets' --include='*.xml' --include='*.txt' --include='*.tpl' --include='*.DotSettings' --include='*.env' --include='*.cshtml' --include='*.sh' --include='*.alloy' 2>/dev/null | grep -v '.git' | sort

echo ""
echo "=== Files containing 'FOUNDRY' (uppercase) ==="
grep -rl 'FOUNDRY' . --include='*.cs' --include='*.csproj' --include='*.json' --include='*.yml' --include='*.yaml' --include='*.env' --include='*.sh' 2>/dev/null | grep -v '.git' | sort
```

Expected: 0 results for all three. Any results must be fixed before proceeding.

- [ ] **Step 2: Check directory and file names one final time**

```bash
cd /Users/traveler/Repos/Wallow
find . -iname '*foundry*' -not -path './.git/*' 2>/dev/null
```

Expected: 0 results.

#### Task 7.3.2: Verify key branding strings are correct

- [ ] **Step 1: Check OpenTelemetry service name**

```bash
grep -rn 'service.namespace\|ServiceName\|AddSource\|AddMeter' --include='*.cs' /Users/traveler/Repos/Wallow/src/ | head -10
```

Expected: All reference "Wallow", not "Foundry".

- [ ] **Step 2: Check OpenAPI / health check names**

```bash
grep -rn '"Wallow' --include='*.cs' /Users/traveler/Repos/Wallow/src/ | head -10
```

Expected: `"Wallow API"`, `"Wallow AsyncAPI"`, etc.

- [ ] **Step 3: Check Docker entrypoint**

```bash
grep 'ENTRYPOINT' /Users/traveler/Repos/Wallow/Dockerfile
```

Expected: `ENTRYPOINT ["dotnet", "Wallow.Api.dll"]`

---

## Epic 8: Repository Creation and Initial Commit

> **Purpose:** Initialize the git repository, create the GitHub remote, push the initial commit, and tag v0.1.0.

### Feature 8.1: Git Setup

#### Task 8.1.1: Create .gitignore and initialize repo

- [ ] **Step 1: Verify .gitignore exists and is appropriate**

```bash
head -20 /Users/traveler/Repos/Wallow/.gitignore
```

- [ ] **Step 2: Make sure git is initialized**

```bash
cd /Users/traveler/Repos/Wallow
git init 2>/dev/null || true
```

#### Task 8.1.2: Stage and create initial commit

- [ ] **Step 1: Stage all files**

```bash
cd /Users/traveler/Repos/Wallow
git add -A
```

- [ ] **Step 2: Review what's being committed (high-level)**

```bash
cd /Users/traveler/Repos/Wallow
git status --short | wc -l
echo "---"
git status --short | head -30
```

- [ ] **Step 3: Create initial commit**

```bash
cd /Users/traveler/Repos/Wallow
git commit -m "$(cat <<'EOF'
feat!: initial Wallow v0.1.0 release

.NET 10 modular monolith platform with:
- Modules: Identity, Billing, Storage, Notifications, Messaging,
  Announcements, Inquiries, Showcases
- Multi-tenancy with per-tenant schema isolation
- Clean Architecture with DDD, CQRS, and RabbitMQ messaging
- Docker, Helm, and Kustomize deployment support
- GitHub Actions CI/CD with conventional commits and release-please
- OpenTelemetry observability with Grafana dashboards

Renamed from Foundry to Wallow with fresh migration history.
EOF
)"
```

### Feature 8.2: GitHub Repository

#### Task 8.2.1: Create GitHub repository

- [ ] **Step 1: Create private repo on GitHub**

```bash
gh repo create bc-solutions-coder/Wallow --private --description "Wallow — a .NET 10 modular monolith platform" --source /Users/traveler/Repos/Wallow
```

- [ ] **Step 2: Set remote and push**

```bash
cd /Users/traveler/Repos/Wallow
git remote add origin https://github.com/bc-solutions-coder/Wallow.git 2>/dev/null || true
git branch -M main
git push -u origin main
```

- [ ] **Step 3: Verify push succeeded**

```bash
cd /Users/traveler/Repos/Wallow
git log --oneline -1
git remote -v
```

#### Task 8.2.2: Tag v0.1.0

- [ ] **Step 1: Create and push tag**

```bash
cd /Users/traveler/Repos/Wallow
git tag -a v0.1.0 -m "Wallow v0.1.0 — Initial release"
git push origin v0.1.0
```

- [ ] **Step 2: Create GitHub release**

```bash
gh release create v0.1.0 \
  --repo bc-solutions-coder/Wallow \
  --title "Wallow v0.1.0" \
  --notes "$(cat <<'EOF'
## Wallow v0.1.0 — Initial Release

.NET 10 modular monolith platform featuring:

- **8 Modules:** Identity, Billing, Storage, Notifications, Messaging, Announcements, Inquiries, Showcases
- **Multi-tenancy** with per-tenant PostgreSQL schema isolation
- **Clean Architecture** with DDD, CQRS, and event-driven messaging via RabbitMQ
- **Deployment:** Docker, Helm charts, Kustomize overlays for multi-region
- **CI/CD:** GitHub Actions with conventional commits, release-please, and container publishing
- **Observability:** OpenTelemetry with Grafana dashboards and SLO alerting

Previously known as Foundry, renamed and reset for a clean v0.1.0.
EOF
)"
```

### Feature 8.3: Post-Creation Verification

#### Task 8.3.1: Verify repository is correct on GitHub

- [ ] **Step 1: Verify repo exists and has correct content**

```bash
gh repo view bc-solutions-coder/Wallow --json name,description,defaultBranchRef
```

- [ ] **Step 2: Verify release exists**

```bash
gh release view v0.1.0 --repo bc-solutions-coder/Wallow
```

- [ ] **Step 3: Clone fresh copy and verify it builds**

```bash
cd /tmp
rm -rf wallow-verify
git clone https://github.com/bc-solutions-coder/Wallow.git wallow-verify
cd wallow-verify
dotnet restore Wallow.slnx
dotnet build Wallow.slnx
echo "=== BUILD VERIFICATION PASSED ==="
rm -rf /tmp/wallow-verify
```

Expected: Clone, restore, and build all succeed.

---

## Epic Summary

| Epic | Tasks | Purpose | Can Parallelize |
|------|-------|---------|-----------------|
| **1. Repository Bootstrap** | 1 | Copy codebase, delete migrations | No (foundation) |
| **2. Directory & File Renames** | 8 | Rename all Foundry-named paths | Partially (features within can parallel) |
| **3. Content — Code & Projects** | 6 | Update .slnx, .csproj, .cs, .cshtml | Features 3.1-3.3 can parallel, then 3.4 gate |
| **4. Content — Config & Infra** | 16 | JSON, Docker, K8s, CI/CD, shell scripts, tools | Most features can parallel |
| **5. Documentation** | 6 | Markdown, CLAUDE.md, version reset | All features can parallel |
| **6. Fresh Migrations** | 11 | Generate InitialCreate per module | Module migrations can parallel |
| **7. Full Verification** | 4 | Build, test, final sweep | Sequential (verification) |
| **8. Repo Creation** | 4 | Git init, GitHub, tag, release | Sequential (depends on everything) |

**Total Tasks: 56**

**Dependency Chain:**
```
Epic 1 → Epic 2 → Epic 3 (must build) → [Epic 4 + Epic 5] (parallel) → Epic 6 → Epic 7 → Epic 8
```

**Estimated Sessions:** 3-5 sessions with team agents, or 1-2 sessions with aggressive parallelization.
