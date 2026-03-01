# Configuration Module

## Architecture

- **Type:** EF Core (CRUD)
- **Schema:** `configuration`
- **Multi-tenant:** Partial (FeatureFlag is global, CustomFieldDefinition is tenant-scoped)
- **Events Published:** CustomFieldDefinitionCreatedEvent, CustomFieldDefinitionDeactivatedEvent

## Domain Model

- `FeatureFlag` - Global feature toggles with types: Boolean, Percentage (0-100 rollout), Variant (A/B/C)
- `FeatureFlagOverride` - Tenant or user-level overrides for flags
- `CustomFieldDefinition` - Tenant-defined custom fields (Text, Number, Date, Dropdown, Checkbox, etc.)

## Layer Structure

- **Domain:** FeatureFlag, FeatureFlagOverride, CustomFieldDefinition entities, domain events
- **Application:** Commands (CreateFeatureFlag, UpdateFeatureFlag, DeleteFeatureFlag, CreateOverride, DeleteOverride, CreateCustomFieldDefinition, UpdateCustomFieldDefinition, DeactivateCustomFieldDefinition, ReorderCustomFields), Queries (GetAllFlags, GetFlagByKey, GetOverridesForFlag, GetCustomFieldDefinitions, GetSupportedEntityTypes), FeatureFlagService, CustomFieldValidator
- **Infrastructure:** ConfigurationDbContext, CustomFieldIndexManager (PostgreSQL GIN indexes for JSONB queries)
- **API:** FeatureFlagsController (admin), CustomFieldsController (admin + user)

## Key Details

- **Custom fields** are stored as JSONB dictionaries on entities. Validate via `CustomFieldValidator`. GIN indexes are managed by `CustomFieldIndexManager`.

## Conventions

- Feature flags are global; overrides provide tenant/user specificity
- Percentage flags use `hash(userId) % 100` for stable rollout
- Variant flags support A/B/C testing

## Known Issues

- No flag evaluation service for percentage/variant flags (manual implementation needed)
- No caching for feature flags (DB query every time)
- No versioning strategy for custom field schema changes
