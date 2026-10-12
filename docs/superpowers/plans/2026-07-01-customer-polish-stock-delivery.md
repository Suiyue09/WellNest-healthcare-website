# Customer Polish Stock Delivery Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve customer page polish and enforce stock-aware checkout while confirming admin and delivery database flows.

**Architecture:** Add small reusable helpers for initials and stock validation. Update existing Razor pages and page models in place. Keep admin stock and delivery assignment/status flows as existing database-driven behavior, with only customer-facing fixes added.

**Tech Stack:** ASP.NET Core Razor Pages, Entity Framework Core, C#, xUnit, shared CSS.

---

### Task 1: Shared Helpers

**Files:**
- Create: `Services/CustomerDisplayService.cs`
- Create: `Services/StockAvailabilityService.cs`
- Test: `HealthcareIndustry.Tests/VoucherAndPaymentServiceTests.cs`

- [ ] Add tests for full-name initials and stock validation.
- [ ] Run the focused test command and confirm the new tests fail because helpers do not exist.
- [ ] Implement the helpers.
- [ ] Run tests and confirm they pass.

### Task 2: Customer Page Polish

**Files:**
- Modify: `Pages/Customer/Profile.cshtml`
- Modify: `Pages/Customer/Profile.cshtml.cs`
- Modify: `Pages/Customer/ShoppingCart.cshtml`
- Modify: `Pages/Customer/Checkout.cshtml`
- Modify: `Pages/Customer/Tracking.cshtml`
- Modify: `wwwroot/css/customer-pages.css`

- [ ] Remove the profile voucher link.
- [ ] Add short-lived notice styling and script.
- [ ] Use name-based initials on profile, cart, checkout, tracking, history, and product detail pages where model data is available.
- [ ] Improve empty cart spacing.
- [ ] Remove the checkout reminder notice.
- [ ] Simplify cancellation messages.

### Task 3: Stock-Aware Cart And Checkout

**Files:**
- Modify: `Pages/Customer/ShoppingCart.cshtml.cs`
- Modify: `Pages/Customer/Checkout.cshtml.cs`

- [ ] Exclude or block out-of-stock selected cart items from checkout.
- [ ] Clamp quantities against current admin stock before placing an order.
- [ ] Show a concise short-lived message when stock changes require customer action.

### Task 4: Verify

**Files:**
- Existing project files only.

- [ ] Run `dotnet test HealthcareIndustry.Tests/HealthcareIndustry.Tests.csproj`.
- [ ] Run `dotnet build "Healthcare Industry.csproj" -c Release`.
- [ ] Report that admin stock control and delivery updates were checked in code.
