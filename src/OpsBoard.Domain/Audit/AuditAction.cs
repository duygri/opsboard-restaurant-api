namespace OpsBoard.Domain.Audit;

public enum AuditAction
{
    OrderCreated,
    OrderStatusChanged,
    OrderCancelled,
    MenuItemPriceChanged,
    MenuItemDisabled,
    UserCreated,
    UserUpdated,
    UserRoleChanged,
    UserDisabled
}
