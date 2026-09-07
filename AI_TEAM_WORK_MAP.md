# AI TEAM WORK MAP
# Laundry SaaS Platform

Version: 1.0
Status: ACTIVE
Scope: BACKEND TEAM OWNERSHIP & PARALLEL DEVELOPMENT
Developers:
- مشتاق اليعري (Mushtaq Al-Yaari)
- شاهر (Shaher)

---

# 1. PURPOSE

هذه الوثيقة تحدد خريطة العمل الرسمية بين المطورين في مشروع Laundry SaaS Platform.

كل مطور وكل AI Coding Agent يجب أن يقرأ هذه الوثيقة قبل تعديل أي كود في الـ Backend.

الهدف الأساسي:

- منع تعارض العمل بين مشتاق وشاهر.
- منع تعديل المطور لملفات أو نطاق المطور الآخر بدون تنسيق.
- تحديد مسؤولية كل Application Service بوضوح.
- توضيح نقاط التكامل بين النطاقين.
- منع أي AI Agent من توسيع نطاق المهمة من نفسه.
- تسهيل الدمج في Git بدون Conflicts غير ضرورية.
- إبقاء Domain وApplication.Contracts المقفلة مستقرة أثناء تنفيذ Application Services.

هذه الوثيقة لا تستبدل:

AI_BACKEND_RULES.md

بل تعمل معها.

في حال وجود تعارض:

1. قرار معماري معتمد صريح.
2. AI_BACKEND_RULES.md
3. AI_TEAM_WORK_MAP.md
4. Domain/Application.Contracts المقفلة.
5. Existing project conventions.

---

# 2. CURRENT PROJECT STATUS

الحالة الحالية قبل تقسيم العمل:

- Foundation: COMPLETE
- Domain: LOCKED
- EF Core Domain Schema: LOCKED
- Domain Hardening: COMPLETE
- Domain Tests: 96 Passed
- Application.Contracts: LOCKED
- Contract DTOs: 87
- Application Service Interfaces: 15
- Application Contract Tests: 26 Passed
- EF Core Tests: 3 Passed
- Total verified tests baseline: 125 Passed
- Application Service Implementations: NEXT PHASE

المرحلة الحالية التي يعمل عليها المطوران:

APPLICATION SERVICE IMPLEMENTATIONS

ثم لاحقًا:

- Authorization / Ownership verification
- API exposure verification
- Integration tests
- Flutter / Angular integration

---

# 3. GOLDEN OWNERSHIP RULE

كل مطور يملك نطاقًا واضحًا.

المطور أو الـ AI Agent:

MUST:
- يعمل فقط داخل النطاق المخصص له.
- يقرأ الكود المشترك عند الحاجة.
- يستخدم Domain methods الموجودة.
- يستخدم Application.Contracts الموجودة.
- يضيف Tests تخص نطاقه.

MUST NOT:
- يعيد تصميم Domain.
- يغير Contracts المقفلة.
- يعدل نطاق المطور الآخر بدون تنسيق صريح.
- ينشئ DTO بديل لنفس الوظيفة.
- ينشئ Application Service Interface جديد بدون قرار معماري.
- ينفذ feature يخص المطور الآخر لمجرد أنه يحتاجه مؤقتًا.

إذا احتاج تغييرًا خارج نطاقه:

STOP AND REPORT CROSS-OWNER CHANGE REQUIRED.

---

# 4. DEVELOPER A — مشتاق اليعري

Owner:

MUSHTAQ

Primary responsibility:

CUSTOMER + ORDERS + INSPECTIONS + COMPLAINTS + NOTIFICATIONS

المجالات التي يملكها مشتاق:

1. Customer Profile
2. Customer-facing Catalog
3. Customer Orders
4. Laundry-side Order Processing
5. Inspection
6. Order Adjustment orchestration
7. Complaints
8. App Notifications

---

# 5. MUSHTAQ — APPROVED APPLICATION SERVICES

مشتاق مسؤول عن تنفيذ هذه الواجهات:

1. ICustomerProfileAppService
2. ICustomerCatalogAppService
3. ICustomerOrderAppService
4. ILaundryOrderAppService
5. IInspectionAppService
6. IComplaintAppService
7. IAppNotificationAppService

عدد الواجهات التابعة لمشتاق:

7 Application Services

---

# 6. MUSHTAQ — IMPLEMENTATION ORDER

يتم التنفيذ بهذا الترتيب قدر الإمكان:

## Phase M1 — CustomerProfileAppService

يشمل:

- Get current customer profile
- Update profile
- Get addresses
- Get address
- Create address
- Update address
- Delete/deactivate address حسب العقد الحالي
- SetDefaultAddressAsync

Security:

Authentication
→ Customer Profile
→ Resource Ownership

ممنوع الاعتماد على CustomerId من Client.

---

## Phase M2 — CustomerCatalogAppService

يشمل:

- Nearby laundries
- Laundry catalog
- Active item types/services/prices
- Coverage validation/query preparation

Rules:

- Customer must not receive internal Tenant operational data.
- No client pricing authority.
- Catalog prices are read from Backend.
- Nearby query must respect CoverageArea.

---

## Phase M3 — CustomerOrderAppService

يشمل:

- CalculateQuote
- CreateOrder
- GetMyOrders
- Get customer order detail
- Customer cancellation
- Accept/approve adjustment
- Reject adjustment

CreateOrder flow:

Authenticated Customer
→ Resolve Customer
→ Load Laundry
→ Verify Laundry active/accepting orders
→ Validate PickupDate + PickupSlotId
→ Load trusted ServicePrice
→ Calculate authoritative quote
→ Create immutable address/schedule/item snapshots
→ Create Order
→ Confirm immediately
→ PendingPickup
→ Save in one UOW

ممنوع:

- client Total
- client UnitPrice
- client DeliveryFee
- client CustomerId
- client TenantId
- ConfirmOrderAsync

---

## Phase M4 — LaundryOrderAppService

يشمل:

- Laundry order listing
- Laundry order detail
- StartInspection
- CreateAdjustment
- StartProcessing
- MoveToNextProcessingStage
- MarkReadyForDelivery
- Administrative cancellation

Security:

Authentication
→ Laundry Staff Profile
→ Permission
→ Current Tenant
→ Order.TenantId ownership

CreateAdjustment must use:

Order
+
stored official Inspection
+
trusted ServicePrice
+
OrderAdjustmentDomainService

لا يقبل NewTotal أو mismatch lines من Flutter.

---

## Phase M5 — InspectionAppService

يشمل:

- Get inspection by order
- Get inspection
- Start inspection
- UpsertInspectionItem
- RecordDamage
- Complete inspection
- Reopen inspection

Important:

Client submits ACTUAL values only.

Backend determines EXPECTED values from OrderItem.

UpsertInspectionItemInput must never be converted إلى trusted expected snapshot from Client.

Inspection completion must preserve Domain invariant:
every original OrderItem represented.

---

## Phase M6 — ComplaintAppService

يشمل:

- My complaints
- Laundry complaints
- Get complaint
- Create complaint
- Add attachment metadata
- Review
- Resolve
- Close

Security:

Customer side:
Authentication
→ Customer Profile
→ Complaint ownership

Laundry side:
Authentication
→ Staff Profile
→ Permission
→ Tenant
→ Complaint tenant ownership

Attachment rule:

BlobName from Client is not automatically trusted.

Application must later verify Blob existence/ownership before linking.

No Base64/Binary inside contract.

---

## Phase M7 — AppNotificationAppService

يشمل:

- GetMyNotifications
- GetUnreadCount
- MarkAsRead
- MarkAllAsRead

Ownership:

TargetUserId == CurrentUser.Id

Never expose another user's notification.

Notifications are not business state authority.

---

# 7. MUSHTAQ — PRIMARY FOLDERS

مشتاق يستطيع إنشاء/تعديل Application implementation files التي تخص نطاقه داخل:

src/laundry_SaaS.Application/Customers/
src/laundry_SaaS.Application/Orders/
src/laundry_SaaS.Application/Inspections/
src/laundry_SaaS.Application/Complaints/
src/laundry_SaaS.Application/Notifications/

ويمكنه إنشاء Tests الخاصة بنطاقه داخل:

test/laundry_SaaS.Application.Tests/Customers/
test/laundry_SaaS.Application.Tests/Orders/
test/laundry_SaaS.Application.Tests/Inspections/
test/laundry_SaaS.Application.Tests/Complaints/
test/laundry_SaaS.Application.Tests/Notifications/

يمكنه READ من بقية Domain/Contracts/EF files.

لا يعدلها إلا وفق Shared Change Protocol.

---

# 8. DEVELOPER B — شاهر

Owner:

SHAHER

Primary responsibility:

HOST + LAUNDRY + STAFF + CATALOG + DRIVERS + LOGISTICS + BAGS

المجالات التي يملكها شاهر:

1. Host Management
2. Laundry Profile
3. Laundry Working Hours
4. Laundry Time Slots
5. Laundry Staff
6. Laundry Catalog & Prices
7. Driver Management
8. Pickup Tasks
9. Delivery Tasks
10. OTP/COD orchestration
11. Bags / Chain of Custody

---

# 9. SHAHER — APPROVED APPLICATION SERVICES

شاهر مسؤول عن تنفيذ هذه الواجهات:

1. ILaundryHostAppService
2. ILaundryProfileAppService
3. ILaundryTimeSlotAppService
4. ILaundryStaffAppService
5. ILaundryCatalogAppService
6. IDriverManagementAppService
7. IDriverTaskAppService
8. IBagAppService

عدد الواجهات التابعة لشاهر:

8 Application Services

---

# 10. SHAHER — IMPLEMENTATION ORDER

## Phase S1 — LaundryProfileAppService

يشمل:

- Get laundry profile
- Update laundry profile
- Set CoverageArea
- Working Hours
- Set working hour

Security:

Authentication
→ Laundry Staff
→ Permission
→ Current Tenant

لا يقبل TenantId من Client.

---

## Phase S2 — LaundryTimeSlotAppService

يشمل:

- List time slots
- Available slots
- Create
- Update
- Activate/Deactivate

Rules:

- Time slot belongs to Laundry aggregate.
- No independent TenantId ownership on child entity.
- Validate WorkingHour constraints.
- Do not hard-delete operational template unless approved lifecycle permits it.

---

## Phase S3 — LaundryStaffAppService

يشمل:

- List staff
- Get staff
- Create/link staff profile
- Update
- Activate/Deactivate

Use:

ABP Identity

Do not create custom authentication.

Preserve audit/history.

---

## Phase S4 — LaundryCatalogAppService

يشمل:

LaundryItemType:
- Get
- List
- Create
- Update
- Activate/Deactivate
- Soft delete if contract/domain supports it

LaundryService:
- Get
- List
- Create
- Update
- Activate/Deactivate
- Soft delete if approved

ServicePrice:
- Get
- List
- Set
- Activate/Deactivate
- Soft delete if approved

Rules:

- Tenant-scoped
- No cross-tenant pricing
- Decimal money
- Unique service-price combination
- Historical OrderItem snapshots unaffected by catalog changes

---

## Phase S5 — DriverManagementAppService

يشمل:

- Driver list
- Get driver
- Create driver
- Update driver
- Availability
- Driver lookup
- Assign pickup driver
- Assign delivery driver

Assignment security:

Laundry Staff
→ Logistics.AssignTasks
→ Current Tenant
→ Driver belongs to Tenant
→ Order belongs to Tenant
→ Domain state permits assignment

Driver does not assign drivers.

---

## Phase S6 — DriverTaskAppService

### Pickup workflow

Assigned
→ Accepted
→ OutForPickup
→ Arrived
→ PickedUp
→ Completed

Methods:

- GetMyTasks
- GetPickupTask
- AcceptPickupTask
- StartPickupTrip
- ArriveAtPickup
- ConfirmPickup
- CompletePickup
- FailPickupTask

Driver ownership:

Task.DriverId == CurrentDriver.Id

---

### Delivery workflow

Assigned
→ Accepted
→ PickedUpFromLaundry
→ OutForDelivery
→ Arrived
→ Delivered

Methods:

- GetDeliveryTask
- AcceptDeliveryTask
- PickUpFromLaundry
- StartDeliveryTrip
- ArriveAtDelivery
- RequestDeliveryOtp
- VerifyDeliveryOtp
- ConfirmCashCollection
- ConfirmDelivery
- FailDeliveryTask

Security:

Authentication
→ Driver Profile
→ Logistics.ExecuteTasks
→ Current Tenant
→ Task.DriverId ownership

---

## Phase S7 — OTP + COD Application Orchestration

OTP rules are already locked in Domain.

Application is responsible for:

- generating secure 6-digit OTP
- hashing OTP
- sending through approved abstraction/provider
- comparing entered OTP securely
- calling Domain verification methods

Domain must not receive plaintext persistence.

Final delivery requires:

Verified OTP
+
Exact COD

ConfirmDelivery must coordinate:

task.ConfirmDelivery(...)
order.MarkAsDelivered()
order.Complete()

in one Unit of Work.

If SMS provider is not implemented:

STOP and report infrastructure dependency.

Do not add NuGet package without approval.

---

## Phase S8 — BagAppService

يشمل:

- Get bags for order
- Find by QR
- Assign bag to order
- Generate BagNumber
- Generate QR
- Scan QR

Rules:

Backend generates BagNumber and QR.

Client cannot send BagEventType.

QR is identifier only, not authorization.

Backend derives custody event from workflow context.

---

## Phase S9 — LaundryHostAppService

يشمل:

- Create Laundry Tenant
- Activate Tenant/Laundry
- Suspend Tenant/Laundry
- Get Tenant/Laundry
- List
- Platform Statistics

Host operation must coordinate:

ABP Tenant
+
Laundry aggregate

Host only.

No tenant staff access.

---

# 11. SHAHER — PRIMARY FOLDERS

شاهر يستطيع إنشاء/تعديل Application implementation files التي تخص نطاقه داخل:

src/laundry_SaaS.Application/HostManagement/
src/laundry_SaaS.Application/Laundries/
src/laundry_SaaS.Application/Catalog/
src/laundry_SaaS.Application/Drivers/
src/laundry_SaaS.Application/PickupDelivery/
src/laundry_SaaS.Application/Bags/

ويمكنه إنشاء Tests داخل:

test/laundry_SaaS.Application.Tests/HostManagement/
test/laundry_SaaS.Application.Tests/Laundries/
test/laundry_SaaS.Application.Tests/Catalog/
test/laundry_SaaS.Application.Tests/Drivers/
test/laundry_SaaS.Application.Tests/PickupDelivery/
test/laundry_SaaS.Application.Tests/Bags/

يمكنه READ من بقية Domain/Contracts/EF files.

لا يعدلها إلا وفق Shared Change Protocol.

---

# 12. OWNERSHIP MATRIX

| Area | مشتاق | شاهر |
|---|---|---|
| Host Management | READ | OWNER |
| Laundry Profile | READ | OWNER |
| Working Hours | READ | OWNER |
| Time Slots | READ | OWNER |
| Staff | READ | OWNER |
| Laundry Catalog | CONSUMER/READ | OWNER |
| Service Prices | CONSUMER/READ | OWNER |
| Customer Profile | OWNER | READ |
| Customer Catalog API | OWNER | PROVIDER DATA |
| Customer Orders | OWNER | READ/INTEGRATION |
| Laundry Order Processing | OWNER | INTEGRATION |
| Inspection | OWNER | READ |
| Order Adjustment | OWNER | READ |
| Complaints | OWNER | READ/LAUNDRY ACTOR |
| Notifications | OWNER | CONSUMER |
| Driver Management | READ | OWNER |
| Pickup Tasks | INTEGRATION | OWNER |
| Delivery Tasks | INTEGRATION | OWNER |
| OTP / COD orchestration | READ | OWNER |
| Bags | READ/INTEGRATION | OWNER |

OWNER = المطور المسؤول عن التنفيذ الداخلي.
READ = يمكنه القراءة فقط.
CONSUMER = يستخدم البيانات/العقود بدون تعديل داخلي.
INTEGRATION = يلتقي النطاقان عبر Domain/API orchestration مع عدم كسر الملكية.

---

# 13. WORKFLOW HANDOFF MAP

المسار الرئيسي للطلب بين النطاقين:

Customer
   ↓
[MUSHTAQ]
CustomerProfile / CustomerCatalog
   ↓
[MUSHTAQ]
Quote + CreateOrder
   ↓
Order = PendingPickup
   ↓
[SHAHER]
Assign Pickup Driver
   ↓
[SHAHER]
Pickup Workflow
   ↓
Complete Pickup
   ↓
Order = ReceivedAtLaundry
   ↓
[MUSHTAQ]
Inspection
   ↓
[MUSHTAQ]
Adjustment if mismatch
   ↓
[MUSHTAQ]
Processing
   ↓
[MUSHTAQ]
ReadyForDelivery
   ↓
[SHAHER]
Assign Delivery Driver
   ↓
[SHAHER]
Delivery Workflow
   ↓
OTP + COD
   ↓
[SHAHER]
Delivered + Completed
   ↓
[MUSHTAQ]
Customer history / complaint / notifications access

---

# 14. CROSS-OWNER INTEGRATION RULE

عند الحاجة لكي يستخدم أحد المطورين بيانات نطاق الآخر:

Preferred:

- Use existing Domain Aggregate/Repository appropriately inside the current monolith
- Respect aggregate ownership
- Use locked public Application Contract where the communication is truly application-level
- Reuse existing domain methods
- Keep orchestration explicit

Do NOT:

- redesign other owner's entity
- duplicate entity
- create shadow DTO
- modify another owner's service silently
- write direct SQL to bypass other owner's logic

---

# 15. SHARED / LOCKED FILES

هذه الملفات أو الفئات تعتبر مشتركة ومقفلة أثناء تنفيذ Application Services:

src/laundry_SaaS.Domain.Shared/**
src/laundry_SaaS.Domain/**
src/laundry_SaaS.Application.Contracts/**
src/laundry_SaaS.EntityFrameworkCore/**
Migrations/**
AI_BACKEND_RULES.md
AI_TEAM_WORK_MAP.md

وكذلك الملفات المركزية مثل:

laundry_SaaSApplicationModule.cs
laundry_SaaSPermissions.cs
laundry_SaaSPermissionDefinitionProvider.cs
DbContext
DbContextModelCreatingExtensions
Domain Shared Enums
Domain Error Codes

لا يعدلها مشتاق أو شاهر بشكل منفرد أثناء مهمة Application Service عادية.

---

# 16. SHARED CHANGE PROTOCOL

إذا احتاج أي مطور تعديل ملف Shared/Locked:

1. STOP current implementation.
2. Do NOT modify the shared file yet.
3. Record:
   - required change
   - why current design is insufficient
   - affected owner
   - affected tests
   - migration impact
4. Notify the other developer.
5. Agree on one developer to perform the shared change.
6. Make the smallest possible change.
7. Run full build/tests.
8. Commit shared change separately if practical.
9. Both branches sync before continuing.

AI Agent must output:

CROSS-OWNER / SHARED CHANGE REQUIRED

instead of silently modifying shared architecture.

---

# 17. DOMAIN LOCK RULE

During this phase:

Domain is LOCKED.

Neither developer nor AI Agent may modify:

- Aggregate rules
- Entity fields
- Value Objects
- State machines
- Domain Services
- Domain enums

unless a genuine blocker is found and approved.

Application code must adapt to the Domain, not redesign it.

---

# 18. CONTRACT LOCK RULE

Application.Contracts are LOCKED.

Current baseline:

- 15 approved interfaces
- 87 DTOs

Do not create:

- extra DTO
- duplicate DTO
- extra AppService interface
- shorter alternate DTO name

without explicit approved contract change.

---

# 19. PERMISSION OWNERSHIP

Shared permissions are already defined.

Each developer uses existing permissions.

Do not create permission constants casually.

If missing permission is discovered:

SHARED CHANGE PROTOCOL applies.

---

# 20. APPLICATION SERVICE SECURITY CHECKLIST

Every service method must answer:

1. Is user authenticated?
2. Which Actor is this?
3. Does actor profile exist?
4. Which Permission is required?
5. Which Tenant should own this resource?
6. Does resource belong to this Tenant?
7. Does resource belong to this Customer/Driver when applicable?
8. Which fields are trusted from Backend?
9. Which fields came from Client?
10. Which Domain method performs the state transition?

If these are not clear, do not finish implementation.

---

# 21. MUSHTAQ SECURITY TEST RESPONSIBILITY

مشتاق مسؤول عن اختبارات:

- Customer ownership
- Customer cross-tenant Order query security
- Customer address ownership
- Quote cannot accept client price
- CreateOrder backend pricing
- Pickup slot integration
- Customer cannot access another customer order
- Customer cannot approve/reject another customer's adjustment
- Inspection expected fields not trusted from client
- Complaint ownership
- Notification TargetUser ownership

---

# 22. SHAHER SECURITY TEST RESPONSIBILITY

شاهر مسؤول عن اختبارات:

- Laundry tenant isolation
- Staff tenant isolation
- Catalog tenant isolation
- Driver tenant isolation
- Driver task ownership
- Driver cannot assign driver
- Wrong driver cannot mutate task
- OTP cannot be generated for unowned task
- COD exact match
- OTP + COD final delivery
- Bag scan authorization
- QR does not bypass permission
- Host operation cannot run in Tenant context

---

# 23. DO NOT DELETE OTHER OWNER'S TESTS

Neither developer nor AI Agent may delete, disable, skip, rename away, or weaken
the other developer's tests to make a build pass.

If a test from another ownership area fails:

STOP.

Report:

CROSS-OWNER REGRESSION FOUND.

---

# 24. BRANCH STRATEGY

Base branch:

develop

مشتاق يعمل على:

feature/customer-orders-application

شاهر يعمل على:

feature/laundry-logistics-application

Do not work directly on main.

Avoid direct long-term development on develop.

---

# 25. INITIAL BRANCH CREATION

MUSHTAQ:

git checkout develop
git pull origin develop
git checkout -b feature/customer-orders-application

SHAHER:

git checkout develop
git pull origin develop
git checkout -b feature/laundry-logistics-application

Each branch starts from the same approved checkpoint.

---

# 26. COMMIT SCOPE — MUSHTAQ

Preferred focused commits:

feat(customer): implement customer profile application service

feat(catalog): implement customer catalog queries

feat(orders): implement customer order application service

feat(orders): implement laundry order processing service

feat(inspections): implement inspection application service

feat(complaints): implement complaint application service

feat(notifications): implement user notification application service

Do not wait until all seven services are done to make one giant commit.

---

# 27. COMMIT SCOPE — SHAHER

Preferred focused commits:

feat(laundry): implement laundry profile application service

feat(laundry): implement time slot application service

feat(staff): implement laundry staff application service

feat(catalog): implement laundry catalog application service

feat(drivers): implement driver management service

feat(logistics): implement driver task workflows

feat(bags): implement bag application service

feat(host): implement host management service

---

# 28. BEFORE EVERY COMMIT

Both developers run:

dotnet build laundry_SaaS.slnx

dotnet test laundry_SaaS.slnx

git status

git diff --stat

No commit should knowingly contain failing existing tests.

---

# 29. DO NOT USE DESTRUCTIVE GIT COMMANDS

AI Agent must not use without explicit approval:

git checkout <file>
git restore
git reset
git clean
git rebase
force push

Uncommitted work may exist.

Always inspect git status first.

---

# 30. SYNC RULE

Before merging or when one branch needs a shared change from develop:

- Commit own work first.
- Fetch/pull safely.
- Resolve conflicts deliberately.
- Do not overwrite other owner's changes.
- Run full tests after sync.

---

# 31. MERGE ORDER

Recommended:

1. Finish small coherent service.
2. Tests pass.
3. Push feature branch.
4. Review.
5. Merge into develop.
6. Other developer syncs develop.
7. Continue next dependent phase.

Do not keep both branches diverged for a very long period if they share dependencies.

---

# 32. HIGH-RISK INTEGRATION POINTS

The following areas are cross-owner and require extra care:

## A. CreateOrder

Owner:
MUSHTAQ

Reads:
Laundry
LaundryTimeSlot
LaundryWorkingHour
ServicePrice
LaundryItemType
LaundryService

Must not mutate Shahr-owned catalog configuration.

---

## B. AssignPickupDriver

Owner:
SHAHER

Uses:
Order

Must call existing Order Domain transition.

Must not redesign Order.

---

## C. CompletePickup

Owner:
SHAHER

Changes:
PickupTask
+
Order transition to ReceivedAtLaundry

Must be atomic.

---

## D. StartInspection

Owner:
MUSHTAQ

Starts after:
ReceivedAtLaundry

Do not let Driver module start inspection.

---

## E. ReadyForDelivery

Owner:
MUSHTAQ

Ends laundry processing.

Handoff to Shaher after Order becomes ReadyForDelivery.

---

## F. AssignDeliveryDriver

Owner:
SHAHER

Requires:
Order ReadyForDelivery
Driver same Tenant

---

## G. FinalDelivery

Owner:
SHAHER

Touches:
DeliveryTask
Order

Requires:
OTP verified
COD exact

One UOW:
DeliveryTask Delivered
Order Delivered
Order Completed

---

# 33. SOURCE OF TRUTH AT HANDOFFS

At every handoff:

Database/Domain state is the source of truth.

Do not use:

- Flutter local status
- Notification status
- AI agent assumptions
- temporary variables from another request

to determine workflow stage.

---

# 34. FILE CREATION RULE

Each AI Agent may create implementation files only in the owner's Application/Test
folders unless a shared change has been approved.

Before creating any class:

search project for an existing class.

---

# 35. XML DOCUMENTATION RULE

كل كود جديد أو معدل يجب أن يحتوي XML Documentation عربية مفيدة حسب
AI_BACKEND_RULES.md.

إلزامي خصوصًا لـ:

- Application Service class
- constructor
- injected dependency meaning when useful
- public service methods
- helper classes
- infrastructure abstractions
- non-obvious protected/private helpers where meaningful

مثال:

/// <summary>
/// ينفذ حالات استخدام طلبات العميل مع التحقق من ملكية العميل
/// واعتماد الأسعار والمواعيد من الخادم.
/// </summary>

Do not add useless comment spam.

---

# 36. AI AGENT STARTUP INSTRUCTION

عند بداية أي جلسة Coding Agent:

يجب أن يقرأ:

1. AI_BACKEND_RULES.md
2. AI_TEAM_WORK_MAP.md

ثم يحدد:

CURRENT DEVELOPER OWNER = MUSHTAQ

أو:

CURRENT DEVELOPER OWNER = SHAHER

ولا يبدأ التنفيذ قبل معرفة صاحب الجلسة.

---

# 37. AI AGENT OWNER HEADER

يفضل أن يبدأ كل Prompt تنفيذي بهذه الصيغة:

DEVELOPER OWNER: MUSHTAQ

أو:

DEVELOPER OWNER: SHAHER

AI Agent MUST restrict modifications to that owner's area.

---

# 38. AI OUT-OF-SCOPE RESPONSE

إذا طلبت مهمة لا تخص Owner الحالي، على الـ Agent أن يقول:

OUT OF OWNER SCOPE

Affected owner: MUSHTAQ / SHAHER

ثم يشرح الحاجة بدون تنفيذ داخلي في نطاق المطور الآخر.

---

# 39. AI SHARED-CONFLICT RESPONSE

إذا احتاج تعديل Shared file:

SHARED CHANGE REQUIRED

ثم يعرض:

- File
- Why needed
- Owner impact
- Suggested minimal change
- Tests affected

ولا ينفذ إلا بعد الموافقة.

---

# 40. NO ARCHITECTURAL EXPANSION

Neither agent may introduce:

- CQRS
- MediatR handlers
- Microservices
- Message broker
- new database
- Payment module
- Wallet
- dynamic pricing
- full live tracking
- multi-branch
- AI feature
- inventory
- loyalty/coupons

during assigned Application Service work.

---

# 41. DEFINITION OF DONE — INDIVIDUAL SERVICE

A service is complete when:

- All locked interface methods implemented.
- No extra public methods added without reason.
- Required permissions enforced.
- Actor profile enforced.
- Tenant scope enforced.
- Resource ownership enforced.
- Domain methods used.
- No protected status direct assignment.
- DTO mapping correct.
- No sensitive field leaked.
- Unit tests added.
- Negative security tests added.
- Build succeeds.
- Full existing tests pass.
- Git diff is scoped.
- Arabic XML docs added.

---

# 42. DEFINITION OF DONE — MUSHTAQ TRACK

Mushtaq track is complete when these 7 implementations are finished:

[ ] CustomerProfileAppService
[ ] CustomerCatalogAppService
[ ] CustomerOrderAppService
[ ] LaundryOrderAppService
[ ] InspectionAppService
[ ] ComplaintAppService
[ ] AppNotificationAppService

And all Mushtaq security/ownership tests pass.

---

# 43. DEFINITION OF DONE — SHAHER TRACK

Shaher track is complete when these 8 implementations are finished:

[ ] LaundryHostAppService
[ ] LaundryProfileAppService
[ ] LaundryTimeSlotAppService
[ ] LaundryStaffAppService
[ ] LaundryCatalogAppService
[ ] DriverManagementAppService
[ ] DriverTaskAppService
[ ] BagAppService

And all Shaher tenant/driver/logistics security tests pass.

---

# 44. FINAL INTEGRATION CHECK

After both tracks merge into develop:

Run:

dotnet build laundry_SaaS.slnx

dotnet test laundry_SaaS.slnx

Verify:

- Existing 125 baseline tests remain passing.
- New Application tests pass.
- No duplicated Application Services.
- No extra DTOs without approval.
- No extra Interfaces without approval.
- No accidental Migration.
- No cross-tenant leakage.
- No customer ownership leakage.
- No driver task ownership leakage.
- Swagger/Application exposure verified later.

---

# 45. FINAL TEAM RULE

MUSHTAQ owns:

CUSTOMER
+
ORDERS
+
INSPECTION
+
COMPLAINTS
+
NOTIFICATIONS

SHAHER owns:

HOST
+
LAUNDRY
+
STAFF
+
CATALOG
+
DRIVERS
+
PICKUP/DELIVERY
+
BAGS

Neither developer competes for ownership.

The system is built through explicit handoffs.

When one developer needs the other's internal change:

COORDINATE FIRST.

Do not bypass boundaries for speed.

---

# 46. VISUAL OWNERSHIP SUMMARY

                    Laundry SaaS Backend

                           develop
                              |
               -------------------------------
               |                             |
               |                             |
          MUSHTAQ TRACK                 SHAHER TRACK
               |                             |
     Customer Profile                 Host Management
     Customer Catalog                 Laundry Profile
     Customer Orders                  Time Slots
     Laundry Orders                   Staff
     Inspection                       Catalog
     Adjustments                      Driver Management
     Complaints                       Pickup Tasks
     Notifications                    Delivery Tasks
                                      OTP / COD
                                      Bags
               |                             |
               -------- Workflow Handoffs ----
                              |
                         Integration
                              |
                            develop

---

# 47. FINAL AI INSTRUCTION

Before modifying code, every AI Coding Agent MUST answer internally:

1. Who is the current developer?
2. Is this file inside their ownership?
3. Is the file Shared/Locked?
4. Is the Domain change required?
5. Is the Contract change required?
6. Is another developer affected?
7. Is there a safer public boundary?
8. What tests belong to this owner?

If current owner does not own the change:

DO NOT IMPLEMENT IT.

If a shared architectural change is required:

STOP AND REPORT IT.

