using StudentTracking.Controllers.DTO.In;
using StudentTracking.Models.Domain.Orders;
using StudentTracking.Models.Domain.Orders.OrderData;
using Utilities;

public class FreeDeductionWithOwnDesireOrder : FreeContingentOrder
{
    private StudentGroupNullifyMoveList _graduates;

    public FreeDeductionWithOwnDesireOrder() : base() {
    
    }

    public FreeDeductionWithOwnDesireOrder(int id) : base (id){
    
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder?>> Create(OrderDTO dto){
        var created = new FreeDeductionWithOwnDesireOrder();
        var result =  await created.MapBase(dto);
        return result.Retrace(created);
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder?>> Create(int id) {
        var fromDb = new FreeDeductionWithOwnDesireOrder(id);
        var result = await fromDb.MapFromDbBase(id);
        return result.Retrace(fromDb);    
    }

    public static async Task<Result<FreeDeductionWithOwnDesireOrder?>> Create(int id, StudentGroupNullifyMoveDTO dto){
        var result = await Create(id);
        if (result.IsFailure){
            return result;
        }
        var moves = await StudentGroupNullifyMoveList.Create(dto);
        if (moves.IsFailure){
            return result.RetraceFailure<FreeDeductionWithOwnDesireOrder>();
        }
        var order = result.ResultObject;
        order._graduates = moves.ResultObject;
        var conductionPossible = order.CheckConductionPossibility();
    
    
    }



    public override Task ConductByOrder()
    {
        throw new NotImplementedException();
    }

    public override Task Save(ObservableTransaction? scope)
    {
        throw new NotImplementedException();
    }

    protected override OrderTypes GetOrderType()
    {
        return OrderTypes.FreeDeductionWithOwnDesire;
    }

    internal override async Task<ResultWithoutValue> CheckConductionPossibility()
    {
        await base.CheckBaseConductionPossibility(_graduates.Moves.Select(x => x.Student));
    }
}