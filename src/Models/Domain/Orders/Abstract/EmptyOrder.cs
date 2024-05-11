using Contingent.Models.Domain.Students;
using Contingent.Import;
using Contingent.Models.Domain.Orders.Infrastructure;
using Contingent.Models.Domain.Orders.OrderData;
using Utilities;

namespace Contingent.Models.Domain.Orders;


public sealed class EmptyOrder : Order
{
    public override string OrderOrgId
    {
        get => "[Не может быть определен на данный момент]";
    }
    private EmptyOrder() : base()
    {
        _effectiveDate = DateTime.Today;
        _specifiedDate = DateTime.Today;
        _id = Utils.INVALID_ID;
        _orderDescription = "";
        _orderDisplayedName = "";
        _orderNumber = 0;
    }

    public static EmptyOrder Empty => new EmptyOrder();

    protected override OrderSequentialGuardian SequentialGuardian => throw new NotImplementedException();

    protected override ResultWithoutValue CheckOrderClassSpecificConductionPossibility(IEnumerable<StudentModel> students)
    {
        throw new NotImplementedException("Невозможно проверить проводимость пустого приказа");
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.EmptyOrder;
    }

    public override void Save(ObservableTransaction? scope)
    {
        throw new NotImplementedException("Невозможно сохранить пустой приказ");
    }

    protected override ResultWithoutValue ConductByOrderInternal()
    {
        throw new NotImplementedException("Невозможно провести пустой приказ");
    }

    public override Result<Order> MapFromCSV(CSVRow row)
    {
        throw new NotImplementedException("Невозможно загрузить пустой приказ из CSV");
    }

    protected override IEnumerable<StudentModel>? GetStudentsForCheck()
    {
        throw new NotImplementedException("Невозможно получить студентов у пустого приказа");
    }
}
