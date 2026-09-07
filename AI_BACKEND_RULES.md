# AI BACKEND DEVELOPMENT RULES
# Laundry SaaS Platform

Version: 1.0
Status: ACTIVE
Scope: BACKEND ONLY
Architecture Baseline: LOCKED unless an approved architectural decision changes it

---

# 1. PURPOSE

This document is the single source of truth for Backend architecture,
organization, coding standards, development workflow, security boundaries,
AI coding behavior, and implementation decisions for the Laundry SaaS Platform.

Every developer and every AI coding agent MUST read this document before
creating, modifying, refactoring, moving, or deleting Backend code.

These rules are mandatory unless a newer approved architectural decision
explicitly replaces them.

Primary goals:

- Consistency between developers
- Consistency between AI agents
- Maintainability
- Security
- Strong tenant isolation
- Clear ownership boundaries
- Predictable code organization
- Testability
- Controlled changes
- Avoiding duplicate implementations
- Avoiding over-engineering
- Preserving approved Domain rules
- Safe collaboration through Git

The simplest correct solution that respects the approved architecture is preferred.

---

# 2. PROJECT IDENTITY

Project:

Laundry SaaS Platform

The system is a multi-tenant SaaS platform for laundries.

Current backend serves:

- Customer Flutter App
- Laundry Flutter App
- Driver Flutter App
- Platform Admin Angular Application

The Angular application is for Host / Platform Administration only.

The current document defines BACKEND rules only.

Frontend implementation rules are outside this file unless they directly affect
API contracts or backend security.

---

# 3. TECHNOLOGY STACK

Backend stack is locked to:

- ASP.NET Core
- .NET 10
- ABP Framework 10.6.0
- ABP Modular Monolith
- Entity Framework Core
- Microsoft SQL Server
- ABP Identity
- OpenIddict
- ABP Roles and Permissions
- ABP Multi-Tenancy
- ABP Unit of Work
- ABP Auditing
- REST APIs

ABP functionality MUST be preferred whenever it reasonably satisfies the requirement.

Do NOT recreate functionality already provided by ABP or .NET.

---

# 4. PRIMARY ARCHITECTURE

The backend uses:

MODULAR MONOLITH.

It is:

- One solution
- One application deployment initially
- One primary SQL Server database initially
- Multiple clearly separated business areas
- Shared infrastructure through ABP
- Explicit domain ownership

This is NOT Microservices.

Do NOT introduce:

- Microservices
- Separate databases per module
- Distributed transactions
- Event brokers
- Service mesh
- API gateways for internal module communication

unless a future approved decision explicitly requires them.

---

# 5. PRACTICAL CLEAN ARCHITECTURE

The project uses practical Clean Architecture.

Main projects/layers:

- Domain.Shared
- Domain
- Application.Contracts
- Application
- EntityFrameworkCore
- HttpApi
- HttpApi.Host
- DbMigrator
- Test projects

Do NOT create additional generic layers such as:

- Business Layer
- Manager Layer for everything
- Extra Repository Layer
- Extra UnitOfWork Layer
- Extra Service Layer
- Extra Infrastructure abstraction

without a real requirement.

ABP already provides many infrastructure concerns.

---

# 6. DEPENDENCY DIRECTION

Dependencies must preserve domain independence.

Preferred direction:

HttpApi
  ↓
Application
  ↓
Domain

Application.Contracts
  ↓
Domain.Shared / public contracts

EntityFrameworkCore
  ↓
Domain

Domain MUST NOT depend on:

- EF Core
- SQL Server
- HTTP
- Controllers
- Flutter
- Angular
- SMS SDKs
- Push SDKs
- Blob provider SDKs
- External service SDKs

Avoid circular dependencies.

---

# 7. CURRENT BUSINESS AREAS

Current business areas are:

1. Identity / Tenancy
2. Host Management
3. Laundries
4. Laundry Staff
5. Catalog & Pricing
6. Customers
7. Orders
8. Drivers
9. Pickup & Delivery
10. Bags / Chain of Custody
11. Inspections
12. Processing
13. Notifications
14. Complaints

These are logical business areas inside the modular monolith.

Do not invent new business modules without a real requirement.

---

# 8. CURRENTLY EXCLUDED FEATURES

The following are outside the current MVP and MUST NOT be implemented unless
an approved decision explicitly adds them:

- Payment Module
- Online payment gateway
- Wallet
- PaymentStatus
- SaaS subscription billing
- Marketplace commissions
- Loyalty program
- Coupons
- Promotions engine
- Dynamic pricing
- Full accounting
- Inventory management
- AI features
- Full live driver tracking
- Advanced multi-branch architecture
- Advanced analytics
- Separate courier marketplace
- Event sourcing
- CQRS infrastructure
- Microservices

Do not create placeholder implementations for excluded features.

---

# 9. TENANCY MODEL

Each Tenant represents one independent laundry/company in the MVP.

There is no advanced multi-branch model in the MVP.

Tenant-owned business data must be isolated by ABP Multi-Tenancy.

Tenant boundaries are security boundaries.

Do NOT trust TenantId sent by clients.

TenantId must be derived from trusted backend context whenever possible.

---

# 10. GLOBAL CUSTOMER MODEL

Customer is GLOBAL / HOST-LEVEL.

A customer registers once for the entire platform.

Customer is NOT owned by a single laundry tenant.

Customer can order from multiple laundries.

Customer.UserId points to the global / Host IdentityUser.

CustomerAddress is also global under Customer.

Customer cross-tenant access is a special case and MUST be handled carefully.

---

# 11. CUSTOMER CROSS-TENANT QUERY RULE

When customer APIs need to query tenant-owned Orders across laundries:

ABP multi-tenant filter may be disabled only in the narrowest possible scope.

Example concept:

IDataFilter.Disable<IMultiTenant>()

BUT the query MUST immediately and explicitly filter by:

CustomerId == CurrentCustomer.Id

Never disable tenant filtering broadly and then return unrestricted tenant data.

Cross-tenant customer queries MUST be covered by tests.

---

# 12. TENANT USER MODEL

Laundry staff and drivers belong to a tenant.

LaundryStaffProfile points to a tenant IdentityUser.

Driver points to a tenant IdentityUser.

Do NOT create a second authentication system.

Use ABP Identity.

---

# 13. SECURITY PIPELINE FOR APPLICATION SERVICES

Every protected Application Service operation must consider security in this order:

1. Authentication
2. Actor Profile
3. Permission
4. Tenant Scope
5. Resource Ownership

Authentication alone is never sufficient.

Permission alone is never sufficient.

Tenant isolation alone is never sufficient for user-owned resources.

---

# 14. ACTOR TYPES

The backend recognizes these main actors:

HOST:
- Platform administrator

CUSTOMER:
- Global platform customer

LAUNDRY:
- Tenant staff / laundry administration

DRIVER:
- Tenant driver

CROSS-CUTTING USER:
- Current authenticated user for notifications and similar self-scoped operations

Do not mix actor-specific responsibilities.

---

# 15. HOST RULES

Host operations run in Host context.

Host may manage:

- Tenant onboarding
- Tenant activation
- Tenant suspension
- Laundry platform records
- Platform-level statistics

Host lifecycle operations must coordinate:

ABP Tenant
+
Laundry Aggregate

Do not activate or suspend only Laundry while leaving the ABP Tenant lifecycle inconsistent.

---

# 16. CUSTOMER OWNERSHIP

Customer APIs may only access resources belonging to the authenticated customer.

Never trust a client-supplied CustomerId when the server can derive it.

Examples:

- Customer orders
- Customer addresses
- Customer complaints

must be scoped to CurrentCustomer.Id.

---

# 17. DRIVER OWNERSHIP

A driver may only access or mutate tasks assigned to that driver.

The server must verify:

DriverId == CurrentDriver.Id

before returning or mutating a PickupTask or DeliveryTask.

A driver must never assign drivers.

Driver assignment is a laundry-side administrative operation.

---

# 18. LAUNDRY OWNERSHIP

Tenant staff can only access tenant-owned data belonging to CurrentTenant.

Use ABP tenant isolation plus permissions.

Do not accept TenantId from Flutter to decide ownership.

---

# 19. DOMAIN LAYER

Domain contains:

- Aggregate Roots
- Entities
- Value Objects
- Domain Services
- Business Rules
- State transitions
- Domain Events where justified
- Repository interfaces only when genuinely required

Domain represents business truth.

Important state changes must happen through domain methods.

Never mutate protected status fields directly from Application code.

---

# 20. DOMAIN IS CURRENTLY LOCKED

The current Domain model has been hardened and tested.

AI agents MUST NOT casually redesign Domain entities, value objects,
state machines, or invariants while implementing Application Services.

If Application implementation reveals a genuine Domain conflict:

- Stop
- Report the conflict
- Explain why current Domain is insufficient
- Propose the smallest change
- Wait for approval before changing Domain

Do not silently modify Domain to make Application code easier.

---

# 21. CURRENT AGGREGATE ROOTS

Current Aggregate Roots include:

1. Laundry
2. LaundryItemType
3. LaundryService
4. ServicePrice
5. LaundryStaffProfile
6. Customer
7. Order
8. Driver
9. PickupTask
10. DeliveryTask
11. Bag
12. Inspection
13. AppNotification
14. Complaint

Do not create duplicate roots for the same business responsibility.

---

# 22. CURRENT CHILD ENTITIES

Current child entities include:

- LaundryWorkingHour -> Laundry
- LaundryTimeSlot -> Laundry
- CustomerAddress -> Customer
- OrderItem -> Order
- OrderAdjustment -> Order
- OrderStatusHistory -> Order
- ProcessingStageHistory -> Order
- BagEvent -> Bag
- InspectionItem -> Inspection
- Damage -> Inspection / InspectionItem
- ComplaintAttachment -> Complaint

Child entities should normally be modified through their owning aggregate.

---

# 23. CURRENT VALUE OBJECTS

Current important Value Objects include:

- AddressSnapshot
- CoverageArea
- CashCollectionInfo
- ComplaintResolutionInfo
- DeliveryVerificationInfo
- PickupScheduleSnapshot

Value Objects do not get repositories.

Do not add Id fields to Value Objects.

---

# 24. ORDER IS A HIGH-COMPLEXITY DOMAIN

Order is not simple CRUD.

Order lifecycle must be controlled by Domain methods.

Current OrderStatus lifecycle includes:

- Draft
- PendingPickup
- PickupAssigned
- OutForPickup
- PickedUp
- ReceivedAtLaundry
- Inspection
- WaitingForAdjustmentApproval
- Processing
- ReadyForDelivery
- OutForDelivery
- Delivered
- Completed
- Cancelled
- OnHold
- PickupFailed
- DeliveryFailed

Do not allow arbitrary status assignment.

---

# 25. ORDER CREATION SEMANTICS

In the MVP, CreateOrder means:

Create
+
Confirm
+
PendingPickup

in one business operation.

There is no public server-side editable Draft workflow for the customer.

Do not create ConfirmOrderAsync.

Do not create generic Draft CRUD.

---

# 26. ORDER PRICING AUTHORITY

The Backend is the authority for order pricing.

Customer CreateOrderInput MUST NOT contain:

- UnitPrice
- LineTotal
- Subtotal
- Total
- DeliveryFee
- Discount
- TenantId
- CustomerId
- Status
- ProcessingStage

Backend calculates trusted values from current catalog and domain rules.

---

# 27. ORDER ITEM SNAPSHOTS

OrderItem must preserve historical snapshots:

- ItemNameSnapshot
- ServiceNameSnapshot
- UnitPriceSnapshot

Do not rewrite historical OrderItem snapshots because catalog data changes later.

Snapshots are historical truth for the original order.

---

# 28. CATALOG AND PRICING MODEL

Catalog uses:

LaundryItemType
+
LaundryService
+
ServicePrice

ServicePrice maps:

TenantId
+
LaundryItemTypeId
+
LaundryServiceId
->
Price

Pricing is predefined by the laundry before the customer creates the order.

Do not invent post-pickup pricing from scratch.

---

# 29. SERVICE PRICE SECURITY

Laundry-authorized users may set catalog prices.

Customer may never set authoritative prices.

Driver may never set service prices.

ServicePrice used in calculations must belong to the same tenant as the Order.

---

# 30. ORDER ADJUSTMENT SOURCE OF TRUTH

Inspection is the Source of Truth for order mismatches.

CreateOrderAdjustmentInput MUST NOT resend mismatch lines.

It must not contain:

- AdjustedItems
- NewTotal
- UnitPrice
- Price

Application Service must:

- Load Order
- Load official Inspection
- Load trusted ServicePrice records
- Call OrderAdjustmentDomainService

Do not trust UI-computed adjustment totals.

---

# 31. ADJUSTMENT PRICING POLICY

For unchanged original order items:

Use UnitPriceSnapshot even if catalog price changed.

For:

- Additional item
- Changed item type
- Changed service

use trusted current ServicePrice.

OrderItem snapshots remain unchanged.

---

# 32. ADJUSTMENT RESULT RULES

Current adjustment behavior:

NewTotal == CurrentTotal
-> no adjustment

NewTotal > CurrentTotal
-> Pending
-> Order waits for customer approval

NewTotal < CurrentTotal
-> auto approved
-> Total updated in customer favor

Do not duplicate this logic in Application layer.

---

# 33. PICKUP SCHEDULING

Customer order creation requires:

PickupDate
+
PickupSlotId

Backend validates:

- slot belongs to selected Laundry
- SlotType == Pickup
- slot active
- day matches PickupDate
- date not in past
- laundry active
- accepting orders
- working hour open
- slot inside working hours
- same-day slot has not already ended

Do not trust client-supplied slot times.

---

# 34. PICKUP SCHEDULE SNAPSHOT

Order stores immutable PickupScheduleSnapshot:

- ScheduledDate
- StartTime
- EndTime
- OriginalSlotId

The snapshot is historical truth.

Changing LaundryTimeSlot later must not change an existing Order schedule.

---

# 35. PROCESSING STAGES

OrderStatus and ProcessingStage are separate concepts.

ProcessingStage values:

- Sorting
- Washing
- Drying
- Ironing
- Folding
- Packaging

When Order.Status != Processing:

CurrentProcessingStage must be null.

When Processing:

stage progression must follow the strict sequence.

Flutter must not choose an arbitrary next ProcessingStage.

Application calls a command such as:

MoveToNextProcessingStage

Backend determines the next allowed stage.

---

# 36. PICKUP TASK LIFECYCLE

PickupTask lifecycle:

Assigned
-> Accepted
-> OutForPickup
-> Arrived
-> PickedUp
-> Completed

Terminal attempt states include:

- Failed
- Cancelled

Do not skip required transitions.

Application commands should be explicit.

---

# 37. DELIVERY TASK LIFECYCLE

DeliveryTask lifecycle:

Assigned
-> Accepted
-> PickedUpFromLaundry
-> OutForDelivery
-> Arrived
-> Delivered

Terminal attempt states include:

- Failed
- Cancelled

Do not expose generic status mutation.

---

# 38. MULTIPLE LOGISTICS ATTEMPTS

Pickup and Delivery may have multiple attempt records per Order.

AttemptNumber must preserve attempt history.

Only one active task of the same type per Order is allowed according to the approved constraints.

Do not overwrite failed task history.

---

# 39. CASH ON DELIVERY ONLY

The platform currently uses CASH ON DELIVERY only.

There is:

- No Payment Module
- No PaymentStatus
- No Wallet
- No online gateway

Cash collection is stored inside DeliveryTask through CashCollectionInfo.

Do not create PaymentMethod merely to represent a constant "CashOnDelivery".

---

# 40. CASH COLLECTION RULES

CashCollectionInfo contains trusted delivery collection state.

Driver may submit CollectedAmount as a factual claim of what was collected.

Backend must verify:

CollectedAmount == AmountToCollect

before final delivery confirmation.

Do not treat driver-submitted CollectedAmount as authoritative pricing.

---

# 41. DELIVERY OTP

Final delivery requires:

OTP verification
+
full COD collection

DeliveryVerificationInfo stores OTP verification state.

The Domain stores only an opaque secure OTP hash.

Domain does NOT:

- generate plaintext OTP
- know SMS providers
- know Push providers
- perform external delivery

---

# 42. OTP RULES

Current OTP rules:

- Exactly 6 digits
- Generated by Backend
- Cryptographically generated
- Plain OTP never stored
- Secure hash stored
- Expires after 5 minutes
- Maximum 3 failed attempts
- Resend cooldown 60 seconds
- Maximum 3 generations per DeliveryTask attempt
- New OTP invalidates old OTP
- OTP bound to current DeliveryTask
- OTP can only be generated in Arrived state
- ConfirmDelivery requires verified OTP + exact COD

Do not weaken these rules in Application code.

---

# 43. OTP HASHING LOCATION

OTP plaintext generation, hashing, comparison, and sending belong outside Domain.

Application / infrastructure may coordinate these concerns.

Domain receives trusted hash/state transitions only.

Do not place SMS SDK calls or cryptographic provider code directly inside Domain entities.

---

# 44. BAG OWNERSHIP

Bag tracks physical chain of custody.

BagNumber and QR code are generated by Backend.

Flutter must not create arbitrary BagNumber or QR identifiers.

AssignBagToOrderAsync receives order identity, not client-created bag credentials.

---

# 45. BAG QR SCANNING

ScanBagQrInput may contain the scanned QR code and approved contextual information.

It MUST NOT contain:

BagEventType

Client does not decide custody event type.

Backend determines allowed operation from:

- current actor
- current task
- current order status
- bag state
- tenant
- permissions

QR identifies a bag; QR is NOT authorization.

---

# 46. BAG EVENTS

BagEvent is append-only chain-of-custody history.

Do not rewrite or delete historical custody events in normal business workflows.

BagEvent is not the source of truth for processing stage.

Order owns processing stage.

---

# 47. INSPECTION ROLE

Inspection compares:

Expected Order
vs
Actual Received Items

Inspection is not a price-entry form.

It records operational reality.

---

# 48. INSPECTION ITEM MODEL

InspectionItem supports:

- matched original item
- missing original item
- additional item
- changed item type
- changed service
- changed quantity

Client may submit actual observed values.

Client MUST NOT submit trusted Expected values.

Expected data must come from OrderItem in Backend.

---

# 49. INSPECTION COMPLETION

Before Inspection is completed:

Every original OrderItem must be represented in the inspection result,
even when ActualQuantity = 0.

Do not allow silent omission of an original order item.

---

# 50. DAMAGES

Damage belongs to Inspection and links to an InspectionItem.

Damage records existing/pre-existing damage observed during inspection.

Do not store image binary in SQL.

Store file reference/metadata only.

---

# 51. COMPLAINTS

Complaint lifecycle currently follows:

Open
-> InReview
-> Resolved
-> Closed

Do not skip state transitions unless Domain explicitly allows it.

Use explicit commands:

Review
Resolve
Close

---

# 52. COMPLAINT ATTACHMENTS

ComplaintAttachment stores metadata/reference only.

Application contracts may contain:

- BlobName
- FileName
- ContentType
- creation metadata

Do NOT place:

- byte[]
- Base64
- IFormFile

inside core Application.Contracts.

Actual binary storage must use an approved Blob Storage mechanism later.

---

# 53. BLOB REFERENCE SECURITY

A client-supplied BlobName is NOT trusted automatically.

Application implementation must verify:

- Blob exists
- current user owns or may use it
- Blob belongs to correct context
- file type and size were validated
- the same Blob is not illegally attached across users/tenants

---

# 54. FILE STORAGE

Use ABP Blob Storing or another approved object/blob storage provider.

Database stores metadata and references only.

Do not store large files directly in SQL Server without explicit approval.

---

# 55. APP NOTIFICATIONS

AppNotification is lightweight and user-targeted.

Notification operations must be scoped by:

TargetUserId == CurrentUser.Id

Do not rely only on TenantId for notification ownership.

Notifications are not the source of truth for order state.

Database domain state remains the source of truth.

---

# 56. APPLICATION CONTRACTS ARE LOCKED

Application.Contracts have been hardened and verified.

Current approved baseline:

- 15 Application Service interfaces
- 87 DTOs
- 40 Input/Command DTOs
- 35 Output/Detail DTOs
- 12 List/Lookup DTOs

Do not add, rename, remove, or duplicate contracts casually.

If implementation reveals a true contract gap:

- report it
- explain the gap
- propose smallest change
- wait for approval before changing locked contracts

---

# 57. APPROVED APPLICATION SERVICE INTERFACES

The approved 15 interfaces are:

1. ILaundryHostAppService
2. ILaundryProfileAppService
3. ILaundryTimeSlotAppService
4. ILaundryStaffAppService
5. ILaundryCatalogAppService
6. ICustomerCatalogAppService
7. ICustomerProfileAppService
8. ICustomerOrderAppService
9. ILaundryOrderAppService
10. IDriverManagementAppService
11. IDriverTaskAppService
12. IBagAppService
13. IInspectionAppService
14. IAppNotificationAppService
15. IComplaintAppService

Do NOT invent:

- IOrderAdjustmentAppService
- ILaundryNotificationAppService

unless a future approved architecture change explicitly requires them.

---

# 58. DTO RULES

Every API boundary uses DTOs.

Do not expose Domain Entities.

Input DTOs contain only values the actor is allowed to submit.

Output DTOs contain only data the actor is allowed to see.

Do not create giant universal DTOs.

Use actor-specific detail DTOs when security and usage differ.

---

# 59. CUSTOMER ORDER DTO PRIVACY

CustomerOrderDetailDto must not expose:

- TenantId
- CustomerId
- staff Identity IDs
- internal audit details
- internal operational notes
- internal security metadata

Customer-facing DTOs must be purpose-built.

---

# 60. LAUNDRY ORDER DTO

LaundryOrderDetailDto may expose wider operational details required by tenant staff.

It still must not expose:

- passwords
- tokens
- OTP hashes
- secret keys
- unrelated Identity internals

---

# 61. NO CLIENT FINANCIAL AUTHORITY

Customer clients may not submit authoritative financial values for Orders.

Laundry users may submit catalog Price only where the use case is explicitly
price administration.

Driver may submit CollectedAmount only for COD factual collection.

No other financial client input should be accepted without approved design.

---

# 62. NO CLIENT STATUS AUTHORITY

No client may submit arbitrary:

OrderStatus
ProcessingStage
PickupTaskStatus
DeliveryTaskStatus
InspectionStatus
ComplaintStatus
BagEventType

Use explicit business commands and Domain methods.

---

# 63. APPLICATION LAYER

Application contains:

- Application Service implementations
- Authorization
- Actor resolution
- Ownership checks
- Tenant orchestration
- Repository calls
- Domain method calls
- Object mapping
- External abstraction coordination
- Unit-of-Work orchestration

Application Services coordinate use cases.

They must not become God Services.

---

# 64. APPLICATION SERVICE FLOW

Typical protected operation:

Request
↓
Authentication
↓
Resolve Actor Profile
↓
Permission Check
↓
Tenant Scope
↓
Resource Ownership
↓
Input Validation
↓
Load Domain Objects
↓
Domain Operation
↓
Repository Changes
↓
ABP Unit of Work
↓
Map to DTO

Keep this flow explicit and reviewable.

---

# 65. DOMAIN METHODS ONLY FOR STATE CHANGES

Application Service must call Domain methods.

Do NOT set:

entity.Status = ...
entity.CurrentStage = ...
entity.IsVerified = ...

when protected Domain methods already exist.

Never bypass state machine guards for convenience.

---

# 66. APPLICATION VALIDATION

Application/DTO validation handles shape and format:

- Required
- MaxLength
- Range
- format
- exactly 6-digit OTP
- valid Guid shape

Domain handles business rules.

Do not duplicate complex business invariants in DTO validators.

---

# 67. EXCEPTION HANDLING

Use ABP centralized exception handling.

Do not wrap every Application Service in generic try/catch.

Use:

- BusinessException
- EntityNotFoundException
- AuthorizationException
- validation mechanisms

Do not expose:

- stack traces
- SQL errors
- internal infrastructure details
- sensitive data

---

# 68. ERROR CODES

Business exceptions should use stable error codes.

Reuse existing error codes before creating new ones.

Organize codes by business area.

Do not rely on arbitrary error strings as the contract.

---

# 69. PERMISSIONS

Use ABP permission system.

Current permission families include:

Host
Laundry
Staff
Catalog
Orders
Drivers / Logistics
Processing / Bags
Inspection
Complaints

Do not create a permission for every UI button.

Permissions protect capabilities, not presentation details.

---

# 70. CUSTOMER SELF-SERVICE AUTHORIZATION

Customer self-service typically uses:

Authentication
+
valid Customer profile
+
resource ownership

Do not create unnecessary granular permissions for ordinary customer self-service
unless a real requirement exists.

---

# 71. DRIVER AUTHORIZATION

Driver task operations require:

Authentication
+
Driver profile
+
Logistics.ExecuteTasks
+
task ownership

All four layers matter.

---

# 72. LAUNDRY AUTHORIZATION

Laundry administrative operations require:

Authentication
+
tenant staff profile
+
appropriate permission
+
tenant scope

Do not trust tenant data from client inputs.

---

# 73. HOST AUTHORIZATION

Host operations require:

Host context
+
appropriate Host permission

Tenant staff must never invoke Host lifecycle operations.

---

# 74. REPOSITORIES

Use ABP Generic Repository for normal persistence.

Create custom repositories only when generic repository is insufficient.

Do not create a repository for every entity.

Prefer repository per Aggregate Root when custom persistence abstraction is justified.

---

# 75. CROSS-AGGREGATE LOADING

Application Services may coordinate multiple repositories to support one use case.

Example:

Adjustment calculation may load:

- Order
- Inspection
- ServicePrice records

then call trusted Domain logic.

Do not push orchestration into DTOs or mapping profiles.

---

# 76. UNIT OF WORK

Use ABP Unit of Work.

Do NOT implement custom UOW.

Do not manually open SQL transactions for ordinary use cases.

Use explicit UOW/transaction configuration only for real consistency requirements.

---

# 77. CRITICAL ATOMIC WORKFLOWS

Sensitive operations should execute atomically when multiple state changes must
succeed or fail together.

Examples:

- CreateOrder
- Assign driver + update Order state
- Complete pickup + update Order state
- Complete delivery + OTP/COD verification + Delivered + Completed
- Order adjustment application
- Inspection completion when it triggers dependent workflow

Use ABP Unit of Work.

---

# 78. DELIVERY COMPLETION ATOMICITY

Final delivery operation must enforce in one Unit of Work:

- DeliveryTask is in valid state
- OTP is verified
- COD fully collected
- task.ConfirmDelivery(...)
- order.MarkAsDelivered()
- order.Complete()

Do not split critical completion into inconsistent independent requests unless
approved business requirements require it.

---

# 79. IDEMPOTENCY

Sensitive commands must consider duplicate requests.

Especially:

- OTP request
- OTP verification
- cash confirmation
- delivery confirmation
- task assignment
- adjustment creation
- bag creation/scanning

Do not blindly perform duplicate state changes.

Use Domain state guards, concurrency, unique constraints, or idempotency keys where justified.

---

# 80. CONCURRENCY

Important mutable aggregates must respect optimistic concurrency where applicable.

Do not silently overwrite concurrent changes.

Use ABP concurrency conventions and ConcurrencyStamp where contracts already require it.

Do not add ConcurrencyStamp to every DTO arbitrarily.

---

# 81. MAPPING

Use ABP ObjectMapper / configured mapping profiles for ordinary transformation.

Mapping is not business logic.

Do not calculate:

- prices
- permissions
- statuses
- ownership
- business decisions

inside mapping configuration.

Complex DTO assembly may be done explicitly in Application when needed.

---

# 82. EF CORE

Use EF Core through the EntityFrameworkCore project.

Domain entities must remain persistence-independent.

Do not put EF configuration attributes/logic into Domain merely for convenience.

---

# 83. EF CONFIGURATION ORGANIZATION

Prefer organizing EF configuration by business area.

Avoid one uncontrolled giant configuration method.

Current existing configuration should not be broadly refactored during unrelated work.

Any future restructuring requires a focused change.

---

# 84. DATABASE MIGRATIONS

Every schema change requires a migration.

Do NOT edit old migrations that have already been applied/shared.

Create a new migration for subsequent schema changes.

Migration must be inspected before DbMigrator execution.

Never drop business tables casually.

---

# 85. MIGRATION SAFETY

Before a risky migration:

- inspect existing row counts
- assess nullable -> required transitions
- avoid inventing backfill values
- stop when existing data requires a business decision

Never reset/drop the database just to make a migration easier unless explicitly approved.

---

# 86. DATABASE SOURCE OF TRUTH

The database/domain state is authoritative.

Do not infer business state from:

- notification delivery
- Flutter local state
- cached UI state
- QR scan alone

Notifications are informational.

---

# 87. INDEXES

Indexes must have real query or uniqueness reasons.

Important current patterns include:

- tenant-scoped catalog uniqueness
- one active pickup/delivery attempt
- one official inspection per order
- default customer address uniqueness
- service price uniqueness

Do not create indexes on every column.

---

# 88. UNIQUE CONSTRAINTS

Critical business uniqueness should be enforced in database where appropriate.

Tenant-scoped uniqueness must include TenantId.

Do not rely only on application-level checks for critical uniqueness.

---

# 89. SOFT DELETE POLICY

Soft delete is selective.

Current admin/catalog-style entities may use FullAudited + SoftDelete where approved.

Protected operational history generally should not be erased.

Do not convert every entity to soft delete automatically.

Do not hard delete operational history casually.

---

# 90. AUDITING

Use ABP auditing base classes/interfaces.

Do not duplicate:

CreationTime
CreatorId
LastModificationTime
LastModifierId
DeletionTime
DeleterId

when ABP already provides them.

---

# 91. APPEND-ONLY HISTORY

History entities such as:

- OrderStatusHistory
- ProcessingStageHistory
- BagEvent

are append-only business history.

Do not provide ordinary Update/Delete APIs for them.

---

# 92. PAGINATION

Any endpoint returning potentially large collections must use pagination.

Examples:

- Orders
- Drivers
- Staff
- Complaints
- Notifications
- Host laundry list

Do not return unbounded large lists.

---

# 93. FILTERING

Filtering must use explicit filter DTOs.

Do not expose unrestricted dynamic database querying.

Only allow approved fields.

---

# 94. SORTING

Sorting must be restricted to approved fields.

Do not concatenate arbitrary client-provided sorting expressions into SQL.

Use ABP conventions safely or validate allowed sort fields.

---

# 95. EF QUERY QUALITY

Avoid:

- N+1
- unnecessary Include
- huge aggregate graphs
- loading unused columns
- unbounded collections

Use:

- projection
- AsNoTracking for read-only queries where appropriate
- efficient tenant filters
- pagination

Do not optimize blindly; inspect real query needs.

---

# 96. ASYNC

Use async APIs for I/O.

Never use:

.Result
.Wait()

in normal Application code.

Avoid sync-over-async.

---

# 97. FILES AND BLOBS

Store actual files outside SQL through approved blob/object storage.

Database stores references/metadata.

Validate:

- file size
- file type
- file name
- access rights
- ownership
- tenant/user context

Do not trust client-provided metadata.

---

# 98. EXTERNAL SERVICES

Third-party providers must be behind an appropriate abstraction.

Examples:

SMS
Push notifications
Blob storage

Domain must not depend on provider SDKs.

Application coordinates abstractions.

Infrastructure/provider implementation belongs outside Domain.

---

# 99. SMS / OTP DELIVERY PROVIDER

OTP sending infrastructure is not part of Domain.

Do not hardcode a provider into DeliveryTask.

Future provider integration should expose a narrow abstraction, for example:

IDeliveryOtpSender

or another approved project-specific abstraction.

Do not add a package before approval.

---

# 100. CONFIGURATION

Use ASP.NET Core / ABP configuration mechanisms.

Never hard-code:

- passwords
- API keys
- tokens
- secrets
- production connection strings
- production URLs

Use environment-specific configuration.

Prefer strongly typed Options for structured infrastructure settings.

---

# 101. SECRETS

Secrets MUST NOT be committed to Git.

Never commit:

- real production connection strings
- SMS credentials
- API keys
- private keys
- OAuth secrets
- access tokens

Use secure secret management.

---

# 102. LOCALIZATION

User-facing backend messages should use the project's localization mechanism where appropriate.

Do not scatter hard-coded user-facing error sentences across services.

Stable error codes are preferred.

---

# 103. LOGGING

Use ABP / ASP.NET Core logging.

Never log:

- passwords
- OTP plaintext
- tokens
- full secrets
- sensitive payment/identity data

Use appropriate log levels.

Do not log every expected BusinessException as Error.

---

# 104. BACKGROUND JOBS

Use ABP Background Jobs only when appropriate.

Good candidates:

- notifications
- non-critical external delivery
- cleanup/retention
- scheduled operations

Do not move critical transactional state transitions into background jobs.

---

# 105. CACHING

Do not introduce caching prematurely.

First ensure:

- correct queries
- correct indexes
- pagination
- projection

Any cache must have a clear invalidation strategy.

---

# 106. CQRS

CQRS is NOT part of the current architecture.

Do not introduce:

- Commands
- Handlers
- Queries
- Query Handlers
- MediatR for ordinary operations

Application Services are the current orchestration model.

---

# 107. DOMAIN EVENTS

Use Domain Events only for meaningful business events that reduce coupling.

Do not create events for every CRUD action.

Critical synchronous invariants must remain explicit.

---

# 108. CONTROLLERS

Prefer ABP conventional API exposure when it fits.

If a custom HttpApi controller is needed:

It must remain thin.

Controller must not contain:

- business logic
- repository queries
- pricing logic
- transaction management
- global exception translation

---

# 109. API DESIGN

Use consistent REST conventions.

Use explicit action endpoints for meaningful business commands when needed.

Examples:

POST /orders/{id}/cancel
POST /driver/tasks/pickup/{id}/accept
POST /driver/tasks/delivery/{id}/verify-otp

Do not create:

PUT /orders/{id}/status

with arbitrary status input.

---

# 110. API VERSIONING

Do not introduce API versioning until a real backward compatibility requirement exists.

---

# 111. API RESPONSE CONSISTENCY

Use ABP DTO and error conventions.

Do not invent a new response envelope for every endpoint.

---

# 112. TESTING PRIORITY

Testing priority:

1. Domain invariants
2. State machines
3. Application use cases
4. Authorization
5. Tenant isolation
6. Resource ownership
7. Database constraints
8. API integration

Do not write tests only to increase numbers.

Tests protect behavior.

---

# 113. CURRENT TEST BASELINE

Current verified baseline before Application Service implementation:

- Domain Tests: 96
- Application Contract Tests: 26
- EF Core Tests: 3
- Total: 125 passing
- Failed: 0
- Skipped: 0

If future work unexpectedly reduces these tests, stop and investigate.

Do not delete tests to make a build pass.

---

# 114. TEST NAMING

Test names must describe behavior.

Preferred examples:

CreateOrder_ShouldReject_WhenPickupSlotDoesNotBelongToLaundry

ConfirmDelivery_ShouldFail_WhenOtpIsNotVerified

Customer_ShouldNotRead_OrderOwnedByAnotherCustomer

Driver_ShouldNotRead_TaskAssignedToAnotherDriver

Avoid vague names.

---

# 115. TENANT ISOLATION TESTS

Application Services must include tests proving tenant boundaries.

At minimum test:

- Tenant A cannot read Tenant B order
- Tenant A cannot use Tenant B ServicePrice
- Tenant A cannot mutate Tenant B driver/task
- Tenant A cannot attach data to Tenant B complaint

---

# 116. CUSTOMER OWNERSHIP TESTS

Test customer ownership explicitly:

- customer cannot read another customer's order
- customer cannot modify another customer's address
- customer cannot approve/reject another customer's adjustment
- customer cannot read another customer's complaint

---

# 117. DRIVER OWNERSHIP TESTS

Test driver ownership explicitly:

- driver cannot read another driver's task
- driver cannot mutate another driver's task
- driver cannot bypass assignment
- driver cannot generate OTP for unowned delivery task

---

# 118. APPLICATION CONTRACT REFERENCE TESTS

Keep Contract integrity tests.

Protect:

- exact approved interfaces
- DTO inventory
- no phantom DTO
- no orphan DTO
- no PaymentMethod
- no AdjustedItems
- no arbitrary BagEventType
- no ConfirmOrderAsync

If contracts intentionally change, update tests together with the approved decision.

---

# 119. ARABIC XML DOCUMENTATION — MANDATORY

Every new or modified backend code element must include useful Arabic XML Documentation where meaningful.

Required for:

- Classes
- Interfaces
- Aggregate Roots
- Entities
- Value Objects
- DTOs
- DTO properties
- Important methods
- Domain methods
- Application Service methods
- Constructors
- Enums
- Permission constants
- Error codes
- Infrastructure abstractions

Example:

/// <summary>
/// يمثل طلب العميل داخل المغسلة ويتحكم في دورة حياته وحالاته ومراحل معالجته.
/// </summary>

The goal is that Visual Studio hover/tooltips explain the code clearly in Arabic.

Do not add useless comment spam.

---

# 120. CODE COMMENTS

Use comments to explain WHY when code is non-obvious.

Do not comment every line.

Do not duplicate obvious code behavior in comments.

XML Summary is mandatory for public/important contract elements.

---

# 121. NULLABILITY

Use nullable reference types correctly.

Do not suppress warnings blindly.

Nullable must represent a real valid state.

Do not use null merely to avoid constructor requirements.

---

# 122. DATE AND TIME

Persist operational timestamps consistently, normally UTC.

Examples:

- CreatedAt
- CompletedAt
- VerifiedAt
- CollectedAt

Business scheduling concepts may use:

DateOnly
TimeOnly

for local laundry schedules.

Do not use device-local time as backend authority.

Use ABP clock/time abstractions where appropriate.

---

# 123. MONEY

Money uses decimal.

Never use float/double for money.

Database money columns should follow approved precision such as decimal(18,2).

Do not invent arbitrary maximum price caps in DTOs.

---

# 124. ENUMS

Use existing Domain.Shared enums.

Do not create duplicate DTO enums for:

- OrderStatus
- ProcessingStage
- PickupTaskStatus
- DeliveryTaskStatus
- ComplaintStatus
- BagEventType
- etc.

Do not use magic integers.

---

# 125. ENUM PERSISTENCE

Enum numeric values are persistence-sensitive.

Do not reorder or change numeric meanings casually after data is stored.

Any enum evolution must consider existing database data.

---

# 126. PACKAGES

AI MUST NOT add a NuGet package automatically.

Before adding a package:

1. Check .NET built-in functionality.
2. Check ABP functionality.
3. Check existing project packages.
4. Explain why new package is required.
5. Review security and maintenance implications.
6. Get approval.

---

# 127. BEFORE CREATING A CLASS

Search the project first.

Ask:

- Does the class already exist?
- Is there a DTO already representing this?
- Is this responsibility already owned elsewhere?
- Can an existing ABP abstraction solve it?

Do not duplicate responsibilities.

---

# 128. BEFORE CREATING A REPOSITORY

Ask:

Can ABP Generic Repository handle it?

If yes:
use it.

If no:
justify the custom repository.

Do not create repositories as ceremony.

---

# 129. BEFORE CREATING A DOMAIN SERVICE

Ask:

Can the Aggregate Root own this behavior?

If yes:
keep it in the Aggregate.

Create Domain Service only for logic that genuinely spans domain objects or
does not belong naturally to one aggregate.

---

# 130. BEFORE CREATING AN APPLICATION SERVICE

Check the locked list of 15 interfaces.

Do not create a new Application Service interface just because one implementation
method feels large.

First identify the correct existing owner.

---

# 131. BEFORE CREATING A DTO

Check the current 87 DTO inventory.

Do not create a second DTO with a shorter/different name for the same use case.

If a real gap exists:

report it before adding a new contract.

---

# 132. BEFORE MODIFYING CONTRACTS

Application.Contracts are locked.

AI must not rename/add/remove DTOs or interfaces during Application Service
implementation unless a genuine gap is demonstrated and approved.

Implementation should conform to contracts, not casually rewrite them.

---

# 133. BEFORE MODIFYING DOMAIN

Domain is locked.

AI must not change entities, state machines, value objects, migrations, or
domain invariants while implementing unrelated features.

Stop and report a real conflict first.

---

# 134. AI EXISTING CODE INSPECTION RULE

Before implementing anything, AI MUST inspect:

- related Domain entity/aggregate
- related Application.Contract interface
- related DTOs
- permissions
- error codes
- repositories/configuration
- tests
- current git status

AI MUST NOT assume something is missing without searching.

---

# 135. AI CHANGE-SCOPE RULE

Changes must remain focused.

Do not perform broad refactors while implementing an unrelated use case.

Do not rename folders, namespaces, abstractions, or entities unless necessary for
the current approved task.

---

# 136. AI NO-SILENT-FIX RULE

If AI discovers a contradiction between:

- Domain
- Application Contracts
- EF schema
- architectural rules

it must not silently "fix" whichever layer is easiest.

It must report:

1. Conflict
2. Expected rule
3. Actual implementation
4. Risk
5. Smallest proposed fix

Approval is required for architecture-changing fixes.

---

# 137. AI NO-GENERIC-STATUS RULE

AI MUST NOT create generic methods such as:

UpdateOrderStatusAsync
SetPickupTaskStatusAsync
SetDeliveryStatusAsync
RecordBagEventAsync(BagEventType type)

when explicit business commands exist.

---

# 138. AI FINANCIAL TRUST RULE

AI must never trust client-supplied:

- order subtotal
- total
- unit price
- delivery fee
- adjustment new total
- arbitrary service price outside authorized catalog management

Backend pricing is authoritative.

---

# 139. AI TENANT TRUST RULE

AI must never trust client-supplied TenantId to select tenant-owned data.

Use CurrentTenant and server-resolved ownership.

---

# 140. AI USER TRUST RULE

AI must never trust client-supplied UserId / CustomerId when authenticated
identity can determine the actor.

---

# 141. AI FILE SAFETY RULE

AI must not:

- delete uploaded/generated files
- delete migrations
- overwrite project documentation
- remove test files
- clean directories

without explicit approval.

---

# 142. GIT SAFETY RULE

Because uncommitted work may exist, AI MUST NOT run destructive Git commands
without explicit user approval.

Forbidden by default:

- git checkout <file>
- git restore
- git reset
- git clean
- git rebase
- force push

AI must inspect git status before risky operations.

---

# 143. GIT COMMIT RULE

AI must not create a commit unless the user explicitly asks for a commit.

Before committing:

- build
- run relevant tests
- inspect git status
- inspect staged files
- inspect git diff --cached --stat
- ensure no temporary files are included

---

# 144. GIT PUSH RULE

AI must not push unless the user explicitly asks.

Never force push without explicit approval and explanation.

---

# 145. BRANCHING

Developers should work on meaningful feature branches when work is divided.

Examples:

feature/orders-application-services

feature/logistics-driver-workflow

feature/complaints-attachments

fix/customer-order-ownership

Do not mix unrelated work in one branch.

---

# 146. COLLABORATION OWNERSHIP

When multiple developers are working simultaneously:

Each developer should own specific modules/features.

Do not modify another developer's internal module without coordination.

If a cross-module change is needed:

- use public contract if possible
- coordinate contract change explicitly
- do not bypass boundaries for speed

---

# 147. NO DUPLICATE IMPLEMENTATIONS

Before implementing:

Search for existing:

- Entity
- Aggregate
- DTO
- Application Service interface
- Application Service implementation
- Repository
- Domain Service
- Permission
- Error code
- Mapper
- Configuration
- Validator
- Test helper

Never create a second implementation of the same responsibility.

---

# 148. APPLICATION IMPLEMENTATION ORDER

Preferred implementation order:

1. Shared application helpers / actor resolvers if truly needed
2. Laundry/Host foundational services
3. Catalog
4. Customer profile
5. Customer order quoting/creation
6. Laundry order operations
7. Driver management
8. Driver tasks
9. Bags
10. Inspection
11. Adjustment orchestration
12. Complaints
13. Notifications
14. Mapping
15. Authorization tests
16. Tenant/ownership tests
17. API exposure verification

Keep dependencies manageable.

---

# 149. MAPPING TESTS

Important actor-specific DTO mappings should be tested where security matters.

Example:

CustomerOrderDetailDto must not accidentally expose internal tenant/staff data.

Mapping tests are especially valuable for security-sensitive output DTOs.

---

# 150. AUTHORIZATION TESTS

Application tests must verify:

- permission denied
- wrong actor
- wrong tenant
- wrong resource owner

Do not only test the happy path.

---

# 151. API EXPOSURE

Application Service implementation does not automatically mean every suggested
route is guaranteed.

Use ABP Conventional API where appropriate.

If a custom route is required:

create a thin HttpApi controller.

Do not duplicate business logic in controller.

---

# 152. API SECURITY

Suggested routes do not override ownership/security checks.

Even if a resource ID is valid:

the current actor must still be authorized to access it.

---

# 153. ROUTE ID VS BODY ID

If an entity ID is already in the route:

do not duplicate the same resource ID inside body DTO without a real reason.

Avoid ambiguity and mismatch attacks.

---

# 154. QR SECURITY

QR code is an identifier.

QR code is NOT proof of authorization.

All bag scan actions still require:

- authenticated actor
- permission
- tenant scope
- correct workflow state
- task/order relation

---

# 155. NOTIFICATION SECURITY

User notification queries must always filter by current user.

Do not expose tenant-wide notification feed unless explicitly required.

---

# 156. COMPLAINT SECURITY

Customer complaint operations require ownership.

Laundry complaint operations require:

- tenant scope
- complaint permission
- correct complaint tenant

Attachments require additional Blob ownership verification.

---

# 157. CUSTOMER ADDRESS RULE

Only one active default address per Customer.

SetDefaultAddress must be implemented safely and atomically.

Do not trust IsDefault alone from arbitrary update payloads.

---

# 158. LAUNDRY TIME SLOT RULE

LaundryTimeSlot is a weekly template.

Order historical schedule is stored separately in PickupScheduleSnapshot.

Do not read mutable time slot values later as historical order schedule truth.

---

# 159. WORKING HOURS RULE

Working hours and time slots belong to Laundry aggregate.

They do not have independent tenant ownership identifiers.

Tenant isolation is inherited through Laundry ownership.

---

# 160. TENANTID NULLABILITY RULE

ABP IMultiTenant uses Guid? TenantId.

Tenant-owned business aggregates still enforce non-null tenant invariant.

Do not make tenant-owned business objects logically host-owned just because the
interface type is nullable.

AppNotification may use nullable TenantId because it can target hybrid contexts.

---

# 161. DELETE POLICY

Do not expose generic Delete endpoints without checking lifecycle semantics.

Catalog/admin entities may support soft delete/deactivation.

Operational history generally must be retained.

Use domain-approved Activate/Deactivate or lifecycle commands.

---

# 162. STAFF LIFECYCLE

Laundry staff should be deactivated/reactivated rather than erased casually.

Use explicit lifecycle operations.

Preserve auditing/history.

---

# 163. DRIVER LIFECYCLE

Driver profile lifecycle must preserve history.

Do not delete driver records needed by historical tasks.

Use activation/availability semantics.

---

# 164. CATALOG LIFECYCLE

LaundryItemType, LaundryService, ServicePrice are catalog/admin data.

Use approved activation/deactivation/soft-delete behavior.

Historical OrderItem snapshots remain valid even when catalog entries are later deactivated.

---

# 165. COMPLAINT COMPENSATION

Compensation amount must be non-negative.

Do not create arbitrary maximum compensation caps in DTOs unless approved business
rules provide such a cap.

---

# 166. FINANCIAL PRECISION

Use decimal(18,2) where the existing schema/configuration specifies it.

Do not switch money precision casually.

---

# 167. CURRENT EXTERNAL PROVIDER STATUS

Do not assume these are already implemented:

- SMS provider
- Push provider
- Blob provider
- external map/geocoding provider

Check the project before creating integration code.

---

# 168. NO LIVE TRACKING

Current MVP does not include full live driver tracking.

Do not create:

- WebSocket GPS stream
- continuous location history
- Kafka location events
- tracking microservice

without an approved requirement.

---

# 169. DRIVER LOCATION

If task/location verification is needed, use only approved location fields and
workflow requirements.

Do not silently turn it into a full tracking system.

---

# 170. NO ADVANCED MULTI-BRANCH

Each Tenant = one laundry/company in MVP.

Do not add:

Branch
BranchId
multi-branch routing
branch inventory
branch-specific staff hierarchy

unless approved later.

---

# 171. NO INVENTORY

The current platform manages laundry services/orders, not warehouse inventory.

Do not add stock/inventory modules unless the requirement changes.

---

# 172. NO LOYALTY / COUPONS

Do not add points, coupons, promotional codes, campaign rules, or discounts engine
without approved scope expansion.

---

# 173. NO DYNAMIC PRICING

Service prices are predefined by the Laundry.

Do not implement dynamic surge pricing, customer-specific pricing, or time-based pricing
in the MVP.

---

# 174. NO ONLINE PAYMENT

Do not add payment gateway SDKs or payment callbacks.

COD is the only payment flow currently.

---

# 175. NO ACCOUNTING

Do not build ledger/accounting/reconciliation modules beyond operational COD collection
needed to complete delivery.

---

# 176. NO AI FEATURES

Do not add AI recommendations, automatic damage recognition, price prediction,
or intelligent routing in the MVP.

---

# 177. DOCUMENTATION FILES

Important project documentation should be maintained.

This file defines HOW backend code must be built.

A separate module/architecture document may define:

- responsibilities
- aggregates
- relationships
- public contracts
- dependencies
- workflows

Do not duplicate architecture truth across conflicting files.

---

# 178. SOURCE OF TRUTH ORDER

Use this hierarchy:

1. Explicit approved architecture decision from the project owner/team
2. AI_BACKEND_RULES.md
3. Approved module/domain documentation
4. Locked Domain and Application.Contracts implementation
5. Existing project conventions
6. ABP/.NET conventions
7. Individual developer preference

Personal preference must not override approved project architecture.

---

# 179. DEVELOPMENT WORKFLOW

Every feature follows:

Requirement
↓
Identify Actor
↓
Identify owning business area
↓
Inspect existing code
↓
Inspect locked contract
↓
Inspect Domain method
↓
Identify permissions/ownership
↓
Identify repositories
↓
Implement Application Service
↓
Map DTO
↓
Write tests
↓
Build
↓
Run tests
↓
Review git diff

Do not start coding before understanding ownership and security.

---

# 180. DEFINITION OF DONE

A backend feature is complete only when applicable items are done:

- Contract respected
- Domain method used correctly
- Authorization applied
- Tenant isolation verified
- Resource ownership verified
- Input validation applied
- Repository orchestration correct
- ABP UOW used correctly
- Mapping correct
- No client-authoritative protected fields
- Tests written for important behavior
- Negative authorization tests included
- Build succeeds
- Existing tests remain passing
- No accidental migration
- No unrelated refactor
- XML Documentation added
- Git diff reviewed

---

# 181. CODE REVIEW CHECKLIST

Reviewers must check:

- Correct actor
- Correct module/business owner
- Correct interface
- Correct DTO
- No Domain bypass
- No arbitrary status setters
- Permission
- Tenant scope
- Resource ownership
- Customer cross-tenant safety
- Repository usage
- UOW usage
- Query efficiency
- Financial trust boundary
- OTP security
- Bag/QR security
- File/Blob security
- Exception handling
- XML Documentation
- Tests
- Git scope

Reject unnecessary complexity.

---

# 182. GOLDEN RULE

DO NOT OVER-ENGINEER.

Prefer:

Simple
→ Correct
→ Secure
→ Maintainable
→ Testable
→ Consistent
→ Extensible

over:

Complex
→ Abstract
→ Theoretical
→ Distributed
→ Difficult to maintain

Every class, abstraction, repository, event, package, service, module,
background job, cache, or infrastructure component must have a clear reason to exist.

If it does not provide meaningful value to the approved requirement:

DO NOT ADD IT.

---

# 183. FINAL PROJECT ARCHITECTURE

The approved Backend architecture is:

ABP Framework 10.6.0
        ↓
.NET 10
        ↓
Modular Monolith
        ↓
Practical Clean Architecture
        ↓
Selective DDD
        ↓
15 Locked Application Service Contracts
        ↓
ABP Repository
        ↓
ABP Unit of Work
        ↓
EF Core
        ↓
SQL Server

Multi-Tenancy:
ABP Multi-Tenancy

Authentication:
ABP Identity + OpenIddict

Payment:
Cash On Delivery only

CQRS:
NOT USED

Microservices:
NOT USED

Event Sourcing:
NOT USED

Custom Authentication:
NOT USED

DDD:
Used where business complexity justifies it

Infrastructure:
Added only when .NET / ABP cannot reasonably satisfy the requirement

---

# 184. FINAL AI RULE

Before an AI coding agent modifies the backend, it MUST:

1. Read this file.
2. Inspect git status.
3. Inspect the relevant Application Contract.
4. Inspect the relevant Domain entity/service.
5. Inspect existing tests.
6. Identify Actor.
7. Identify Permission.
8. Identify Tenant Scope.
9. Identify Resource Ownership.
10. Identify trusted vs client-controlled fields.
11. Search for existing implementation.
12. Avoid locked Domain/Contract changes unless explicitly approved.
13. Avoid destructive Git commands.
14. Keep the change focused.
15. Build and test before declaring completion.

If any architectural contradiction is found:

STOP AND REPORT IT.

Do not silently redesign the project.

---

# 185. CUSTOMER CATALOG / SHARED BUSINESS DECISIONS

The following authoritative architectural decisions govern customer catalog, discovery, and ordering:

1. **Authoritative Minimum Order Amount**: `Laundry.MinimumOrderAmount` is the single authoritative business value for MVP across nearby laundry listings, customer catalogs, and order eligibility.
2. **Legacy CoverageArea Minimum**: `CoverageArea.MinimumOrderAmount` is legacy/duplicated model data and MUST NOT be used for pricing, display, or order eligibility until a future approved cleanup.
3. **Display Distance Semantics**: `LaundryNearbyListDto.DistanceKm` strictly represents the distance from the customer's coordinates to the physical laundry location (`Laundry.Latitude`, `Laundry.Longitude`).
4. **Coverage Eligibility Semantics**: Coverage area eligibility strictly uses the distance from customer coordinates to the coverage area center (`CoverageArea.CenterLatitude`, `CoverageArea.CenterLongitude`) against `CoverageArea.DeliveryRadiusKm`.
5. **No Zero-Coordinate Convention**: Coordinates (0,0) have no special "missing location" meaning. No fallback to coverage center coordinates is permitted.
6. **Customer Application Tenant Visibility**: Customer Application services trust `Laundry.IsActive` (and `Laundry.AcceptingOrders` for order-related workflows) as the customer-visible availability source without depending on Host/Tenant management services.
7. **Host Management Suspension Ownership**: Host Management (`ILaundryHostAppService` / Shaher scope) owns the synchronization between ABP Tenant suspension/activation and `Laundry.IsActive`.
8. **MaxDistanceKm Semantics**: `GetNearbyLaundriesInput.MaxDistanceKm` limits the maximum physical distance from the customer to the laundry (`Laundry.Latitude`, `Laundry.Longitude`). The coverage area center is strictly for delivery coverage eligibility (`coverageDistanceKm <= DeliveryRadiusKm`) and is NOT used for `MaxDistanceKm` filtering.


