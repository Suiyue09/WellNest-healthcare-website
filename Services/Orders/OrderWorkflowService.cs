using Healthcare_Industry.Models;

namespace Healthcare_Industry.Services;

public interface IOrderWorkflowService
{
    bool CanCustomerCancel(Order order);
}

public sealed class OrderWorkflowService : IOrderWorkflowService
{
    public bool CanCustomerCancel(Order order)
        => order.Status is OrderStatus.Pending or OrderStatus.Packed;
}
