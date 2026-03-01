# Identity Module Integration Tests

Integration tests for the API Management Service Accounts feature in the Identity module.

## Test Structure

```
ServiceAccounts/
├── CreateServiceAccountTests.cs       - Test service account creation
├── ListServiceAccountsTests.cs        - Test listing and tenant isolation
├── RotateSecretTests.cs               - Test secret rotation
├── RevokeServiceAccountTests.cs       - Test revocation/deletion
└── ApiScopesTests.cs                  - Test API scopes listing
```

## Test Infrastructure

- **Base Class**: `ServiceAccountIntegrationTestBase` provides common setup
- **Test Containers**: Uses `FoundryApiFactory` which spins up PostgreSQL via Testcontainers
- **Authentication**: Uses `TestAuthHandler` instead of real Keycloak for simplified testing
- **Tenant Context**: Fixed test tenant via `TestConstants.TestTenantId`

## Important Notes

### Keycloak Mocking

These tests use `TestAuthHandler` instead of a real Keycloak instance. This means:
- ✅ We test the service logic, API endpoints, and database persistence
- ✅ We test tenant isolation and authorization requirements
- ❌ We do NOT test the actual OAuth2 client credentials flow
- ❌ We do NOT test real Keycloak client creation/deletion

The `KeycloakServiceAccountService` expects to make HTTP calls to Keycloak's Admin API. In a test environment without Keycloak, these calls will fail. To make these tests pass, you would need to either:

1. **Add Keycloak Testcontainer** (recommended for true end-to-end testing)
2. **Mock the HttpClient** used by `KeycloakServiceAccountService`
3. **Create a test implementation** of `IServiceAccountService` that doesn't call Keycloak

### Running the Tests

```bash
# Build
dotnet build tests/Modules/Identity/Foundry.Identity.IntegrationTests

# Run all tests
dotnet test tests/Modules/Identity/Foundry.Identity.IntegrationTests

# Run specific test class
dotnet test tests/Modules/Identity/Foundry.Identity.IntegrationTests --filter FullyQualifiedName~CreateServiceAccountTests
```

## Test Scenarios

### CreateServiceAccountTests
- Should create service account with valid clientId and secret
- Should create via API endpoint
- Should store metadata correctly
- Should validate input (empty name fails)

### ListServiceAccountsTests
- Should list all service accounts for current tenant
- Should list via API endpoint
- Should enforce tenant isolation
- Should return empty list when none exist

### RotateSecretTests
- Should rotate secret successfully
- Should rotate via API endpoint
- Should fail for non-existent account
- Old secret should be invalidated after rotation

### RevokeServiceAccountTests
- Should revoke account successfully
- Should revoke via API endpoint
- Should fail for non-existent account
- Should not list revoked accounts
- Should prevent operations on revoked accounts

### ApiScopesTests
- Should list all available scopes
- Should list via API endpoint
- Should filter by category
- Should include scope metadata
- Should identify default scopes
- Should return consistent results

## Keycloak Integration Tests (OAuth2/)

Real OAuth2 flow tests using Keycloak Testcontainer:

- **TokenAcquisitionTests**: Validates client credentials flow with real Keycloak tokens
- **TokenValidationTests**: Verifies JWT validation against protected API endpoints
- **ServiceAccountFlowTests**: End-to-end service account authentication and API access

These tests use `KeycloakTestFixture` which starts a real Keycloak instance in a Docker container with a pre-configured realm (`foundry`) containing test clients and users. Unlike the other tests that use `TestAuthHandler`, these tests validate real OAuth2 flows including token issuance and JWT signature verification.

### Running Keycloak Tests

Requires Docker to be running:

```bash
dotnet test tests/Modules/Identity/Foundry.Identity.IntegrationTests --filter FullyQualifiedName~OAuth2
```
