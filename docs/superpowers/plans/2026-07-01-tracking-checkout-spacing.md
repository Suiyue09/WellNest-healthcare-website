# Tracking Checkout Spacing Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Improve tracking page spacing and send checkout directly to payment from one confirm button.

**Architecture:** Keep existing Razor Pages. Add page-specific tracking classes in shared customer CSS, and remove the checkout confirmation toggle by defaulting `ConfirmOrder` to true and deleting the review branch.

**Tech Stack:** ASP.NET Core Razor Pages, C#, xUnit, CSS.

---

### Task 1: Checkout Direct Confirm

**Files:**
- Modify: `HealthcareIndustry.Tests/VoucherAndPaymentServiceTests.cs`
- Modify: `Pages/Customer/Checkout.cshtml`
- Modify: `Pages/Customer/Checkout.cshtml.cs`

- [ ] Add a test proving new checkout input confirms by default.
- [ ] Run focused test and confirm it fails.
- [ ] Default `ConfirmOrder` to true, remove the review branch, and show one confirm button.
- [ ] Run focused test and confirm it passes.

### Task 2: Tracking Layout Spacing

**Files:**
- Modify: `Pages/Customer/Tracking.cshtml`
- Modify: `wwwroot/css/customer-pages.css`

- [ ] Add tracking page wrapper classes.
- [ ] Reduce oversized blank card space and add better internal gaps.
- [ ] Keep responsive behavior clean on mobile.

### Task 3: Verification

**Files:**
- Existing project files only.

- [ ] Run `dotnet test HealthcareIndustry.Tests/HealthcareIndustry.Tests.csproj -c Release`.
- [ ] Run `dotnet build "Healthcare Industry.csproj" -c Release`.
