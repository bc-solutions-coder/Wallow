#!/bin/bash
set -euo pipefail

CERT_DIR="${CERT_DIR:-/app/certs}"
BUNDLE_DIR="${BUNDLE_DIR:-/app/bundles}"

# -------------------------------------------------------
# 1. Generate OpenIddict certificates (if not present)
# -------------------------------------------------------
if [ ! -f "$CERT_DIR/signing.pfx" ] || [ ! -f "$CERT_DIR/encryption.pfx" ]; then
    echo "Generating OpenIddict certificates..."
    mkdir -p "$CERT_DIR"

    openssl req -x509 -nodes -newkey rsa:2048 \
        -keyout /tmp/signing.key -out /tmp/signing.crt \
        -days 3650 -subj "/CN=wallow-signing" 2>/dev/null

    openssl pkcs12 -export \
        -out "$CERT_DIR/signing.pfx" \
        -inkey /tmp/signing.key -in /tmp/signing.crt \
        -password "pass:${OPENIDDICT_SIGNING_CERT_PASSWORD}" 2>/dev/null

    openssl req -x509 -nodes -newkey rsa:2048 \
        -keyout /tmp/encryption.key -out /tmp/encryption.crt \
        -days 3650 -subj "/CN=wallow-encryption" 2>/dev/null

    openssl pkcs12 -export \
        -out "$CERT_DIR/encryption.pfx" \
        -inkey /tmp/encryption.key -in /tmp/encryption.crt \
        -password "pass:${OPENIDDICT_ENCRYPTION_CERT_PASSWORD}" 2>/dev/null

    rm -f /tmp/*.key /tmp/*.crt
    echo "Certificates generated at $CERT_DIR"
else
    echo "Certificates already exist, skipping generation."
fi

# -------------------------------------------------------
# 2. Run EF Core migrations
# -------------------------------------------------------
if [ -z "${CONNECTION_STRING:-}" ]; then
    echo "ERROR: CONNECTION_STRING environment variable is not set."
    exit 1
fi

MODULES=(
    identity billing storage notifications messaging
    announcements apikeys branding inquiries audit
)

for module in "${MODULES[@]}"; do
    bundle="${BUNDLE_DIR}/efbundle-${module}"
    echo "Applying migrations for ${module}..."

    if [ ! -f "${bundle}" ]; then
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

# -------------------------------------------------------
# 3. Start the API
# -------------------------------------------------------
echo "Starting Wallow API..."
exec dotnet Wallow.Api.dll
