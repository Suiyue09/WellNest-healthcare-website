# Carousel Product Images Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add local product imagery to the homepage carousel.

**Architecture:** Update `Pages/Customer/HomePage.cshtml` only. The carousel remains static Razor markup with CSS-driven layout and existing local product image paths.

**Tech Stack:** ASP.NET Core Razor Pages, CSS, local static image assets.

---

### Task 1: Hero Visuals

**Files:**
- Modify: `Pages/Customer/HomePage.cshtml`

- [ ] Add `.hero-visual` CSS for local product images.
- [ ] Add product image clusters to each hero slide.
- [ ] Keep responsive behavior readable on mobile.
- [ ] Run `dotnet build "Healthcare Industry.csproj" -c Release`.
