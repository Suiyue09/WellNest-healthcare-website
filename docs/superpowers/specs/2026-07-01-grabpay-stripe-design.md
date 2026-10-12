# GrabPay Stripe Design

## Goal

Add one real e-wallet payment option, GrabPay, through the existing Stripe Checkout integration.

## Scope

The checkout page keeps FPX as online banking and changes the e-wallet option to GrabPay. The Stripe gateway maps FPX orders to `fpx` and GrabPay orders to `grabpay`. Existing Stripe return verification remains the source of truth for paid or failed payment status.

## Architecture

`CheckoutModel` continues to store the selected payment method in `Order.PaymentMethod`. `StripeCheckoutPaymentGateway` translates the app-level payment method value into Stripe Checkout `PaymentMethodTypes`. Demo Stripe configuration still uses `DemoPaymentGateway`; real Stripe configuration creates a Stripe Checkout Session.

## Error Handling

Unsupported payment method values fall back to card-compatible Stripe behavior only where the existing code already supports card payments. Stripe API errors continue to surface as the current configuration exception.

## Testing

Unit tests cover the app-to-Stripe payment method mapping for FPX and GrabPay. Existing demo gateway tests remain unchanged.
