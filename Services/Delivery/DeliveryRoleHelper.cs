using System;
using System.Security.Claims;

namespace Healthcare_Industry.Services;

public static class DeliveryRoleHelper
{
    public const string AdminEmail = "Delivery@wellnest.com";

    public static bool IsDeliveryAdmin(this ClaimsPrincipal? user)
        => string.Equals(user?.Identity?.Name, AdminEmail, StringComparison.OrdinalIgnoreCase);
}