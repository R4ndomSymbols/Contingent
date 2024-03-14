using Utilities;


namespace StudentTracking.Models.Domain.Orders;

public abstract class FreeContingentOrder : Order
{
    public override string OrderOrgId {
        get => _orderNumber + "-" + "к";
    }
    
    protected FreeContingentOrder() : base()
    {

    }
    protected FreeContingentOrder(int id) : base(id)
    {

    }
    // проверяет отсутствие у студента договора о платном обучении
    // а так же проводит базовую проверку, свойственную любому приказу
    internal override async Task<ResultWithoutValue> CheckBaseConductionPossibility(IEnumerable<StudentModel> toCheck)
    {
        var baseCheck = await base.CheckBaseConductionPossibility(toCheck);
        if (baseCheck.IsFailure)
        {
            return baseCheck;
        }
        foreach (var std in toCheck){
            if (std.PaidAgreement.IsConcluded()){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, проходящих по приказу К, имеют договор о платном образовании"));
            }
        }
        return ResultWithoutValue.Success();
    }


}
