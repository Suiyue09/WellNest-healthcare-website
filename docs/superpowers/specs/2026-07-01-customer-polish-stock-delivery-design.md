# Customer Polish, Stock, And Delivery Design

## Goal

Polish the customer profile, cart, checkout, and cancellation experience while confirming that admin stock and delivery-driver order updates are connected to real database state.

## Scope

Remove the profile voucher navigation item because vouchers are recommended at checkout. Short-lived customer notices should disappear after a few seconds. Customer-facing initials should prefer the user's full name. Empty cart spacing should feel less cramped. Checkout should not proceed with items that are now out of stock or above current stock. The checkout confirmation step should proceed with a button only, without the extra reminder notice. Cancellation messages should be concise and short-lived.

## Architecture

Customer pages keep their existing Razor Page structure. A small shared helper computes initials from full name first and email second. Cart and checkout handlers continue to query products from the database so admin stock changes flow into the customer interface. Delivery pages already read assigned orders by `AssignedDeliveryId` and update status through the database; this pass verifies that behavior rather than replacing it.

## Testing

Unit tests cover the shared initial helper and stock validation helper. The full project test suite and Release build verify Razor/C# compilation.
