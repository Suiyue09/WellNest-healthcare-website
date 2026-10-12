# Product Images Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add stable local product images and show them on storefront pages.

**Architecture:** Store images in `wwwroot/images/products`. Add a catalog helper for sample image URL assignment. Update Razor pages to render `<img>` when `Product.ImageUrl` exists and retain category emoji fallback otherwise.

**Tech Stack:** ASP.NET Core Razor Pages, C#, xUnit, CSS, local static files.

---

### Task 1: Local Image Assets

**Files:**
- Create: `wwwroot/images/products/*.png`

- [ ] Crop generated contact sheet into individual product PNG files.
- [ ] Save the original generated contact sheet copy for traceability.

### Task 2: Product Image Mapping

**Files:**
- Create: `Services/ProductImageCatalog.cs`
- Modify: `Data/SampleDataSeeder.cs`
- Test: `HealthcareIndustry.Tests/VoucherAndPaymentServiceTests.cs`

- [ ] Add a failing test for known product image lookup.
- [ ] Implement product image lookup helper.
- [ ] Update seeder to assign image URLs for known products.

### Task 3: Storefront Rendering

**Files:**
- Modify: `Pages/Customer/HomePage.cshtml`
- Modify: `Pages/Customer/ProductDetails.cshtml`
- Modify: `Pages/Customer/ShoppingCart.cshtml`
- Modify: `Pages/Customer/Checkout.cshtml`
- Modify: `Pages/Customer/Tracking.cshtml`
- Modify: `Pages/Customer/Tracking.cshtml.cs`
- Modify: `wwwroot/css/customer-pages.css`

- [ ] Render product image if available.
- [ ] Keep category emoji fallback if image is missing.
- [ ] Load product images for tracking order items by product id.

### Task 4: Verification

**Files:**
- Existing project files only.

- [ ] Run `dotnet test HealthcareIndustry.Tests/HealthcareIndustry.Tests.csproj -c Release`.
- [ ] Run `dotnet build "Healthcare Industry.csproj" -c Release`.
