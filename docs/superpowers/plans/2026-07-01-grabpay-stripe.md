# GrabPay Stripe Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add GrabPay as the first real e-wallet payment method through the existing Stripe Checkout flow.

**Architecture:** The checkout page stores `GatewayWallet` for GrabPay. `StripeCheckoutPaymentGateway` maps `GatewayBanking` to Stripe `fpx` and `GatewayWallet` to Stripe `grabpay`, then creates a normal Checkout Session. Stripe return verification remains unchanged.

**Tech Stack:** ASP.NET Core Razor Pages, C#, Stripe.net, xUnit.

---

### Task 1: Stripe Payment Method Mapping

**Files:**
- Modify: `Services/Payments/StripeCheckoutPaymentGateway.cs`
- Test: `HealthcareIndustry.Tests/VoucherAndPaymentServiceTests.cs`

- [ ] **Step 1: Write tests for payment method mapping**

Add tests that assert `GatewayBanking` maps to `fpx` and `GatewayWallet` maps to `grabpay`.

- [ ] **Step 2: Implement mapping**

Add a small internal helper on `StripeCheckoutPaymentGateway` that returns `["fpx"]`, `["grabpay"]`, or `["card"]` based on `Order.PaymentMethod`.

- [ ] **Step 3: Use mapping in Checkout Session**

Replace the hard-coded `PaymentMethodTypes = ["card"]` with the helper result and remove the demo fallback for `GatewayBanking` and `GatewayWallet` when a real Stripe key is configured.

- [ ] **Step 4: Run focused tests**

Run `dotnet test HealthcareIndustry.Tests/HealthcareIndustry.Tests.csproj`.

### Task 2: Checkout Copy

**Files:**
- Modify: `Pages/Customer/Checkout.cshtml`

- [ ] **Step 1: Rename e-wallet option**

Change the e-wallet label from generic gateway language to `GrabPay e-wallet`.

- [ ] **Step 2: Clarify FPX copy**

Make FPX copy mention Stripe FPX directly.

- [ ] **Step 3: Build project**

Run `dotnet build "Healthcare Industry.csproj"`.
