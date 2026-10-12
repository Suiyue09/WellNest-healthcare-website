# Product Images Design

## Goal

Use local product images across the storefront instead of relying on external website image links.

## Scope

Generate a clean local product image set, crop it into per-product files under `wwwroot/images/products`, map sample product names to those image paths, and render `Product.ImageUrl` in customer-facing product areas with the existing category emoji as fallback.

## Architecture

`ProductImageCatalog` owns the sample product name-to-image path mapping. `SampleDataSeeder` applies those paths when products are created or when known sample products have no image yet. Razor pages render `ImageUrl` when present and keep the emoji fallback for products without images.

## Testing

Unit tests cover the product image catalog mapping. Release test/build commands verify the full app.
