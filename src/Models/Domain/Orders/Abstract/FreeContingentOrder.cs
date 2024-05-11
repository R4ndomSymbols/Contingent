using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders.Infrastructure;
using Utilities;


namespace Contingent.Models.Domain.Orders;

public abstract class FreeContingentOrder : Order
{
    public override string OrderOrgId
    {
        get => _orderNumber + "-" + "к";
    }

    protected override OrderSequentialGuardian SequentialGuardian => FreeOrderSequentialGuardian.Instance;

    protected FreeContingentOrder() : base()
    {

    }
    protected FreeContingentOrder(int id) : base(id)
    {

    }
    // проверяет отсутствие у студента договора о платном обучении
    // а так же проводит базовую проверку, свойственную любому приказу
    protected override ResultWithoutValue CheckOrderClassSpecificConductionPossibility(IEnumerable<StudentModel> toCheck)
    {
        // приказ о переводе на бюджет требует того, чтобы у студента был договор,
        // но он к
        var suppressCheck = this.GetOrderTypeDetails().Type == OrderTypes.FreeTransferFromPaidToFree;
        foreach (var std in toCheck)
        {
            if (suppressCheck && !std.PaidAgreement.IsConcluded())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError("имеет не имеет договора о платном обучении, это недопустимо для приказа о переводе на бюджет", std)
                );
            }
            if (std.PaidAgreement.IsConcluded())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError("имеет договор о платном обучении, это недопустимо для приказа", std)
                );
            }
        }
        var lowerCheck = CheckTypeSpecificConductionPossibility();
        return lowerCheck;
    }
    protected abstract ResultWithoutValue CheckTypeSpecificConductionPossibility();

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
        SequentialGuardian.Insert(this);
        SequentialGuardian.Save();
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
