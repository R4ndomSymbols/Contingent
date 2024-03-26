namespace StudentTracking.Models.Domain.Orders;

public abstract class AdditionalContingentOrder : Order{
    public override string OrderOrgId {
        get => _orderNumber + "-ДК";
    }

}
