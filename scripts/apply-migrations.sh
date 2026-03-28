#!/usr/bin/env bash
set -euo pipefail

# Migration runner script for the wallow-migrations init container.
# Runs all EF Core migration bundles sequentially, stopping on first failure.
#
# Expects:
#   CONNECTION_STRING  — PostgreSQL connection string

if [[ -z "${CONNECTION_STRING:-}" ]]; then
    echo "ERROR: CONNECTION_STRING environment variable is not set."
    exit 1
fi

# Create schemas for third-party tools that manage their own tables but don't create schemas.
# EF Core module schemas are created by migrationBuilder.EnsureSchema() in the bundles.
echo "Creating third-party schemas (Elsa, Hangfire)..."
psql "${CONNECTION_STRING}" -c 'CREATE SCHEMA IF NOT EXISTS "Elsa"; CREATE SCHEMA IF NOT EXISTS "hangfire";'
echo "Third-party schemas ready."

MODULES=(
    identity
    billing
    storage
    notifications
    messaging
    announcements
    apikeys
    branding
    inquiries
    audit
)

BUNDLE_DIR="/app/bundles"

for module in "${MODULES[@]}"; do
    bundle="${BUNDLE_DIR}/efbundle-${module}"

    echo "Applying migrations for ${module}..."

    if [[ ! -f "${bundle}" ]]; then
        echo "ERROR: Bundle not found: ${bundle}"
        exit 1
    fi

    if "${bundle}" --connection "${CONNECTION_STRING}"; then
        echo "Migrations for ${module} succeeded."
    else
        echo "ERROR: Migrations for ${module} failed."
        exit 1
    fi
done

echo "All migrations applied successfully."
