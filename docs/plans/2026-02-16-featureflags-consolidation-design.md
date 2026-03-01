# FeatureFlags → Configuration Consolidation Design

**Date:** 2026-02-16
**Status:** Approved

## Overview

Consolidate the FeatureFlags module into the Configuration module to reduce deployment complexity, cut maintenance overhead, and improve conceptual clarity. Both modules are simple CRUD settings modules with similar patterns.

## Decision Summary

| Aspect | Decision |
|--------|----------|
| Module name | Configuration |
| Domain structure | Flat (all entities at root) |
| Database schema | Single `configuration` schema |
| API routes | Nested under `/api/configuration/...` |
| Migration approach | Fresh start (drop databases, no data migration) |

## Module Structure

```
src/Modules/Configuration/
  Foundry.Configuration.Domain/
    Entities/
      CustomFieldDefinition.cs
      FeatureFlag.cs
      FeatureFlagOverride.cs
    Enums/
      FieldType.cs
      FlagType.cs

  Foundry.Configuration.Application/
    CustomFields/
      Commands/
      Queries/
    FeatureFlags/
      Commands/
      Queries/

  Foundry.Configuration.Infrastructure/
    ConfigurationDbContext.cs
    Configurations/
      CustomFieldDefinitionConfiguration.cs
      FeatureFlagConfiguration.cs
      FeatureFlagOverrideConfiguration.cs

  Foundry.Configuration.Api/
    Controllers/
      CustomFieldsController.cs
      FeatureFlagsController.cs
```

The FeatureFlags module is deleted entirely after consolidation.

## Database Schema

All tables in the `configuration` schema:

```sql
configuration.custom_field_definitions  -- existing
configuration.feature_flags             -- from FeatureFlags
configuration.feature_flag_overrides    -- from FeatureFlags
```

## API Routes

```
POST   /api/configuration/custom-fields
GET    /api/configuration/custom-fields
GET    /api/configuration/custom-fields/{id}
PUT    /api/configuration/custom-fields/{id}
DELETE /api/configuration/custom-fields/{id}
POST   /api/configuration/custom-fields/reorder

POST   /api/configuration/feature-flags
GET    /api/configuration/feature-flags
GET    /api/configuration/feature-flags/{key}
PUT    /api/configuration/feature-flags/{key}
DELETE /api/configuration/feature-flags/{key}

POST   /api/configuration/feature-flags/{key}/overrides
GET    /api/configuration/feature-flags/{key}/overrides
DELETE /api/configuration/feature-flags/{key}/overrides/{id}
```

## Implementation Steps

1. Move domain entities to Configuration.Domain
2. Move application layer (commands, queries, handlers) to Configuration.Application/FeatureFlags/
3. Extend ConfigurationDbContext with new DbSets and entity configurations
4. Move API controller to Configuration.Api with updated route prefix
5. Remove `AddFeatureFlagsModule()` from FoundryModules.cs
6. Delete FeatureFlags module (all 4 projects)
7. Delete existing migrations, create fresh Configuration migration
8. Move FeatureFlags tests into Configuration test projects
