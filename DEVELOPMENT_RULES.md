# LAUNDRY SAAS PLATFORM
## DEVELOPMENT RULES & STANDARDS

---

### 1. Architecture & Core Stack
- **Architecture:** ABP Modular Monolith (Domain-Driven Design - DDD).
- **Backend:** ASP.NET Core (.NET 10).
- **Framework:** ABP Framework (10.6.0).
- **Database:** Microsoft SQL Server.
- **ORM:** Entity Framework Core (EF Core).
- **Primary Keys:** All new Domain Entities MUST use `Guid` as their primary key. Never use `int` identity unless an existing entity strictly dictates it.
- **Monetary Values (Money):**
  - In Domain: `decimal`
  - In SQL Server: `decimal(18,2)`
  - STRICTLY FORBIDDEN: `float`, `double` for currency or monetary calculations.
- **Time Infrastructure:** Standard ABP UTC / Clock conventions (`IClock`). Do not build custom time systems.

---

### 2. Multi-Tenancy & Security
- **Multi-Tenancy:** Strictly backend-enforced (`CurrentTenant`, `IMultiTenant`).
- **Zero Trust - TenantId:** NEVER trust `TenantId` sent from client apps (Flutter, Angular). Backend always resolves tenant from context/headers.
- **Zero Trust - Financials:** NEVER trust order totals, item prices, or delivery fees sent from client apps. All calculations must be computed on the backend.
- **Authentication:** ABP Identity + OpenIddict. Do not build custom JWT mechanisms.
- **Authorization:** Roles + Permissions + Tenant Context + Resource Ownership.

---

### 3. Business Logic & Clean Code
- **Business Logic:** Must NEVER reside inside API Controllers. Controllers are thin wrappers calling Application Services or Domain Services.
- **DTOs & Data Exposure:** Never expose Domain Entities directly to API clients or mobile apps. Always use DTOs (Data Transfer Objects).
- **Validation:** Rely on ABP automatic validation, Data Annotations, and FluentValidation.
- **Error Codes Convention:**
  - Base pattern: `laundry_SaaS:<Module>:<Number>`
  - Examples: `laundry_SaaS:Orders:001`, `laundry_SaaS:Pickup:001`, `laundry_SaaS:Processing:001`
- **Permissions Base Convention:**
  - Root: `laundry_SaaS`
  - Pattern: `laundry_SaaS.<Module>.<Action>` (e.g., `laundry_SaaS.Orders.Create`)

---

### 4. Payment Policy (Final MVP Decision)
- **Payment Method:** **Cash On Delivery (COD) only**.
- **No Payment Module in MVP:**
  - No Payment Module
  - No PaymentStatus
  - No Wallet system
  - No Online Payment
  - No Payment Gateway / Provider integration
- **Collection Workflow:** Cash collection is recorded as part of the Delivery Workflow by drivers, not as a standalone payment system.

---

### 5. Git & Collaboration Strategy
- **Branches:**
  - `main`: Production-ready releases only.
  - `develop`: Main development integration branch.
  - `feature/mushtaq-*`: Features developed by Mushtaq.
  - `feature/shaher-*`: Features developed by Shaher.
- **Rules:**
  - No direct commits to `main`.
  - Feature branches branch off `develop` and merge back into `develop` via PR / review.
  - No unauthorized force pushing or modifying remote histories.

---

### 6. Module Ownership & AI Boundaries
- **Ownership:** Follow [MODULE_OWNERSHIP.md](file:///d:/Laundry_Platfrorm_SaaS/MODULE_OWNERSHIP.md).
- **Modifications:** No developer may directly modify a module owned by the other without explicit prior agreement.
- **Shared Contracts:** Changes to `Domain.Shared` or shared contracts require mutual agreement.
- **AI Tools Policy:** AI agents/assistants must never modify, refactor, or delete files outside the current developer's owned scope.
