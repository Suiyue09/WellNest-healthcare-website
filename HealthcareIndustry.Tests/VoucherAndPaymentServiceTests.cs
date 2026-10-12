using Healthcare_Industry.Models;
using Healthcare_Industry.Pages.Customer;
using Healthcare_Industry.Services;

namespace HealthcareIndustry.Tests;

public class VoucherAndPaymentServiceTests
{
    [Fact]
    public void Recommends_vouchers_that_match_cart_total_and_categories()
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = 2,
                Product = new Product
                {
                    Price = 70m,
                    Category = ProductCategory.Supplements
                }
            }
        };

        var recommendations = service.GetRecommendedVouchers(items).ToList();

        Assert.Contains(recommendations, v => v.Code == "WELLNESS10" && v.IsEligible);
        Assert.Contains(recommendations, v => v.Code == "SUPP15" && v.IsEligible);
        Assert.Contains(recommendations, v => v.Code == "FREESHIP120" && v.IsEligible);
    }

    [Fact]
    public void Applies_fixed_percentage_and_shipping_vouchers_to_checkout_total()
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = 1,
                Product = new Product
                {
                    Price = 100m,
                    Category = ProductCategory.HealthDevices
                }
            }
        };

        var result = service.CalculateTotals(items, "WELLNESS10");

        Assert.Equal(100m, result.Subtotal);
        Assert.Equal(10m, result.Discount);
        Assert.Equal(8m, result.Shipping);
        Assert.Equal(98m, result.Total);
        Assert.Equal("WELLNESS10", result.AppliedVoucherCode);
    }

    [Fact]
    public void Applies_one_reward_voucher_with_automatic_free_shipping()
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = 1,
                Product = new Product
                {
                    Price = 120m,
                    Category = ProductCategory.HealthDevices
                }
            }
        };

        var result = service.CalculateTotals(items, "CASHBACK5", null);

        Assert.Equal(120m, result.Subtotal);
        Assert.Equal(6m, result.Discount);
        Assert.Equal(0m, result.Shipping);
        Assert.Equal(114m, result.Total);
        Assert.Contains("CASHBACK5", result.AppliedVoucherCodes);
        Assert.Contains("FREESHIP120", result.AppliedVoucherCodes);
    }

    [Fact]
    public void Allows_only_one_cashback_or_discount_reward_voucher()
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = 1,
                Product = new Product
                {
                    Price = 120m,
                    Category = ProductCategory.HealthDevices
                }
            }
        };

        var result = service.CalculateTotals(items, "CASHBACK5", "WELLNESS10");

        Assert.Contains("CASHBACK5", result.AppliedVoucherCodes);
        Assert.DoesNotContain("WELLNESS10", result.AppliedVoucherCodes);
        Assert.Contains("Choose only one cashback or discount voucher", result.VoucherMessage);
    }

    [Theory]
    [InlineData(1, 55, 0, 63)]
    [InlineData(2, 110, 55, 63)]
    [InlineData(3, 165, 55, 110)]
    public void Applies_buy_one_free_one_product_discount(int quantity, decimal subtotal, decimal discount, decimal total)
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = quantity,
                Product = new Product
                {
                    Price = 55m,
                    Label = ProductLabel.Buy1Free1,
                    Category = ProductCategory.BabyCare
                }
            }
        };

        var result = service.CalculateTotals(items, null);

        Assert.Equal(subtotal, result.Subtotal);
        Assert.Equal(discount, result.Discount);
        Assert.Equal(total, result.Total);
    }

    [Fact]
    public async Task Demo_payment_gateway_creates_redirect_request_with_reference()
    {
        var gateway = new DemoPaymentGateway();
        var order = new Order
        {
            Id = 42,
            CustomerEmail = "customer@example.com",
            Total = 98m
        };

        var request = await gateway.CreatePaymentRequestAsync(order, "https://localhost/payment-return");

        Assert.Equal("DEMO-42", request.Reference);
        Assert.Contains("/Customer/PaymentGateway", request.RedirectUrl);
        Assert.Contains("orderId=42", request.RedirectUrl);
        Assert.Contains("reference=DEMO-42", request.RedirectUrl);
        Assert.Equal(98m, request.Amount);
    }

    [Theory]
    [InlineData("GatewayBanking", "fpx")]
    [InlineData("GatewayWallet", "grabpay")]
    [InlineData("Card", "card")]
    public void Stripe_gateway_maps_checkout_payment_method_to_stripe_method(string paymentMethod, string expectedStripeMethod)
    {
        var order = new Order { PaymentMethod = paymentMethod };

        var stripePaymentMethods = StripeCheckoutPaymentGateway.GetPaymentMethodTypes(order);

        Assert.Equal([expectedStripeMethod], stripePaymentMethods);
    }

    [Theory]
    [InlineData("Vincy Lim", "vincy@example.com", "V")]
    [InlineData("   ", "customer@example.com", "C")]
    [InlineData(null, null, "U")]
    public void Customer_initial_prefers_full_name_before_email(string? fullName, string? email, string expected)
    {
        var initial = CustomerDisplayService.GetInitial(fullName, email);

        Assert.Equal(expected, initial);
    }

    [Fact]
    public void Stock_validation_detects_out_of_stock_and_excess_quantity_items()
    {
        var items = new[]
        {
            new CartItem
            {
                Quantity = 1,
                Product = new Product { Name = "Digital Thermometer", Stock = 0 }
            },
            new CartItem
            {
                Quantity = 4,
                Product = new Product { Name = "Vitamin C", Stock = 2 }
            },
            new CartItem
            {
                Quantity = 1,
                Product = new Product { Name = "Omega-3", Stock = 5 }
            }
        };

        var result = StockAvailabilityService.Validate(items);

        Assert.False(result.IsAvailable);
        Assert.Contains("Digital Thermometer is out of stock.", result.Messages);
        Assert.Contains("Vitamin C only has 2 in stock.", result.Messages);
    }

    [Fact]
    public void Checkout_input_confirms_order_by_default()
    {
        var input = new CheckoutModel.InputModel();

        Assert.True(input.ConfirmOrder);
    }

    [Theory]
    [InlineData("Vitamin C 1000mg", "/images/products/vitamin-c.png")]
    [InlineData("Blood Pressure Monitor", "/images/products/blood-pressure-monitor.png")]
    [InlineData("Unknown Product", null)]
    public void Product_image_catalog_returns_local_image_for_known_products(string productName, string? expectedUrl)
    {
        var imageUrl = ProductImageCatalog.GetImageUrl(productName);

        Assert.Equal(expectedUrl, imageUrl);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, true)]
    [InlineData(OrderStatus.Packed, true)]
    [InlineData(OrderStatus.Shipped, false)]
    [InlineData(OrderStatus.OutForDelivery, false)]
    [InlineData(OrderStatus.Delivered, false)]
    [InlineData(OrderStatus.Cancelled, false)]
    public void Customer_can_cancel_only_before_order_is_shipped(OrderStatus status, bool expected)
    {
        var workflow = new OrderWorkflowService();
        var order = new Order { Status = status };

        var canCancel = workflow.CanCustomerCancel(order);

        Assert.Equal(expected, canCancel);
    }

    [Fact]
    public void Calculates_totals_only_for_selected_checkout_items()
    {
        var service = new VoucherService();
        var selectedItems = new[]
        {
            new CartItem
            {
                Id = 2,
                Quantity = 2,
                Product = new Product
                {
                    Price = 55m,
                    Label = ProductLabel.Buy1Free1,
                    Category = ProductCategory.BabyCare
                }
            }
        };

        var result = service.CalculateTotals(selectedItems, null);

        Assert.Equal(110m, result.Subtotal);
        Assert.Equal(55m, result.Discount);
        Assert.Equal(63m, result.Total);
    }

    [Fact]
    public void Puts_best_eligible_voucher_first_for_checkout_auto_selection()
    {
        var service = new VoucherService();
        var items = new[]
        {
            new CartItem
            {
                Quantity = 2,
                Product = new Product
                {
                    Price = 70m,
                    Category = ProductCategory.Supplements
                }
            }
        };

        var recommendation = service.GetRecommendedVouchers(items).First(v => v.IsEligible);

        Assert.Equal("SUPP15", recommendation.Code);
    }

    [Theory]
    [InlineData("4111 1111 1111 1234", "1234")]
    [InlineData("5555444433332222", "2222")]
    public void Masks_demo_card_to_last_four_only(string cardNumber, string expectedLastFour)
    {
        var result = DemoCardDetails.FromInput("Vincy Lim", cardNumber, "12/28");

        Assert.Equal(expectedLastFour, result.LastFour);
        Assert.Equal($"**** **** **** {expectedLastFour}", result.MaskedNumber);
    }
}
