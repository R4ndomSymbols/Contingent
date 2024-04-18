using StudentTracking.Models.Domain.Orders.Infrastructure;
using Utilities;


namespace StudentTracking.Models.Domain.Orders;

public abstract class FreeContingentOrder : Order
{
    public override string OrderOrgId {
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
    internal override ResultWithoutValue CheckConductionPossibility(IEnumerable<StudentModel>? toCheck)
    {
        var baseCheck = base.CheckConductionPossibility(toCheck);
        if (baseCheck.IsFailure || toCheck is null)
        {
            return baseCheck;
        }
        foreach (var std in toCheck){
            if (std.PaidAgreement.IsConcluded()){
                return ResultWithoutValue.Failure(
                    new OrderValidationError(
                        string.Format("Студент {0} имеет договор о платном обучении, это недопустимо для данного приказа", std.GetName())
                        )
                );
            }
        }
        var lowerCheck = CheckSpecificConductionPossibility();
        if (lowerCheck.IsFailure)
        {
            return lowerCheck;
        }
        return ResultWithoutValue.Success();
    }
    protected abstract ResultWithoutValue CheckSpecificConductionPossibility();

    public override async Task Save(ObservableTransaction? scope) 
    {
        await base.Save(scope);
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
