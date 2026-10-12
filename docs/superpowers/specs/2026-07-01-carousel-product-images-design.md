# Carousel Product Images Design

## Goal

Make the homepage carousel image-backed instead of text-only.

## Scope

Use existing local product image assets from `wwwroot/images/products`. Each carousel slide keeps readable text and a call-to-action while adding a right-side product image collage. The carousel remains local/static and does not rely on external image URLs.

## Design

Slides use a dark blue image-backed card with a soft overlay for readability. Product images sit in a `.hero-visual` cluster on the right side. On smaller screens, the visual moves below the text.

## Verification

Run a Release build after Razor/CSS changes.
