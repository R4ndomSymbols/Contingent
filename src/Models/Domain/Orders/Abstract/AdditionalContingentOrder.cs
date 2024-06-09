using Contingent.Models.Domain.Students;
using Contingent.Models.Domain.Orders.Infrastructure;
using Contingent.Utilities;

namespace Contingent.Models.Domain.Orders;

public abstract class AdditionalContingentOrder : Order
{
    public override string OrderOrgId
    {
        get => _orderNumber + "-дк";
    }

    protected AdditionalContingentOrder() : base()
    {

    }
    protected AdditionalContingentOrder(int id) : base(id)
    {

    }

    protected override OrderSequentialGuardian SequentialGuardian => PaidOrderSequentialGuardian.Instance;

    protected override ResultWithoutValue CheckOrderClassSpecificConductionPossibility(IEnumerable<StudentModel> toCheck, ObservableTransaction scope)
    {
        // проверка на наличие договора о платном обучении для всех студентов, без его наличия невозможно проведение по этим приказам
        foreach (var std in toCheck)
        {
            if (!std.PaidAgreement.IsConcluded())
            {
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        "не может быть проведен по приказу, т.к. у него отсутствует договор о платном обучении", std));
            }
        }
        var lowerCheck = this.CheckTypeSpecificConductionPossibility(scope);
        if (lowerCheck.IsFailure)
        {
            return lowerCheck;
        }
        return ResultWithoutValue.Success();

    }
    protected abstract ResultWithoutValue CheckTypeSpecificConductionPossibility(ObservableTransaction scope);

    public override void Save(ObservableTransaction scope)
    {
        base.Save(scope);
        SequentialGuardian.Insert(this, scope);
        SequentialGuardian.Save(scope);
    }

}
