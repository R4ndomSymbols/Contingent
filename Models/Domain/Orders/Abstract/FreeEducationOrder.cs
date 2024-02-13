using System.Reflection.Metadata;
using System.Text.Json.Serialization;
using Npgsql;
using Npgsql.PostgresTypes;
using Utilities;
using Utilities.Validation;
using StudentTracking.Models.JSON;
using System.Net.Http.Headers;
using StudentTracking.Models.Domain.Orders.OrderData;
using StudentTracking.Controllers.DTO.In;


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

    internal override async Task<ResultWithoutValue> CheckBaseConductionPossibility(IEnumerable<StudentModel> toCheck)
    {
        var baseCheck = await base.CheckBaseConductionPossibility(toCheck);
        if (baseCheck.IsFailure)
        {
            return baseCheck;
        }
        foreach (var std in toCheck){
            if (!std.PaidAgreement.IsConcluded()){
                return ResultWithoutValue.Failure(new OrderValidationError("Один или несколько студентов, проходящих по приказу К, имеют договор о платном образовании"));
            }
        }
        return ResultWithoutValue.Success();
    }


}
