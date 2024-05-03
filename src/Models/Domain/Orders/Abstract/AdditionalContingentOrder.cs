using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders.Infrastructure;
using Utilities;

namespace Contingent.Models.Domain.Orders;

public abstract class AdditionalContingentOrder : Order
{
    public override string OrderOrgId
    {
        get => _orderNumber + "-ДК";
    }

    protected AdditionalContingentOrder() : base()
    {

    }
    protected AdditionalContingentOrder(int id) : base(id)
    {

    }

    protected override OrderSequentialGuardian SequentialGuardian => PaidOrderSequentialGuardian.Instance;

    internal override ResultWithoutValue CheckConductionPossibility(IEnumerable<StudentModel>? toCheck)
    {
        var upperCheck = base.CheckConductionPossibility(toCheck);
        if (upperCheck.IsFailure || toCheck is null)
        {
            return upperCheck;
        }
        // проверка на наличие договора о платном обучении для всех студентов, без его наличия невозможно проведение по этим приказам
        foreach (var std in toCheck)
        {
            if (!std.PaidAgreement.IsConcluded())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} не может быть проведен по приказу, т.к. у него отсутствует договор о платном обучении", std.GetName())
                    )
                );
            }
        }
        var lowerCheck = this.CheckSpecificConductionPossibility();
        if (lowerCheck.IsFailure)
        {
            return lowerCheck;
        }
        this._conductionStatus = OrderData.OrderConductionStatus.ConductionReady;
        return ResultWithoutValue.Success();

    }
    protected abstract ResultWithoutValue CheckSpecificConductionPossibility();

    public override void Save(ObservableTransaction? scope)
    {
        base.Save(scope);
        SequentialGuardian.Insert(this);
        SequentialGuardian.Save();
    }

}
