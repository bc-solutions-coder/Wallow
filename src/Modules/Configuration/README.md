# Configuration Module

The Configuration module provides tenant-configurable custom fields for extending entities with additional data without schema changes.

## Overview

Custom fields allow each tenant to define their own fields on supported entities (Invoice, Payment, Subscription, etc.). Field values are stored as JSONB and validated against the tenant's field definitions.

### Key Features

- **Tenant-scoped field definitions** - Each tenant configures their own fields
- **Multiple field types** - Text, Number, Date, Dropdown, MultiSelect, Email, URL, and more
- **Validation rules** - Min/max length, numeric ranges, regex patterns, required fields
- **JSONB storage** - Flexible storage with efficient GIN indexing
- **Dynamic schema** - API returns field definitions for frontend form rendering

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Configuration Module                      │
├─────────────────────────────────────────────────────────────┤
│  CustomFieldDefinition    - What fields a tenant has        │
│  CustomFieldValidator     - Validates data against schema   │
│  CustomFieldsController   - API for managing definitions    │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Shared.Kernel                          │
├─────────────────────────────────────────────────────────────┤
│  IHasCustomFields         - Interface for customizable      │
│  CustomFieldType          - Enum of field types             │
│  FieldValidationRules     - Validation rule definitions     │
│  CustomFieldRegistry      - Supported entity types          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Consuming Modules                         │
├─────────────────────────────────────────────────────────────┤
│  Invoice, Payment, Subscription implement IHasCustomFields  │
│  Command handlers call ICustomFieldValidator                │
│  JSONB column stores custom field values                    │
└─────────────────────────────────────────────────────────────┘
```

## Supported Entity Types

| Entity | Module | Description |
|--------|--------|-------------|
| Invoice | Billing | Invoices and billing documents |
| Payment | Billing | Payment records |
| Subscription | Billing | Subscription plans |

To add support for additional entities, see [Extending Custom Fields](#extending-custom-fields).

## Field Types

| Type | Description | Validation Options |
|------|-------------|-------------------|
| `Text` | Single-line text | MinLength, MaxLength, Pattern |
| `TextArea` | Multi-line text | MinLength, MaxLength, Pattern |
| `Number` | Integer | Min, Max |
| `Decimal` | Decimal number | Min, Max |
| `Date` | Date only | MinDate, MaxDate |
| `DateTime` | Date and time | MinDate, MaxDate |
| `Boolean` | True/false | - |
| `Dropdown` | Single select | Options list |
| `MultiSelect` | Multiple select | Options list |
| `Email` | Email address | Built-in format validation |
| `Url` | URL | Built-in format validation |
| `Phone` | Phone number | Pattern |

## API Endpoints

### Get Supported Entity Types
```http
GET /api/configuration/custom-fields/entity-types
```

### Get Field Definitions for Entity Type
```http
GET /api/configuration/custom-fields/{entityType}
```

### Create Field Definition
```http
POST /api/configuration/custom-fields
Content-Type: application/json

{
  "entityType": "Invoice",
  "fieldKey": "po_number",
  "displayName": "PO Number",
  "fieldType": "Text",
  "isRequired": true,
  "validationRules": {
    "maxLength": 50,
    "pattern": "^PO-\\d+$",
    "patternMessage": "Must be format PO-####"
  }
}
```

### Update Field Definition
```http
PUT /api/configuration/custom-fields/{id}
Content-Type: application/json

{
  "displayName": "Purchase Order Number",
  "isRequired": false
}
```

### Deactivate Field (Soft Delete)
```http
DELETE /api/configuration/custom-fields/{id}
```

### Reorder Fields
```http
POST /api/configuration/custom-fields/{entityType}/reorder
Content-Type: application/json

{
  "fieldIds": ["guid1", "guid2", "guid3"]
}
```

## Usage Examples

### Creating a Dropdown Field

```http
POST /api/configuration/custom-fields
{
  "entityType": "Invoice",
  "fieldKey": "department",
  "displayName": "Department",
  "fieldType": "Dropdown",
  "isRequired": true,
  "options": [
    { "value": "ACCT", "label": "Accounting" },
    { "value": "SALES", "label": "Sales" },
    { "value": "ENG", "label": "Engineering" }
  ]
}
```

### Creating an Invoice with Custom Fields

```http
POST /api/billing/invoices
{
  "userId": "...",
  "invoiceNumber": "INV-001",
  "currency": "USD",
  "customFields": {
    "po_number": "PO-12345",
    "department": "ACCT"
  }
}
```

### Fetching Invoice with Schema

```http
GET /api/billing/invoices/{id}/with-schema

Response:
{
  "invoice": {
    "id": "...",
    "invoiceNumber": "INV-001",
    "customFields": {
      "po_number": "PO-12345",
      "department": "ACCT"
    }
  },
  "customFieldSchema": [
    {
      "fieldKey": "po_number",
      "displayName": "PO Number",
      "fieldType": "Text",
      "isRequired": true,
      ...
    },
    {
      "fieldKey": "department",
      "displayName": "Department",
      "fieldType": "Dropdown",
      "options": [...]
    }
  ]
}
```

## Querying by Custom Fields

Custom fields are stored as JSONB with GIN indexes for efficient querying:

```csharp
// Find invoices with specific PO number
var invoices = await context.Invoices
    .FindByCustomFieldAsync("po_number", "PO-12345");

// Find invoices matching multiple criteria
var invoices = await context.Invoices
    .FindByCustomFieldsAsync(new Dictionary<string, string>
    {
        ["po_number"] = "PO-12345",
        ["department"] = "ACCT"
    });
```

## Extending Custom Fields

To add custom field support to a new entity:

### 1. Register the Entity Type

```csharp
// In module initialization or CustomFieldRegistry
CustomFieldRegistry.Register("Contact", "CRM", "Customer contacts");
```

### 2. Implement IHasCustomFields

```csharp
public sealed class Contact : AggregateRoot<ContactId>, ITenantScoped, IHasCustomFields
{
    public Dictionary<string, object>? CustomFields { get; private set; }

    public void SetCustomFields(Dictionary<string, object>? customFields)
    {
        CustomFields = customFields;
    }
}
```

### 3. Configure EF Core

```csharp
builder.Property(x => x.CustomFields)
    .HasColumnName("custom_fields")
    .HasColumnType("jsonb")
    .HasConversion(...);
```

### 4. Add Migration

```bash
dotnet ef migrations add AddCustomFieldsToContact ...
```

### 5. Add Validation to Handlers

```csharp
public async Task<ContactDto> Handle(CreateContact command, CancellationToken ct)
{
    var contact = Contact.Create(...);
    contact.SetCustomFields(command.CustomFields);

    var validation = await _customFieldValidator.ValidateAsync(contact, ct);
    if (!validation.IsValid)
        throw new ValidationException(validation.Errors);

    // ...
}
```

### 6. Add GIN Index

```sql
CREATE INDEX ix_contacts_custom_fields_gin
ON crm.contacts USING GIN (custom_fields jsonb_path_ops)
WHERE custom_fields IS NOT NULL;
```

## Database Schema

```sql
-- Field definitions (one per tenant/entity/field combination)
CREATE TABLE configuration.custom_field_definitions (
    id UUID PRIMARY KEY,
    tenant_id UUID NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    field_key VARCHAR(50) NOT NULL,
    display_name VARCHAR(100) NOT NULL,
    description VARCHAR(500),
    field_type VARCHAR(20) NOT NULL,
    display_order INT DEFAULT 0,
    is_required BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    validation_rules JSONB,
    options JSONB,
    created_at TIMESTAMP NOT NULL,
    created_by UUID,
    updated_at TIMESTAMP,
    updated_by UUID,
    UNIQUE (tenant_id, entity_type, field_key)
);

-- Custom field values stored on entities
ALTER TABLE billing.invoices ADD COLUMN custom_fields JSONB;
CREATE INDEX ix_invoices_custom_fields_gin
ON billing.invoices USING GIN (custom_fields jsonb_path_ops);
```

## Dependencies

- **Foundry.Shared.Kernel** - IHasCustomFields, CustomFieldType, validation interfaces
- **PostgreSQL** - JSONB storage and GIN indexing
- **Wolverine** - Command/query handling

## Related Documentation

- [Foundry Design Document](../../docs/plans/2026-02-04-foundry-pivot-design.md)
- [Multi-tenancy](../../docs/DEVELOPER_GUIDE.md#multi-tenancy)
