using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

namespace StudentTracking.Models.Domain.Orders;


public sealed class EmptyOrder : Order
{
    public override string OrderOrgId {
        get => _orderNumber.ToString() + "-TEMP";
    }
    private EmptyOrder(){
        _conductionStatus = OrderConductionStatus.ConductionNotAllowed;
        _effectiveDate = DateTime.Today;
        _specifiedDate = DateTime.Today;
        _id = Utils.INVALID_ID;
        _orderDescription = "";
        _orderDisplayedName = "";
        _orderNumber = 0;
    }

    public static EmptyOrder Empty => new EmptyOrder();

    public override Task ConductByOrder()
    {
        throw new NotImplementedException("Невозможно провести пустой приказ");
    }

    protected override OrderTypes GetOrderType(){
        return OrderTypes.EmptyOrder;
    }

    public override Task Save(ObservableTransaction? scope)
    {
        throw new NotImplementedException("Невозможно сохранить пустой приказ");
    }

    internal override Task<ResultWithoutValue> CheckConductionPossibility()
    {
        throw new NotImplementedException("Невозможно проверить проводимость пустого приказа");
    }
}
