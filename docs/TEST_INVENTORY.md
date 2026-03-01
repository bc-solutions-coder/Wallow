# Test Inventory

Comprehensive catalog of all test projects in the Foundry codebase. Counts sourced from `dotnet test --list-tests`.

## Summary

| Project | Tests | Layer Coverage |
|---------|-------|----------------|
| Foundry.Billing.Tests | 581 | Domain, Application, Infrastructure, Api, Integration |
| Foundry.Storage.Tests | 240 | Domain, Application, Infrastructure, Api, Integration |
| Foundry.Configuration.Tests | 451 | Domain, Application, Infrastructure, Api |
| Foundry.Communications.Tests | 629 | Domain, Application, Infrastructure, Api |
| Foundry.Identity.Tests | 819 | Domain, Application, Infrastructure, Api, Integration |
| Foundry.Identity.IntegrationTests | 55 | OAuth2, ServiceAccounts, Scim, Sso |
| Foundry.Shared.Kernel.Tests | 287 | Domain, Identity, MultiTenancy, Results, Extensions, CustomFields, Plugins, Diagnostics, Messaging |
| Foundry.Shared.Infrastructure.Tests | 329 | Auditing, BackgroundJobs, Persistence, Plugins, AsyncApi, Workflows, Middleware, Services |
| Foundry.Api.Tests | 215 | Extensions, Middleware, Services, Hubs, Health, Integration, Jobs, Logging |
| Foundry.Architecture.Tests | 112 | CleanArchitecture, ModuleIsolation, CQRS, Wolverine, ApiVersioning, MultiTenancy |
| Foundry.Messaging.IntegrationTests | 12 | CrossModule, PublishConsume, Retry, DeadLetter |
| **Total** | **3,730** | |

---

## 1. Billing Module (581 tests)

### Domain (102 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| MoneyTests | 23 | Money value object: creation, validation, arithmetic, equality, formatting |
| MeterDefinitionCreateTests | 12 | MeterDefinition.Create with codes, display names, units, and Valkey key patterns |
| InvoiceLineItemTests | 9 | Adding/removing line items, total recalculation, and status guards |
| MeterDefinitionUpdateTests | 8 | Updating meter definition properties, code immutability |
| QuotaDefinitionCreatePlanQuotaTests | 8 | Creating plan-level quota definitions with limits and actions |
| InvoiceCreateTests | 7 | Invoice.Create factory method, validation, and domain event raising |
| UsageRecordCreateTests | 7 | UsageRecord.Create with period validation and meter codes |
| QuotaDefinitionCreateTenantOverrideTests | 6 | Creating tenant-specific quota overrides |
| SubscriptionCreateTests | 6 | Subscription.Create, SubscriptionCreatedDomainEvent, and custom fields |
| InvoiceMarkAsPaidTests | 5 | Marking invoices as paid, status transitions, and InvoicePaidDomainEvent |
| SubscriptionCancelTests | 5 | Cancellation with CancelledAt timestamp and domain event |
| InvoiceCancelTests | 4 | Cancellation from draft/issued/overdue states, rejects paid |
| InvoiceIssueTests | 4 | Issuing invoices from draft status with line item requirements |
| InvoiceMarkAsOverdueTests | 4 | Overdue status transition from issued invoices |
| PaymentCompleteTests | 4 | Completing pending payments with transaction references |
| PaymentCreateTests | 4 | Payment.Create factory, PaymentReceivedDomainEvent, and custom fields |
| PaymentFailTests | 4 | Failing payments with reasons and PaymentFailedDomainEvent |
| PaymentRefundTests | 4 | Refunding completed payments, rejects non-completed |
| SubscriptionExpireTests | 4 | Expiring subscriptions with EndDate |
| SubscriptionMarkPastDueTests | 4 | Marking subscriptions as past due, rejects invalid transitions |
| SubscriptionRenewTests | 4 | Renewing active subscriptions, rejects past-due/cancelled/expired |
| UsageRecordAddValueTests | 4 | Adding usage values to existing records |
| QuotaDefinitionUpdateLimitTests | 2 | Updating quota limits and actions |

### Application / Event Handlers (20 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| InvoicePaidDomainEventHandlerTests | 6 | Publishing InvoicePaidEvent, handles missing invoices with warning log |
| InvoiceCreatedDomainEventHandlerTests | 5 | Publishing InvoiceCreatedEvent integration event from domain event |
| InvoiceOverdueDomainEventHandlerTests | 5 | Publishing InvoiceOverdueEvent integration event |
| PaymentReceivedDomainEventHandlerTests | 4 | Publishing PaymentReceivedEvent integration event with tenant context |

### Application / Validators (47 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CreateSubscriptionValidatorTests | 12 | Validation for CreateSubscriptionCommand (UserId, PlanName, Price, Currency, PeriodEnd) |
| AddLineItemValidatorTests | 10 | Validation for AddLineItemCommand (InvoiceId, Description, UnitPrice, Quantity) |
| ProcessPaymentValidatorTests | 10 | Validation for ProcessPaymentCommand (InvoiceId, UserId, Amount, Currency, PaymentMethod) |
| CreateInvoiceValidatorTests | 9 | Validation for CreateInvoiceCommand (UserId, InvoiceNumber, Currency) |
| CancelInvoiceValidatorTests | 4 | Validation for CancelInvoiceCommand (InvoiceId, CancelledByUserId) |
| CancelSubscriptionValidatorTests | 4 | Validation for CancelSubscriptionCommand (SubscriptionId, CancelledByUserId) |
| IssueInvoiceValidatorTests | 4 | Validation for IssueInvoiceCommand (InvoiceId, IssuedByUserId) |

### Application / Handlers (25 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CreateInvoiceHandlerTests | 5 | Invoice creation, duplicate detection, custom fields, concurrent creation |
| ProcessPaymentHandlerTests | 5 | Payment processing, not-found invoices, invalid methods, concurrent payments |
| AddLineItemHandlerTests | 3 | Adding line items to invoices |
| CancelInvoiceHandlerTests | 3 | Invoice cancellation, handles not-found |
| CancelSubscriptionHandlerTests | 2 | Subscription cancellation, handles not-found |
| CreateSubscriptionHandlerTests | 2 | Subscription creation with different currencies |
| IssueInvoiceHandlerTests | 2 | Issuing invoices with line items, handles not-found |

### Application / Queries (20 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| GetSubscriptionsByUserIdHandlerTests | 3 | Querying subscriptions by user ID |
| GetSubscriptionByIdHandlerTests | 3 | Querying subscription by ID |
| GetPaymentsByInvoiceIdHandlerTests | 3 | Querying payments by invoice ID |
| GetPaymentByIdHandlerTests | 3 | Querying payment by ID |
| GetInvoicesByUserIdHandlerTests | 3 | Querying invoices by user ID |
| GetInvoiceByIdHandlerTests | 3 | Querying invoice by ID |
| GetAllInvoicesHandlerTests | 2 | Querying all invoices |

### Application / Metering (40 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SetQuotaOverrideValidatorTests | 8 | Validator for SetQuotaOverride command |
| SetQuotaOverrideHandlerTests | 6 | Setting tenant quota overrides, idempotency, concurrent tenants |
| GetQuotaStatusHandlerTests | 6 | Querying quota status with usage vs. limits |
| GetCurrentUsageHandlerTests | 4 | Querying current usage with/without meter code filter |
| GetUsageHistoryHandlerTests | 4 | Querying usage history |
| RemoveQuotaOverrideValidatorTests | 4 | Validator for RemoveQuotaOverride command |
| GetMeterDefinitionsHandlerTests | 3 | Querying meter definitions |
| UsageFlushedDomainEventHandlerTests | 3 | Handling usage flush domain events |
| RemoveQuotaOverrideHandlerTests | 2 | Removing quota overrides, handles not-found |
| QuotaThresholdReachedDomainEventHandlerTests | 2 | Handling quota threshold domain events |

### Api / Controllers (88 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| InvoicesControllerTests | 31 | Invoice REST endpoints: CRUD, issue, cancel, list |
| SubscriptionsControllerTests | 17 | Subscription REST endpoints: create, renew, cancel |
| PaymentsControllerTests | 14 | Payment REST endpoints: process, complete, fail, refund |
| UsageControllerTests | 11 | Usage REST endpoints: query, history |
| QuotasControllerTests | 11 | Quota REST endpoints: status, overrides |
| MetersControllerTests | 4 | Meter definition REST endpoints |

### Api / Metering (15 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| MeteringMiddlewareTests | 15 | API metering middleware: route skipping, 429 responses, quota headers, increment logic |

### Api / Contracts & Extensions (33 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ResultExtensionsTests | 19 | Result-to-ActionResult mapping extensions |
| RequestContractTests | 7 | Request DTO contract validation |
| ResponseContractTests | 7 | Response DTO contract validation |

### Infrastructure / Metering (34 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ValkeyMeteringServiceAdditionalTests | 11 | Additional Valkey/Redis metering edge cases |
| ValkeyMeteringServiceTests | 9 | Valkey/Redis-based metering: increment, key format, quota checks |
| FlushUsageJobAdditionalTests | 8 | Additional flush job scenarios |
| ParsePeriodTests | 7 | Period string parsing for monthly, daily, and hourly formats |
| FlushUsageJobTests | 5 | Background job that flushes Valkey counters to PostgreSQL |
| FlushUsageJobExceptionTests | 5 | Flush job exception handling |

### Infrastructure / Persistence (62 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| QuotaDefinitionRepositoryTests | 9 | EF Core repository for quota definitions |
| InvoiceRepositoryTests | 9 | EF Core repository for invoices |
| SubscriptionRepositoryTests | 7 | EF Core repository for subscriptions |
| MeteringDbSeederTests | 7 | Database seeding for metering data |
| PaymentRepositoryTests | 6 | EF Core repository for payments |
| DesignTimeTenantContextTests | 6 | Design-time tenant context for migrations |
| MeterDefinitionRepositoryTests | 5 | EF Core repository for meter definitions |
| InvoiceRepositoryExtensionsTests | 5 | Invoice repository extension methods |
| BillingDbContextFactoryTests | 5 | DbContext factory for design-time |

### Infrastructure / Services (20 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| MeteringQueryServiceTests | 7 | Metering query service for reporting |
| UsageReportServiceTests | 5 | Usage report generation |
| SubscriptionQueryServiceTests | 4 | Subscription query service |
| PaymentReportServiceTests | 2 | Payment report generation |
| InvoiceReportServiceTests | 2 | Invoice report generation |
| RevenueReportServiceTests | 1 | Revenue report generation |
| InvoiceQueryServiceTests | 1 | Invoice query service |

### Infrastructure / Other (10 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| DapperQueryTests | 9 | Dapper SQL queries for payment, invoice, and revenue reports |
| InvoiceCreatedTriggerTests | 1 | Elsa workflow trigger for invoice creation |

### Integration (7 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| UsageRecordRepositoryTests | 7 | EF Core repository for usage records: CRUD, history queries, tenant isolation |

---

## 2. Storage Module (240 tests)

### Domain (27 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| StorageBucketTests | 23 | Bucket creation, content type filtering (wildcards, exact match, case insensitive), file size limits, updates |
| StoredFileTests | 4 | StoredFile.Create, default values, metadata updates, public flag |

### Application (82 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CreateBucketValidatorTests | 23 | Validation for CreateBucketCommand |
| UploadFileValidatorTests | 21 | Validation for UploadFileCommand |
| GetUploadPresignedUrlHandlerTests | 7 | Upload presigned URL generation |
| UploadFileHandlerTests | 6 | File upload: bucket lookup, content type/size validation, storage key format |
| CreateBucketHandlerTests | 6 | Bucket creation: duplicate detection, retention policies |
| GetPresignedUrlHandlerTests | 5 | Presigned URL generation: tenant verification, custom/default expiry |
| DeleteBucketValidatorTests | 5 | Validation for DeleteBucketCommand |
| DeleteBucketHandlerTests | 5 | Bucket deletion: not-found, non-empty guard, force delete |
| GetFilesByBucketHandlerTests | 4 | Listing files by bucket with tenant filtering and path prefix |
| StorageMappingsTests | 4 | DTO mapping tests |
| DeleteFileValidatorTests | 4 | Validation for DeleteFileCommand |
| GetFileByIdHandlerTests | 3 | File lookup by ID with tenant verification |
| DeleteFileHandlerTests | 3 | File deletion: not-found, tenant isolation, storage cleanup |
| GetBucketByNameHandlerTests | 2 | Bucket lookup by name, not-found handling |
| ApplicationExtensionsTests | 1 | Application DI registration |

### Infrastructure (22 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| S3StorageProviderTests | 12 | S3-compatible storage: PutObject, GetObject, DeleteObject, presigned URLs, error handling |
| LocalStorageProviderTests | 10 | Local filesystem storage: upload, download, delete, exists, presigned URLs |

### Api (82 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| StorageControllerTests | 55 | Storage REST endpoints: upload, download, delete, presigned URLs, buckets |
| ResultExtensionsTests | 19 | Result-to-ActionResult mapping |
| ResponseContractTests | 8 | Response DTO contract validation |
| RequestContractTests | 4 | Request DTO contract validation |

### Integration (6 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| StoredFileRepositoryTests | 6 | EF Core repository: CRUD, bucket/path filtering, tenant isolation |

---

## 3. Configuration Module (451 tests)

### Domain (127 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CustomFieldDefinitionValidationRulesTests | 31 | Custom field validation rules (min/max length, regex, range) |
| CustomFieldDefinitionCreateTests | 18 | Creating custom field definitions with types, options, and validation |
| CustomFieldDefinitionUpdateTests | 11 | Updating custom field definitions |
| CustomFieldDefinitionOptionsTests | 7 | Custom field option management |
| FeatureFlagCreatePercentageTests | 7 | Creating percentage-based rollout flags with validation |
| VariantWeightConstructionTests | 6 | Variant weight value object construction |
| CustomFieldDefinitionLifecycleTests | 5 | Custom field lifecycle (activate/deactivate) |
| FeatureFlagUpdatePercentageTests | 5 | Updating rollout percentage, disabling at 0%, type guard |
| FeatureFlagSetVariantsTests | 4 | Replacing variants, empty list guard, default variant check |
| FeatureFlagOverrideCreateForTenantTests | 4 | Creating tenant-level overrides with variants and expiration |
| VariantWeightEqualityTests | 4 | Variant weight equality comparisons |
| FeatureFlagOverrideExpirationTests | 3 | Override expiration: past, future, no-expiration |
| FeatureFlagCreateVariantTests | 3 | Creating variant (A/B test) flags with weighted variants |
| FeatureFlagCreateBooleanTests | 3 | Creating boolean feature flags with enabled/disabled defaults |
| FeatureFlagIdTests | 3 | Feature flag ID generation and comparison |
| CustomFieldDefinitionIdTests | 3 | Custom field definition ID generation |
| CustomFieldExceptionTests | 3 | Custom field exception handling |
| FeatureFlagUpdateTests | 2 | Updating flag name, description, and enabled state |
| FeatureFlagOverrideCreateForUserTests | 2 | Creating user-level overrides with variants |
| FeatureFlagOverrideCreateForTenantUserTests | 2 | Creating tenant+user overrides with combined scope |
| FeatureFlagOverrideIdTests | 2 | Override ID generation |
| FeatureFlagEvaluatedEventTests | 2 | Feature flag evaluated domain event |
| FeatureFlagOverridesCollectionTests | 1 | New flags have empty overrides collection |
| FeatureFlagOverrideIdGenerationTests | 1 | Unique ID generation for overrides |
| FeatureFlagUpdatedEventTests | 1 | Flag updated domain event |
| FeatureFlagDeletedEventTests | 1 | Flag deleted domain event |
| FeatureFlagCreatedEventTests | 1 | Flag created domain event |
| CustomFieldDefinitionDeactivatedEventTests | 1 | Deactivated domain event |
| CustomFieldDefinitionCreatedEventTests | 1 | Created domain event |

### Application / Handlers (59 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| UpdateCustomFieldDefinitionHandlerTests | 8 | Updating custom field definitions: validation, not-found |
| CreateOverrideHandlerTests | 7 | Override creation: tenant/user/tenant+user scopes, conflict detection, cache invalidation |
| UpdateFeatureFlagHandlerTests | 6 | Updating feature flags |
| CreateCustomFieldDefinitionHandlerTests | 6 | Custom field definition creation: duplicate key detection |
| CreateFeatureFlagHandlerTests | 5 | Feature flag creation: boolean/percentage/variant types, duplicate detection |
| ReorderCustomFieldsHandlerTests | 4 | Reordering custom field display order |
| GetCustomFieldDefinitionsHandlerTests | 4 | Querying custom field definitions |
| DeleteOverrideHandlerTests | 4 | Deleting feature flag overrides |
| DeleteFeatureFlagHandlerTests | 4 | Deleting feature flags |
| GetSupportedEntityTypesHandlerTests | 3 | Querying supported entity types |
| GetOverridesForFlagHandlerTests | 3 | Querying overrides for a flag |
| GetFlagByKeyHandlerTests | 3 | Querying flag by key |
| GetAllFlagsHandlerTests | 3 | Querying all flags |
| DeactivateCustomFieldDefinitionHandlerTests | 3 | Deactivating custom fields |
| GetCustomFieldDefinitionByIdHandlerTests | 2 | Querying custom field by ID |

### Application / Mappings (13 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CustomFieldDefinitionMapperTests | 7 | Custom field DTO mapping |
| FeatureFlagMappingsTests | 6 | Feature flag DTO mapping |

### Application / Services (32 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CustomFieldValidatorTests | 32 | Custom field runtime validation service |

### Infrastructure (93 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CustomFieldIndexManagerValidationTests | 25 | Custom field index manager SQL injection protection and validation |
| FeatureFlagServiceIsEnabledTests | 12 | Feature flag IsEnabled evaluation logic |
| FeatureFlagServiceGetVariantTests | 6 | Feature flag GetVariant evaluation |
| ConfigurationModuleExtensionsTests | 6 | Module DI registration |
| CachedFeatureFlagServiceIsEnabledTests | 5 | Cached feature flag IsEnabled with Redis |
| FeatureFlagServiceGetAllFlagsTests | 4 | GetAllFlags query service |
| CachedFeatureFlagServiceGetVariantTests | 3 | Cached GetVariant with Redis |
| CachedFeatureFlagServiceInvalidateTests | 1 | Cache invalidation |
| CachedFeatureFlagServiceGetAllFlagsTests | 1 | Cached GetAllFlags |

### Infrastructure / Persistence (43 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ConfigurationDbContextModelTests | 12 | DbContext model configuration tests |
| CustomFieldDefinitionRepositoryTests | 11 | EF Core repository for custom field definitions |
| FeatureFlagOverrideRepositoryTests | 9 | EF Core repository for feature flag overrides |
| FeatureFlagRepositoryTests | 8 | EF Core repository for feature flags |
| ConfigurationDbContextFactoryTests | 2 | DbContext factory for design-time |
| DesignTimeTenantContextTests | 1 | Design-time tenant context |

### Api (84 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| FeatureFlagsControllerTests | 32 | Feature flag REST endpoints |
| ResultExtensionsTests | 19 | Result-to-ActionResult mapping |
| CustomFieldsControllerTests | 17 | Custom field REST endpoints |
| EnumMappingsTests | 14 | Enum-to-string mapping tests |
| RequestContractTests | 11 | Request DTO contract validation |
| ResponseContractTests | 4 | Response DTO contract validation |

---

## 4. Communications Module (629 tests)

### Domain / Email (37 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| EmailAddressCreateTests | 8 | EmailAddress value object creation and validation |
| EmailContentCreateTests | 8 | EmailContent value object creation |
| EmailPreferenceStateTests | 8 | Email preference state management |
| EmailPreferenceCreateTests | 4 | EmailPreference creation |
| InvalidEmailAddressExceptionTests | 3 | Exception for invalid emails |
| EmailContentEqualityTests | 3 | EmailContent equality comparisons |
| EmailAddressEqualityTests | 3 | EmailAddress equality comparisons |
| EmailAddressConversionTests | 2 | EmailAddress type conversions |

### Domain / InApp (9 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| EmailMessageSendingTests | 4 | Marking emails as sent/failed, domain events, retry count |
| EmailMessageRetryTests | 3 | Retry reset, retry limit enforcement |
| EmailMessageCreateTests | 2 | EmailMessage.Create with sender/null sender |

### Domain / Notifications (5 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| NotificationMarkAsReadTests | 3 | Marking notifications as read, ReadAt timestamp, repeat reads |
| NotificationCreateTests | 2 | Notification.Create with unread state and NotificationCreatedDomainEvent |

### Domain / Announcements (62 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ChangelogEntryUpdateTests | 11 | Updating changelog entries |
| ChangelogEntryCreateTests | 11 | Creating changelog entries |
| AnnouncementCreateTests | 11 | Creating announcements |
| AnnouncementUpdateTests | 8 | Updating announcements |
| AnnouncementStatusTests | 6 | Announcement status transitions |
| ChangelogEntryItemTests | 6 | Changelog entry item management |
| ChangelogItemCreateTests | 5 | Creating changelog items |
| ChangelogItemUpdateTests | 4 | Updating changelog items |
| ChangelogEntryPublishTests | 4 | Publishing changelog entries |
| AnnouncementDismissalCreateTests | 2 | Creating announcement dismissals |

### Domain / Identity & Events (20 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CommunicationsStronglyTypedIdTests | 16 | Strongly-typed ID creation and comparison |
| NotificationReadDomainEventTests | 1 | Notification read event |
| NotificationCreatedDomainEventTests | 1 | Notification created event |
| EmailSentDomainEventTests | 1 | Email sent event |
| EmailFailedDomainEventTests | 1 | Email failed event |

### Application / Announcements (87 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CreateAnnouncementValidatorTests | 12 | Validation for CreateAnnouncementCommand |
| AnnouncementTargetingServiceTests | 12 | Announcement targeting service |
| AnnouncementTargetingEdgeCaseTests | 9 | Targeting service edge cases |
| CreateChangelogEntryValidatorTests | 8 | Validation for CreateChangelogEntryCommand |
| CreateAnnouncementHandlerTests | 7 | Announcement creation handler |
| ChangelogMappingTests | 6 | Changelog DTO mapping |
| GetChangelogHandlerTests | 5 | Querying changelog |
| DismissAnnouncementHandlerTests | 5 | Dismissing announcements |
| GetChangelogByVersionHandlerTests | 4 | Querying changelog by version |
| GetAllAnnouncementsHandlerTests | 4 | Querying all announcements |
| GetActiveAnnouncementsHandlerTests | 4 | Querying active announcements |
| UpdateAnnouncementHandlerTests | 4 | Updating announcements |
| PublishChangelogEntryHandlerTests | 4 | Publishing changelog entries |
| PublishAnnouncementHandlerTests | 4 | Publishing announcements |
| CreateChangelogEntryHandlerTests | 4 | Creating changelog entries |
| ArchiveAnnouncementHandlerTests | 4 | Archiving announcements |
| UpdateAnnouncementValidatorTests | 4 | Validation for UpdateAnnouncementCommand |
| GetLatestChangelogHandlerTests | 3 | Querying latest changelog |
| CreateChangelogEntryMappingTests | 3 | Changelog entry DTO mapping |
| DismissAnnouncementValidatorTests | 3 | Validation for dismiss command |
| PublishChangelogEntryValidatorTests | 2 | Validation for publish command |
| PublishAnnouncementValidatorTests | 2 | Validation for publish command |
| ArchiveAnnouncementValidatorTests | 2 | Validation for archive command |
| AnnouncementDtoTests | 3 | Announcement DTO tests |

### Application / Channels / Email (53 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SendEmailValidatorTests | 8 | Validation for SendEmailCommand |
| SendEmailHandlerTests | 8 | Email sending handler |
| UpdateEmailPreferencesValidatorTests | 7 | Validation for UpdateEmailPreferencesCommand |
| EmailMappingsTests | 6 | Email DTO mapping |
| UpdateEmailPreferencesHandlerTests | 5 | Updating email preferences |
| UserRegisteredEmailEventHandlerTests | 5 | Handling user registration email event |
| SendEmailRequestedEventHandlerTests | 5 | Handling send email requested event |
| PasswordResetRequestedEventHandlerTests | 5 | Handling password reset event |
| GetEmailPreferencesHandlerTests | 4 | Querying email preferences |

### Application / Channels / InApp (42 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| AnnouncementPublishedEventHandlerTests | 12 | Handling announcement published event |
| AnnouncementPublishedMarkdownTests | 10 | Markdown processing for announcements |
| SendNotificationValidatorTests | 5 | Validation for SendNotificationCommand |
| SendNotificationHandlerTests | 5 | Notification sending handler |
| GetUserNotificationsHandlerTests | 4 | Querying user notifications |
| MarkNotificationReadHandlerTests | 4 | Marking notifications as read |
| UserRegisteredInAppEventHandlerTests | 4 | Handling user registration notification event |
| GetUnreadCountHandlerTests | 3 | Querying unread count |
| MarkAllNotificationsReadHandlerTests | 3 | Marking all notifications as read |
| NotificationMappingsTests | 2 | Notification DTO mapping |

### Application / Other (21 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| NotificationsModuleTelemetryTests | 6 | Notification module telemetry |
| EmailModuleTelemetryTests | 6 | Email module telemetry |
| EmailDtoTests | 4 | Email DTO tests |
| ApplicationExtensionsTests | 3 | Application DI registration |
| InAppApplicationExtensionsTests | 2 | InApp DI registration |
| EmailApplicationExtensionsTests | 2 | Email DI registration |

### Infrastructure / Services (52 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SimpleEmailTemplateServiceTests | 16 | Email template rendering |
| SmtpEmailServiceTests | 15 | SMTP email sending |
| SignalRNotificationServiceEdgeCaseTests | 10 | SignalR notification edge cases |
| SimpleEmailTemplateServiceEdgeCaseTests | 8 | Template service edge cases |
| SignalRNotificationServiceTests | 6 | SignalR real-time notifications |
| SmtpSettingsTests | 2 | SMTP configuration validation |

### Infrastructure / Persistence (50 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ChangelogRepositoryTests | 12 | EF Core repository for changelog entries |
| CommunicationsDbContextTests | 9 | DbContext configuration tests |
| NotificationRepositoryTests | 9 | EF Core repository for notifications |
| EmailPreferenceRepositoryTests | 7 | EF Core repository for email preferences |
| AnnouncementRepositoryTests | 7 | EF Core repository for announcements |
| EmailMessageRepositoryTests | 6 | EF Core repository for email messages |
| AnnouncementDismissalRepositoryTests | 4 | EF Core repository for dismissals |

### Api (100 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ResultExtensionsTests | 21 | Result-to-ActionResult mapping |
| NotificationsControllerTests | 18 | Notification REST endpoints |
| AdminAnnouncementsControllerTests | 17 | Admin announcement REST endpoints |
| ResponseContractTests | 11 | Response DTO contract validation |
| EnumMappingsTests | 10 | Enum-to-string mapping tests |
| EmailPreferencesControllerTests | 10 | Email preferences REST endpoints |
| ChangelogControllerTests | 10 | Changelog REST endpoints |
| AnnouncementsControllerTests | 10 | Announcement REST endpoints (public) |
| AdminChangelogControllerTests | 8 | Admin changelog REST endpoints |
| RequestContractTests | 6 | Request DTO contract validation |

---

## 5. Identity Module - Unit Tests (819 tests)

### Domain (63 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SsoConfigurationTests | 30 | SSO configuration entity: create, update, activate, disable |
| ScimConfigurationTests | 10 | SCIM configuration entity tests |
| ServiceAccountMetadataTests | 11 | Service account lifecycle: create, mark used, revoke, update scopes |
| ApiScopeTests | 8 | ApiScope.Create with code/display name/category validation |
| ScimSyncLogTests | 4 | SCIM synchronization log entity |

### Application / Commands (10 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| UpdateServiceAccountScopesCommandTests | 3 | Handler mapping command to scope update |
| RevokeServiceAccountCommandTests | 3 | Handler delegation to service for revocation |
| CreateServiceAccountCommandTests | 2 | Handler mapping command to creation request |
| RotateServiceAccountSecretCommandTests | 2 | Handler delegation for secret rotation |

### Application / Queries (11 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| GetApiScopesQueryTests | 5 | API scope listing with category filter, DTO mapping |
| GetServiceAccountsQueryTests | 3 | Service account listing, empty results |
| GetServiceAccountQueryTests | 3 | Service account by ID query |

### Application / Validators (12 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CreateServiceAccountValidatorTests | 9 | Validation: name required/max length, description max length, scopes required |
| UpdateServiceAccountScopesValidatorTests | 3 | Validation for scope update command |

### Application / Extensions (2 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ApplicationExtensionsTests | 2 | Application DI registration |

### Infrastructure (528 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ScimFilterLexerTests | 48 | SCIM filter tokenization |
| ScimFilterParserTests | 42 | SCIM filter expression parsing (eq, co, sw, and, or, not) |
| ScimUserServiceTests | 27 | SCIM user provisioning/deprovisioning operations |
| ScimServiceTests | 27 | SCIM 2.0 service endpoint handling |
| KeycloakSsoServiceTests | 26 | Keycloak SSO provider configuration and management |
| KeycloakIdpServiceTests | 25 | Keycloak identity provider CRUD operations |
| ScimToKeycloakTranslatorAdditionalTests | 22 | Additional SCIM-to-Keycloak attribute mapping edge cases |
| KeycloakAdminServiceTests | 18 | Keycloak admin API client for user/role management |
| SsoClaimsSyncServiceTests | 18 | SSO claims synchronization service |
| ScimUserServiceAdditionalTests | 17 | Additional SCIM user service edge cases |
| ScimGroupServiceTests | 17 | SCIM group provisioning operations |
| ScimAuthenticationMiddlewareTests | 17 | SCIM endpoint authentication middleware |
| KeycloakOrganizationServiceGapTests | 16 | Keycloak organization service coverage gaps |
| KeycloakAdminServiceGapTests | 16 | Keycloak admin service coverage gaps |
| RedisApiKeyServiceTests | 14 | Redis-backed API key storage and validation |
| KeycloakOrganizationServiceTests | 14 | Keycloak organization/tenant management |
| ScimToKeycloakTranslatorTests | 13 | SCIM-to-Keycloak attribute translation |
| PermissionExpansionMiddlewareTests | 13 | Permission expansion in HTTP request pipeline |
| TenantResolutionMiddlewareTests | 12 | Tenant resolution from HTTP requests |
| KeycloakOrganizationServiceAdditionalTests | 11 | Additional organization service scenarios |
| ServiceAccountTrackingMiddlewareTests | 9 | Service account usage tracking middleware |
| KeycloakSsoServiceGapTests | 9 | SSO service coverage gaps |
| KeycloakServiceAccountServiceAdditionalTests | 9 | Additional service account service scenarios |
| UserQueryServiceTests | 8 | User query service for lookups |
| KeycloakTokenServiceTests | 8 | Keycloak token acquisition and validation |
| SsoClaimsSyncServiceAdditionalTests | 7 | Additional SSO claims sync edge cases |
| ScimFilterExceptionTests | 7 | SCIM filter parsing error handling |
| KeycloakAdminServiceAdditionalTests | 7 | Additional admin service scenarios |
| RolePermissionMappingTests | 6 | Role-to-permission mapping configuration |
| UserQueryServiceAdditionalTests | 6 | Additional user query service scenarios |
| KeycloakServiceAccountServiceTests | 6 | Keycloak service account CRUD operations |
| CurrentUserServiceTests | 6 | Current user context extraction from HTTP |
| RedisApiKeyServiceAdditionalTests | 5 | Additional API key service edge cases |
| ApiKeyAuthenticationMiddlewareTests | 5 | API key authentication middleware |
| KeycloakTokenServiceGapTests | 5 | Token service coverage gaps |
| UserServiceTests | 4 | User service operations |
| PermissionAuthorizationPolicyProviderTests | 4 | Dynamic authorization policy provider |
| PermissionAuthorizationHandlerTests | 3 | Permission-based authorization handler |
| RolePermissionLookupTests | 3 | Role permission lookup operations |
| HasPermissionAttributeTests | 2 | HasPermission authorization attribute |
| PermissionRequirementTests | 1 | Permission requirement value object |

### Api (183 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ScimControllerTests | 23 | SCIM 2.0 REST endpoints |
| AuthControllerTests | 17 | Authentication REST endpoints |
| HasPermissionAttributeTests | 17 | Permission attribute tests (Api layer) |
| RequestContractTests | 17 | Request DTO contract validation |
| ResultExtensionsTests | 15 | Result-to-ActionResult mapping |
| ApiKeysControllerTests | 15 | API key management REST endpoints |
| EnumMappingsTests | 14 | Enum-to-string mapping tests |
| UsersControllerTests | 12 | User management REST endpoints |
| SsoControllerTests | 12 | SSO configuration REST endpoints |
| OrganizationsControllerTests | 11 | Organization REST endpoints |
| ResponseContractTests | 11 | Response DTO contract validation |
| ServiceAccountsControllerTests | 8 | Service account REST endpoints |
| ScopesControllerTests | 3 | Scope management REST endpoints |
| RolesControllerTests | 3 | Role management REST endpoints |

### Integration (10 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ServiceAccountRepositoryTests | 6 | EF Core repository for service accounts against real Postgres |
| SsoConfigurationRepositoryTests | 4 | EF Core repository for SSO configurations against real Postgres |

---

## 6. Identity Module - Integration Tests (55 tests)

### OAuth2 (15 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| TokenAcquisitionTests | 6 | OAuth2 token acquisition flows with Keycloak |
| TokenValidationTests | 5 | JWT token validation and claims extraction |
| ServiceAccountFlowTests | 4 | client_credentials OAuth2 flow for service accounts |

### ServiceAccounts (24 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ApiScopesTests | 7 | API scope management endpoints |
| RevokeServiceAccountTests | 5 | Service account revocation API endpoint |
| CreateServiceAccountTests | 4 | Service account creation API endpoint |
| ListServiceAccountsTests | 4 | Service account listing API endpoint |
| RotateSecretTests | 4 | Secret rotation API endpoint |

### Scim (8 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ScimProvisioningTests | 8 | SCIM 2.0 user provisioning end-to-end with mock IdP |

### Sso (8 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SsoConfigurationTests | 8 | SSO provider configuration management endpoints |

---

## 7. Shared.Kernel Tests (287 tests)

### Domain (55 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| DomainExceptionTests | 15 | Domain exception creation and error codes |
| EntityTests | 12 | Entity base class equality and comparison |
| ValueObjectTests | 11 | Value object equality and comparison |
| AggregateRootTests | 10 | Aggregate root domain event management |
| AuditableEntityTests | 7 | Auditable entity timestamp tracking |
| SystemClockTests | 2 | System clock abstraction |

### Identity (14 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| UserIdTests | 8 | UserId strongly-typed ID |
| TenantIdTests | 8 | TenantId strongly-typed ID |
| StronglyTypedIdExtensionsTests | 3 | Extension methods for strongly-typed IDs |
| StronglyTypedIdConverterTests | 3 | JSON/EF Core converters for strongly-typed IDs |

### MultiTenancy (25 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| TenantContextTests | 7 | Tenant context creation and access |
| TenantContextFactoryTests | 6 | Tenant context factory |
| TenantSaveChangesInterceptorTests | 6 | EF Core interceptor for tenant isolation |
| RegionConfigurationTests | 5 | Region configuration |
| RegionSettingsTests | 4 | Region settings |
| TenantQueryExtensionsTests | 3 | Query extensions for tenant filtering |

### Results (44 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ErrorTests | 15 | Error value object creation and comparison |
| PagedResultTests | 12 | Paged result creation and metadata |
| ResultTests | 9 | Result pattern: success, failure, error codes |
| ResultOfTTests | 8 | Generic Result<T> pattern |

### Extensions (34 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| StringExtensionsTests | 27 | String utility extensions (truncate, slug, etc.) |
| ServiceCollectionExtensionsTests | 7 | DI registration extensions |

### CustomFields (28 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| FieldValidationRulesTests | 24 | Field validation rules (required, min/max, regex) |
| CustomFieldTypeTests | 13 | Custom field type enumeration |
| CustomFieldRegistryTests | 8 | Custom field registry management |
| CustomFieldOptionTests | 4 | Custom field option value object |
| CustomFieldValidationResultTests | 3 | Validation result creation |
| CustomFieldValidationErrorTests | 3 | Validation error creation |

### Plugins (24 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| PluginPermissionTests | 10 | Plugin permission model |
| PluginLifecycleStateTests | 6 | Plugin lifecycle state transitions |
| PluginManifestTests | 5 | Plugin manifest parsing |
| PluginDependencyTests | 3 | Plugin dependency resolution |
| PluginContextTests | 1 | Plugin context creation |

### Diagnostics (12 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SloDefinitionsTests | 8 | SLO definition and threshold checks |
| DiagnosticsTests | 4 | Diagnostic info collection |

### Messaging (5 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| WolverineErrorHandlingExtensionsTests | 5 | Wolverine error handling configuration |

---

## 8. Shared.Infrastructure Tests (329 tests)

### Auditing (29 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| AuditingExtensionsTests | 13 | Audit DI registration and configuration |
| AuditInterceptorTests | 7 | EF Core SaveChanges interceptor |
| AuditEntryTests | 6 | Audit entry creation and serialization |
| AuditTenantIsolationTests | 3 | Audit tenant isolation |

### BackgroundJobs (13 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| HangfireJobSchedulerTests | 8 | Hangfire job scheduling abstraction |
| BackgroundJobsExtensionsTests | 5 | Background job DI registration |

### Persistence (30 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| DictionaryValueComparerTests | 15 | EF Core dictionary value comparer |
| DataResidencyFilterTests | 9 | Data residency query filter |
| RegionAwareConnectionFactoryTests | 6 | Region-aware database connection factory |

### Plugins (85 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| PluginLifecycleManagerTests | 36 | Plugin lifecycle management |
| PluginServiceExtensionsTests | 13 | Plugin DI registration extensions |
| PluginRegistryTests | 12 | Plugin registry management |
| PluginPermissionValidatorTests | 10 | Plugin permission validation |
| PluginLoaderTests | 10 | Plugin assembly loading |
| PluginManifestLoaderTests | 8 | Plugin manifest file loading |
| PluginLifecycleManagerLoggingTests | 7 | Plugin lifecycle logging |
| PluginLoadExceptionTests | 6 | Plugin load error handling |
| PluginConfigurationTests | 6 | Plugin configuration management |
| PluginAssemblyLoadContextTests | 5 | Plugin assembly load context isolation |

### AsyncApi (50 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| EventFlowDiscoveryTests | 13 | Event flow discovery across modules |
| AsyncApiDocumentGeneratorTests | 11 | AsyncAPI document generation |
| MermaidFlowGeneratorTests | 9 | Mermaid diagram generation for event flows |
| JsonSchemaGeneratorTests | 9 | JSON schema generation for events |
| AsyncApiIntegrationTests | 8 | AsyncAPI end-to-end tests |

### Workflows (26 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| WorkflowActivityBaseTests | 22 | Elsa workflow activity base class |
| ElsaExtensionsTests | 4 | Elsa DI registration extensions |

### Middleware (5 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| WolverineModuleTaggingMiddlewareTests | 5 | Module tagging middleware for Wolverine messages |

### Services (47 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| HtmlSanitizationServiceTests | 47 | HTML sanitization for user content |

### ServiceClients (9 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| HttpNotificationServiceClientTests | 9 | HTTP client for notification service |

### Integration (3 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| AuditingExtensionsIntegrationTests | 3 | Auditing integration tests |

---

## 9. Api Tests (215 tests)

### Extensions (66 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ServiceCollectionExtensionsTests | 35 | API service registration and configuration |
| ResultExtensionsTests | 17 | Result-to-ActionResult mapping |
| AsyncApiEndpointExtensionsTests | 8 | AsyncAPI endpoint registration |
| HangfireExtensionsTests | 6 | Hangfire dashboard and job registration |

### Middleware (49 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ApiVersionRewriteMiddlewareTests | 15 | API version URL rewriting |
| GlobalExceptionHandlerTests | 13 | Global exception handling and ProblemDetails |
| RateLimitDefaultsTests | 10 | Rate limiting configuration defaults |
| SecurityHeadersMiddlewareTests | 9 | Security headers (CSP, HSTS, etc.) |
| ModuleTaggingMiddlewareTests | 5 | Module tagging for telemetry |
| HangfireDashboardAuthFilterTests | 4 | Hangfire dashboard authorization |

### Services (35 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| RedisPresenceServiceTests | 16 | Redis-based user presence tracking |
| SignalRRealtimeDispatcherTests | 12 | SignalR real-time event dispatching |
| InProcessNotificationServiceClientTests | 7 | In-process notification service client |

### Hubs (12 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| RealtimeHubTests | 12 | SignalR hub for real-time communication |

### Health (5 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| RegionHealthCheckTests | 5 | Region-aware health checks |

### Integration (9 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| RealtimeHubIntegrationTests | 5 | SignalR hub integration tests |
| HealthCheckTests | 4 | Health endpoint integration tests |

### Jobs (2 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| SystemHeartbeatJobTests | 2 | System heartbeat background job |

### Logging (2 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| ModuleEnricherTests | 2 | Serilog module name enricher |

---

## 10. Architecture Tests (112 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CleanArchitectureTests | 25 | Validates Clean Architecture layer dependencies |
| ModuleIsolationTests | 20 | Validates module boundary isolation |
| CqrsConventionTests | 15 | Validates CQRS handler conventions |
| ModuleRegistrationTests | 12 | Validates module registration patterns |
| WolverineConventionTests | 11 | Validates Wolverine handler conventions |
| ApiVersioningTests | 10 | Validates API versioning conventions |
| MultiTenancyArchitectureTests | 9 | Validates multi-tenancy patterns |
| SharedContractsTests | 5 | Validates shared contracts usage |
| ApiConventionTests | 5 | Validates API controller conventions |

---

## 11. Messaging Integration Tests (12 tests)

| Test Class | Tests | Description |
|------------|-------|-------------|
| CrossModuleEventPropagationTests | 5 | Cross-module event propagation via RabbitMQ |
| MessagePublishConsumeTests | 3 | Message publish/consume round-trip |
| MessageRetryTests | 2 | Message retry with exponential backoff |
| MessageDeadLetterTests | 2 | Dead letter queue handling |
